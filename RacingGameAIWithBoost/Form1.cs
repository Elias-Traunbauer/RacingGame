using GameLogic;
using System.Diagnostics;
using System.Numerics;
using EmmentalerModel;
using static GameLogic.GameAgent;
using System;

namespace RacingGameAIWithBoost
{
    public partial class Form1 : Form
    {
        readonly GameController GameController;

        public Vector2 CameraPosition { get; set; } = new Vector2(0, 0);

        public bool Running { get; set; } = true;

        public PictureBox pb = new();

        public Form1()
        {
            GameController = new();
            InitializeComponent();

            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);

            this.Controls.Add(pb);
            pb.Dock = DockStyle.Fill;

            this.KeyDown += Form1_KeyDown;
            this.KeyUp += Form1_KeyUp;
        }

        public /*async Task*/ void Render()
        {
            //try
            //{
            Vector2 offset = new(Width / 2 - GameController.TrackWidth / 2, Height / 2);
            Vector2 multiplier = new(1, -1);
            int cnt = 0;

            int width = Width;
            int height = Height;

            Bitmap bitmap = new Bitmap(width, height);
            var g = Graphics.FromImage(bitmap);
            g.Clear(Color.Gray);
            bool minimal = false;
            if (minimal)
            {
                g.DrawString($"Generation: {generation}", new Font("Arial", 12), Brushes.White, new PointF(10, 110));
                Invoke(new MethodInvoker(() =>
                {
                    VisualizeVisionOfAgent(g, GameController.Agents.OrderByDescending(x => x.Position.Y).First(), 100, 10, 800, 800);
                    g.Dispose();
                    pb.Image?.Dispose();
                    pb.Image = bitmap;
                    renderSemaphore.Release();
                }));
                return;
            }

            var track = GameController.TrackWithResolutionModifier(3);
            if (track.Length == 0)
            {
                renderSemaphore.Release();
                return;
            }
            Vector2 previousTrackPiece = track.First() - CameraPosition;
            previousTrackPiece *= multiplier;
            previousTrackPiece += offset;
            var trackParts = track.Where(x => Vector2.Distance(x, CameraPosition) < 900);
            if (!trackParts.Any())
            {
                renderSemaphore.Release();
                return;
            }
            int trackPartsSkipped = track.ToList().IndexOf(trackParts.First());
            cnt += trackPartsSkipped;
            foreach (var item in trackParts)
            {
                cnt++;
                bool drawCool = cnt / 10 % 2 == 0;

                var pos = item - CameraPosition;
                pos *= multiplier;
                pos += offset;
                var trackWidth = GameController.TrackWidth;

                if (drawCool)
                {
                    Vector2 centerPointOfLastPoint = previousTrackPiece + new Vector2(trackWidth / 2, 0);
                    Vector2 centerPointOfCurrentPoint = pos + new Vector2(trackWidth / 2, 0);

                    Vector2 direction = centerPointOfCurrentPoint - centerPointOfLastPoint;

                    Vector2 normal = new Vector2(direction.Y, -direction.X);

                    normal = Vector2.Normalize(normal);

                    Vector2[] points = new Vector2[]
                    {
                            centerPointOfLastPoint + normal * 5,
                            centerPointOfLastPoint - normal * 5,
                            centerPointOfCurrentPoint - normal * 5,
                            centerPointOfCurrentPoint + normal * 5
                    };

                    g.FillPolygon(Brushes.Gray, points.Select(x => new PointF(x.X, x.Y)).ToArray());
                }

                //g.FillRectangle(Brushes.Gray, pos.X, pos.Y, trackWidth, 10);

                g.DrawLine(Pens.White, previousTrackPiece.X, previousTrackPiece.Y, pos.X, pos.Y);

                g.DrawLine(Pens.White, previousTrackPiece.X + trackWidth, previousTrackPiece.Y, pos.X + trackWidth, pos.Y);

                previousTrackPiece = pos;
            }
            //previousTrackPiece = GameController.Track.First() - CameraPosition;
            //previousTrackPiece *= multiplier;
            //previousTrackPiece += offset;
            //foreach (var item in GameController.TrackRaw)
            //{
            //    var pos = item - CameraPosition;
            //    pos *= multiplier;
            //    pos += offset;
            //    var trackWidth = GameController.TrackWidth;

            //    g.FillRectangle(Brushes.Gray, pos.X, pos.Y, trackWidth, 10);

            //    g.DrawLine(Pens.Magenta, previousTrackPiece.X, previousTrackPiece.Y, pos.X, pos.Y);

            //    g.DrawLine(Pens.Magenta, previousTrackPiece.X + trackWidth, previousTrackPiece.Y, pos.X + trackWidth, pos.Y);

            //    previousTrackPiece = pos;
            //}

            foreach (var agent in GameController.Agents)
            {
                var pos = agent.Position - CameraPosition;
                pos *= multiplier;
                pos += offset;

                Vector2 forward = new((float)Math.Cos(agent.Rotation), (float)Math.Sin(agent.Rotation));
                forward = new Vector2(forward.Y, forward.X);

                var frontCarPos = pos;
                frontCarPos += forward * multiplier * agent.FrontCarPosition.Length();

                var backCarPos = pos;
                backCarPos -= forward * multiplier * agent.FrontCarPosition.Length();

                // draw steering direction
                var steeringDirection = agent.SteeringDirection;
                var steeringDirectionNormal = new Vector2(steeringDirection.Y, -steeringDirection.X);
                var steeringPos = frontCarPos + steeringDirection * multiplier * 20;

                //g.DrawLine(Pens.Yellow, frontCarPos.X, frontCarPos.Y, steeringPos.X, steeringPos.Y);

                int carWidth = 20;
                int carHeight = (int)(agent.FrontCarPosition.Length() * 2);
                Matrix3x2 rotationMatrix = Matrix3x2.CreateRotation(agent.Rotation);
                Vector2[] carPoints = new Vector2[]
                {
                        new(-carWidth / 2, -carHeight / 2),
                        new(carWidth / 2, -carHeight / 2),
                        new(carWidth / 2, carHeight / 2),
                        new(-carWidth / 2, carHeight / 2)
                };

                for (int i = 0; i < carPoints.Length; i++)
                {
                    carPoints[i] = Vector2.Transform(carPoints[i], rotationMatrix);
                    carPoints[i] += pos;
                }

                // draw boost
                if (agent is GameAgent ag && ag.HasBoost)
                {
                    g.DrawLine(Pens.Orange, (PointF)backCarPos, (PointF)(pos - (forward * multiplier * agent.FrontCarPosition.Length() * 2)));
                }

                Vector2[] frontWheelPositions = new Vector2[]
                {
                        frontCarPos + Vector2.Transform(new Vector2(10, 10), rotationMatrix),
                        frontCarPos - Vector2.Transform(new Vector2(10, -10), rotationMatrix)
                };
                Vector2[] backWheelPositions = new Vector2[]
                {
                        backCarPos + Vector2.Transform(new Vector2(10, -10), rotationMatrix),
                        backCarPos - Vector2.Transform(new Vector2(10, 10), rotationMatrix)
                };

                Vector2[] wheelPoints = new Vector2[]
                {
                        new(-3, -7),
                        new(3, -7),
                        new(3, 7),
                        new(-3, 7)
                };

                foreach (var item in backWheelPositions)
                {
                    var copyWheels = wheelPoints.ToArray();
                    for (int i = 0; i < wheelPoints.Length; i++)
                    {
                        copyWheels[i] = Vector2.Transform(copyWheels[i], rotationMatrix);
                        copyWheels[i] += item;
                    }

                    g.FillPolygon(Brushes.Black, copyWheels.Select(x => new PointF(x.X, x.Y)).ToArray());
                }

                Matrix3x2 steeringMatrix = Matrix3x2.CreateRotation(agent.SteeringAngle);

                foreach (var item in frontWheelPositions)
                {
                    var copyWheels = wheelPoints.ToArray();
                    for (int i = 0; i < wheelPoints.Length; i++)
                    {
                        copyWheels[i] = Vector2.Transform(copyWheels[i], rotationMatrix);
                        copyWheels[i] = Vector2.Transform(copyWheels[i], steeringMatrix);
                        copyWheels[i] += item;
                    }

                    g.FillPolygon(Brushes.Black, copyWheels.Select(x => new PointF(x.X, x.Y)).ToArray());
                }

                g.FillPolygon(new SolidBrush(agent.IsAlive ? agent.Color : Color.DarkGray), carPoints.Select(x => new PointF(x.X, x.Y)).ToArray());

            }
            g.Flush();
            foreach (var item in GameController.rays)
            {
                // draw rays
                var (start, end) = item;
                float len = Vector2.Distance(start, end);

                start -= CameraPosition;
                start *= multiplier;
                start += offset;

                end -= CameraPosition;
                end *= multiplier;
                end += offset;
                g.Flush();
                g.DrawLine(len == 1000 ? Pens.Red : Pens.Blue, start.X, start.Y, end.X, end.Y);
            }

            int agentsAlive = GameController.Agents.Count(x => x.IsAlive);
            float topAgentScore = GameController.Agents.Where(x => x.IsAlive).Any() ? GameController.Agents.Where(x => x.IsAlive).OrderByDescending(x => x.Score).First().Score : 0;
            g.DrawString($"Agents alive: {agentsAlive}", new Font("Arial", 12), Brushes.White, new PointF(10, 10));
            g.DrawString($"Top agent score: {topAgentScore}", new Font("Arial", 12), Brushes.White, new PointF(10, 30));
            float speedsAvgOfTopAgent = GameController.Agents.Where(x => x.IsAlive).Any() ? GameController.Agents.Where(x => x.IsAlive).OrderByDescending(x => x.Score).First().AverageSpeeds.Average() : 0;
            g.DrawString($"Top agent speed: {speedsAvgOfTopAgent}", new Font("Arial", 12), Brushes.White, new PointF(10, 50));
            g.DrawString($"Last score average: {lastScoreAverage}", new Font("Arial", 12), Brushes.White, new PointF(10, 70));
            g.DrawString($"Last top score: {lastTopScore}", new Font("Arial", 12), Brushes.White, new PointF(10, 90));
            g.DrawString($"Generation: {generation}", new Font("Arial", 12), Brushes.White, new PointF(10, 110));
            int size = width / 3;
            int bestAgentIndex = GameController.Agents.ToList().IndexOf((GameController.Agents.Where(x => x.IsAlive).Any() ? GameController.Agents.Where(x => x.IsAlive) : GameController.Agents).OrderByDescending(x => x.Score).First());
            VisualizeNeuralNetworkOnImage(g, emmentalers[bestAgentIndex], 10, 130, size, size);

            int visionWidth = size;
            int visionHeight = size;

            int visionX = width - visionWidth - 20;
            int visionY = 10;
            VisualizeVisionOfAgent(g, GameController.Agents.ToArray()[bestAgentIndex], visionX, visionY, visionWidth, visionHeight);

            Invoke(new MethodInvoker(() =>
            {
                g.Dispose();
                pb.Image?.Dispose();
                pb.Image = bitmap;
                renderSemaphore.Release();
            }));
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show(ex.ToString());
            //    Running = false;
            //    Close();
            //}
            //finally
            //{

            //}
        }
        Semaphore renderSemaphore = new Semaphore(1, 1);

        GameAgent[] agents = new GameAgent[100];
        Emmentaler[] emmentalers = new Emmentaler[100];

        public string SaveFile = @"C:\Users\trauni\Desktop\RacingGame\RacingGameAIWithBoost\save.emmentaler";

        public void Save(Emmentaler[] emmentalers)
        {
            byte[][] emmentalersAsBytes = emmentalers.Select(x => x.ToBinary()).ToArray();
            int[] counts = emmentalersAsBytes.Select(x => x.Length).ToArray();

            byte[] data = new byte[sizeof(int) + sizeof(int) + emmentalersAsBytes.SelectMany(x => x).Count() + counts.Length * sizeof(int)];

            int offset = 0;

            // generation count
            Buffer.BlockCopy(BitConverter.GetBytes(generation), 0, data, offset, sizeof(int));
            offset += sizeof(int);

            Buffer.BlockCopy(BitConverter.GetBytes(emmentalersAsBytes.Length), 0, data, offset, sizeof(int));
            offset += sizeof(int);

            for (int i = 0; i < counts.Length; i++)
            {
                Buffer.BlockCopy(BitConverter.GetBytes(counts[i]), 0, data, offset, sizeof(int));
                offset += sizeof(int);
            }

            for (int i = 0; i < emmentalersAsBytes.Length; i++)
            {
                Buffer.BlockCopy(emmentalersAsBytes[i], 0, data, offset, emmentalersAsBytes[i].Length);
                offset += emmentalersAsBytes[i].Length;
            }

            File.WriteAllBytes(SaveFile, data);
        }

        public void LoadEmmentalers()
        {
            byte[] data = File.ReadAllBytes(SaveFile);

            int offset = 0;

            int generationCount = BitConverter.ToInt32(data, offset);
            generation = generationCount;

            offset += sizeof(int);

            int emmentalerCount = BitConverter.ToInt32(data, offset);

            offset += sizeof(int);

            int[] counts = new int[emmentalerCount];

            for (int i = 0; i < emmentalerCount; i++)
            {
                counts[i] = BitConverter.ToInt32(data, offset);
                offset += sizeof(int);
            }

            Emmentaler[] emmentalers = new Emmentaler[emmentalerCount];

            for (int i = 0; i < emmentalerCount; i++)
            {
                byte[] emmentalerData = new byte[counts[i]];
                Buffer.BlockCopy(data, offset, emmentalerData, 0, counts[i]);
                offset += counts[i];

                emmentalers[i] = Emmentaler.FromBinary(emmentalerData);
            }

            this.emmentalers = emmentalers;
        }

        public void InitializeAgents()
        {
            string projectPath = Path.GetDirectoryName(Path.GetDirectoryName(Directory.GetCurrentDirectory()))[0..^3] + "save.emmentaler";
            SaveFile = projectPath;
            bool emmentalersLoaded = false;
            if (File.Exists(SaveFile))
            {
                LoadEmmentalers();
                emmentalersLoaded = true;
            }
            for (int i = 0; i < agents.Length; i++)
            {
                agents[i] = new GameAgent(GameController);
                if (!emmentalersLoaded)
                {
                    emmentalers[i] = new Emmentaler(21, 5, new int[] { 30, 20, 15 }/*Enumerable.Repeat(4, 69).ToArray()*/);
                }
                GameController.AddAgent(agents[i]);
            }
        }
        float lastScoreAverage = 0;
        float lastTopScore = 0;
        int generation = 0;
        public void Evolution()
        {
            generation++;
            int populationSize = emmentalers.Length;

            // Evaluate fitness for each agent
            double[] fitnessScores = new double[populationSize];
            for (int i = 0; i < populationSize; i++)
            {
                fitnessScores[i] = agents[i].FinalScore;  // Assume this method returns a fitness score
            }

            float[][] weightMedians = new float[emmentalers[0].Weights.Length][];
            float[][] biasMedians = new float[emmentalers[0].Biases.Length][];

            // Select the top performers
            int numberOfSurvivors = populationSize / 2;  // Keep top 25%
            Emmentaler[] survivors = SelectTopPerformers(emmentalers, fitnessScores, numberOfSurvivors / 2);

            // gather median weights and biases
            for (int i = 0; i < emmentalers[0].Weights.Length; i++)
            {
                weightMedians[i] = survivors.Select(x => x.Weights[i]).Select(x => x.OrderBy(x => x).ToArray()).Select(x => x[x.Length / 2]).ToArray();
            }

            for (int i = 0; i < emmentalers[0].Biases.Length; i++)
            {
                biasMedians[i] = survivors.Select(x => x.Biases[i]).Select(x => x.OrderBy(x => x).ToArray()).Select(x => x[x.Length / 2]).ToArray();
            }


            // select another 25% randomly of the 75 remaining with higher probability for higher scores
            Random rnd = new Random();

            for (int i = 0; i < numberOfSurvivors / 2; i++)
            {
                double[] probabilities = fitnessScores.Select(x => x / fitnessScores.Sum()).ToArray();
                double[] cumulativeProbabilities = new double[probabilities.Length];
                double sum = 0;
                for (int j = 0; j < probabilities.Length; j++)
                {
                    sum += probabilities[j];
                    cumulativeProbabilities[j] = sum;
                }

                double random = rnd.NextDouble();
                int index = 0;
                for (int j = 0; j < cumulativeProbabilities.Length; j++)
                {
                    if (random < cumulativeProbabilities[j])
                    {
                        index = j;
                        break;
                    }
                }

                survivors = [.. survivors, emmentalers[index]];
            }

            int wildlyRandom = (populationSize - survivors.Length) / 2;

            // Create a new generation
            for (int i = 0; i < populationSize; i++)
            {
                if (i < numberOfSurvivors)
                {
                    // Keep the survivors
                    emmentalers[i] = survivors[i];
                }
                else
                {
                    // Generate new offspring through crossover and mutation
                    Emmentaler parent1 = survivors[Random.Shared.Next(numberOfSurvivors)];
                    Emmentaler parent2 = survivors[Random.Shared.Next(numberOfSurvivors)];
                    emmentalers[i] = Crossover(parent1, parent2);
                    if (i < wildlyRandom)
                    {
                        Mutate(emmentalers[i], 0.5f, 0.1f);
                    }
                    else
                    {
                        Mutate(emmentalers[i], 0.1f, 0.05f);
                    }
                }
            }

            foreach (var item in agents)
            {
                item.Restart();
            }

            // Optionally save the new generation's models
            Save(emmentalers);
            if (generation % 30 == 0)
                GameController.GenerateTrack(100, Random.Shared.Next());
        }

        private static Emmentaler[] SelectTopPerformers(Emmentaler[] networks, double[] fitnessScores, int count)
        {
            return networks.Zip(fitnessScores, (net, score) => new { Network = net, Score = score })
                           .OrderByDescending(ns => ns.Score)
                           .Take(count)
                           .Select(ns => ns.Network)
                           .ToArray();
        }

        private Emmentaler Crossover(Emmentaler parent1, Emmentaler parent2)
        {
            Emmentaler child = new Emmentaler(parent1.InputNeuronCount, parent1.OutputNeuronCount, parent1.HiddenNeuronCounts);
            Random rnd = new Random();
            float weightForCrossoverCombination = Remap((float)rnd.NextDouble(), 0, 1, 0.3f, 0.7f);

            for (int i = 0; i < child.Weights.Length; i++)
            {
                float[] resultWeight = parent1.Weights[i].Select(x => x *  weightForCrossoverCombination).ToArray();
                float[] result2Weight = parent2.Weights[i].Select(x => x * (1 - weightForCrossoverCombination)).ToArray();

                child.Weights[i] = resultWeight.Zip(result2Weight).Select((x) => (x.First + x.Second)).ToArray();
            }

            for (int i = 0; i < child.Biases.Length; i++)
            {
                float[] resultWeight = parent1.Biases[i].Select(x => x * weightForCrossoverCombination).ToArray();
                float[] result2Weight = parent2.Biases[i].Select(x => x * (1 - weightForCrossoverCombination)).ToArray();

                child.Biases[i] = resultWeight.Zip(result2Weight).Select((x) => (x.First + x.Second)).ToArray();
            }
            return child;
        }

        private void Mutate(Emmentaler network, double mutationRate, double mutationStrength)
        {
            Random rnd = new Random();
            for (int i = 0; i < network.Weights.Length; i++)
            {
                for (int j = 0; j < network.Weights[i].Length; j++)
                {
                    if (rnd.NextDouble() < mutationRate)
                    {  // Only mutate some weights, based on mutationRate
                        network.Weights[i][j] += (float)(mutationStrength * (2 * rnd.NextDouble() - 1));  // Adjust by ±mutationStrength
                    }
                }
            }

            for (int i = 0; i < network.Biases.Length; i++)
            {
                for (int j = 0; j < network.Biases[i].Length; j++)
                {
                    if (rnd.NextDouble() < mutationRate)
                    {  // Only mutate some biases, based on mutationRate
                        network.Biases[i][j] += (float)(mutationStrength * (2 * rnd.NextDouble() - 1));  // Adjust by ±mutationStrength
                    }
                }
            }
        }

        public void Loop()
        {
            GameController.TrackMaxDeltaX = 300;
            GameController.TrackWidth = 200;
            GameController.GenerateTrack(100, Random.Shared.Next());

            InitializeAgents();

            Stopwatch w = new Stopwatch();
            float speed = 0.1f;
            while (Running)
            {

                w.Stop();
                GameController.Update(/*(float)w.Elapsed.TotalSeconds*/speed);
                w.Restart();
                // lerp camera
                var followAgent = agents.Where(x => x.IsAlive).Any() ? agents.Where(x => x.IsAlive).OrderByDescending(x => x.Position.Y).First() : null;
                CameraPosition = Vector2.Lerp(CameraPosition, followAgent?.Position ?? new Vector2(0, 0), speed);

                //// update the neural network
                //var res = emmentaler.Predict(GameAgent.State);

                //// update the agent
                //GameAgent.ForwardControl = res[0] > 0.5;
                //GameAgent.BackwardControl = res[1] > 0.5;
                //GameAgent.LeftControl = res[2] > 0.5;
                //GameAgent.RightControl = res[3] > 0.5;
                void Process(object obj)
                {
                    int i = (int)obj;
                    if (agents[i].IsAlive == false)
                    {
                        return;
                    }
                    var res = emmentalers[i].Predict(agents[i].State);

                    agents[i].ForwardControl = res[0] > 0.5;
                    agents[i].BackwardControl = res[1] > 0.5;
                    agents[i].LeftControl = res[2] > 0.5;
                    agents[i].RightControl = res[3] > 0.5;
                    agents[i].BoostControl = res[4] > 0.5;
                }

                //Parallel.For(0, agents.Length, (i) => Process(i));
                for (int i = 0; i < agents.Length; i++)
                {
                    Process(i);
                }

                try
                {
                    renderSemaphore.WaitOne();
                    Render();
                    renderSemaphore.WaitOne();
                    renderSemaphore.Release();
                }
                catch (ObjectDisposedException)
                {

                }
                //Render();

                Application.DoEvents();

                if (agents.All(x => !x.IsAlive))
                {
                    CameraPosition = agents.OrderByDescending(x => x.Position.Y).First().Position;
                    try
                    {
                        renderSemaphore.WaitOne();
                        Render();
                        renderSemaphore.WaitOne();
                        renderSemaphore.Release();
                    }
                    catch (ObjectDisposedException)
                    {

                    }
                    Evolution();
                }

            }
        }

        private void VisualizeVisionOfAgent(Graphics g, IGameAgent agent, int x, int y, int width, int height)
        {
            float[] visionRays = agent.State[6..];

            bool[] controls = [
                agent.ForwardControl,
                agent.RightControl,
                agent.BackwardControl,
                agent.LeftControl,
            ];

            g.DrawString("Controls", new Font("Arial", 12), Brushes.Black, new PointF(x + 10, y + 10));
            g.DrawString("Vision", new Font("Arial", 12), Brushes.Black, new PointF(x + 10, y + height * 0.7f));

            if (agent is GameAgent ag)
            {
                g.DrawString($"Input counts", new Font("Arial", 12), Brushes.Black, new PointF(x + 10, y + height * 0.7f + 20));
                g.DrawString($"Forward: {ag.InputCounts[0]}", new Font("Arial", 12), Brushes.Black, new PointF(x + 10, y + height * 0.7f + 40));
                g.DrawString($"Backward: {ag.InputCounts[1]}", new Font("Arial", 12), Brushes.Black, new PointF(x + 10, y + height * 0.7f + 60));
                g.DrawString($"Left: {ag.InputCounts[2]}", new Font("Arial", 12), Brushes.Black, new PointF(x + 10, y + height * 0.7f + 80));
                g.DrawString($"Right: {ag.InputCounts[3]}", new Font("Arial", 12), Brushes.Black, new PointF(x + 10, y + height * 0.7f + 100));
                g.DrawString($"Boost: {ag.InputCounts[4]}", new Font("Arial", 12), Brushes.Black, new PointF(x + 10, y + height * 0.7f + 120));
            }

            float controlsPortion = 0.3f;

            int controlY = (int)(height - height * controlsPortion);
            int controlHeight = height - controlY;
            int visionHeight = height - controlY;

            float maxRayLength = width / 2;

            float DegToRad(float deg) => deg * MathF.PI / 180;

            float[] angles = [
                0,
                10,
                -10,
                20,
                -20,
                30,
                -30,
                45,
                -45,
                60,
                -60,
                75,
                -75,
                90,
                -90
            ];
            Array.Sort(angles);
            Vector2 rayOrigin = new Vector2(width / 2, controlY);
            Vector2 up = new Vector2(0, -1);

            PointF[] rayTargets = visionRays.Select((ray, i) =>
            {
                float angle = DegToRad(angles[i]);
                Vector2 direction = Vector2.Transform(up, Matrix3x2.CreateRotation(angle));
                direction.X *= -1;
                Vector2 target = rayOrigin + direction * ray * maxRayLength;
                return new PointF(target.X, target.Y);
            }).ToArray();

            g.DrawRectangle(new Pen(Color.Black), x, y, width, height);

            //g.FillRectangle(Brushes.Gray, x, y, width, controlY);

            Vector2[] TriangleArrow = new Vector2[]
            {
                new Vector2(0, -1),
                new Vector2(0.5f, -0.5f),
                new Vector2(0, -0.5f),
                new Vector2(-0.5f,-0.5f),
            };

            float triangleScale = width / 100;

            float angle = 0;
            for (int i = 0; i < controls.Length; i++)
            {
                if (controls[i])
                {
                    PointF[] points = TriangleArrow.Select(p => new PointF(p.X * 10, p.Y * 10)).ToArray();
                    Matrix3x2 rotationMatrixx = Matrix3x2.CreateRotation(angle);
                    Vector2 offset = Vector2.Zero;
                    points = points.Select(p => Vector2.Transform(new Vector2(p.X, p.Y), rotationMatrixx)).Select(x => new PointF(x.X, x.Y)).ToArray();
                    points = points.Select(p => new PointF(p.X * triangleScale + width / 2 + x + offset.X, p.Y * triangleScale + controlY + y + controlHeight / 2 + offset.Y)).ToArray();
                    g.FillPolygon(Brushes.Black, points);
                }
                angle += MathF.PI / 2;
            }
            if (agent is GameAgent agg)
            {
                if (agg.BoostControl)
                {
                    g.FillRectangle(Brushes.Orange, new Rectangle(
                        width / 2 + x - 10,
                        controlY + y + controlHeight / 2 - 10,
                        20,
                        20
                        ));
                }
            }

            foreach (var item in rayTargets)
            {
                // from rayOrigin to item

                g.DrawLine(Pens.Blue, rayOrigin.X + x, rayOrigin.Y + y, item.X + x, item.Y + y);
            }

            var polygonPoints = new PointF[]
            {
                rayTargets[0],
                rayTargets[1],
                rayTargets[2],
                rayTargets[3],
                rayTargets[4],
                rayTargets[5],
                rayTargets[6],
                rayTargets[7],
                rayTargets[8],
                rayTargets[9],
                rayTargets[10],
                rayTargets[11],
                rayTargets[12],
                rayTargets[13],
                rayTargets[14],
                rayTargets[0],
            };

            g.DrawPolygon(Pens.Black, polygonPoints.Select(d => new PointF(d.X + x, d.Y + y)).ToArray());

            g.Flush();

            // ray angles
            //  10
            //  -10
            // 20
            //- 20
            // 30
            //- 30
            // 45
            //- 45
            // 60
            //- 60
            // 75
            //- 75
            // 90
            //- 90

            // agent rotation and steering direction
            Vector2 multiplier = new Vector2(1, -1);
            var pos = new Vector2(x + width * 0.8f, y + controlY + controlHeight / 2);

            Vector2 forward = new((float)Math.Cos(0), (float)Math.Sin(0));
            //Vector2 forward = new((float)Math.Cos(agent.Rotation), (float)Math.Sin(agent.Rotation));
            forward = new Vector2(forward.Y, forward.X);

            var frontCarPos = pos;
            frontCarPos += forward * multiplier * agent.FrontCarPosition.Length();

            var backCarPos = pos;
            backCarPos -= forward * multiplier * agent.FrontCarPosition.Length();

            // draw steering direction
            var steeringDirection = agent.SteeringDirection;
            var steeringDirectionNormal = new Vector2(steeringDirection.Y, -steeringDirection.X);
            var steeringPos = frontCarPos + steeringDirection * 20;

            //g.DrawLine(Pens.Yellow, frontCarPos.X, frontCarPos.Y, steeringPos.X, steeringPos.Y);

            int carWidth = 20;
            int carHeight = (int)(agent.FrontCarPosition.Length() * 2);
            Matrix3x2 rotationMatrix = Matrix3x2.CreateRotation(0);
            //Matrix3x2 rotationMatrix = Matrix3x2.CreateRotation(agent.Rotation);
            Vector2[] carPoints = new Vector2[]
            {
                        new(-carWidth / 2, -carHeight / 2),
                        new(carWidth / 2, -carHeight / 2),
                        new(carWidth / 2, carHeight / 2),
                        new(-carWidth / 2, carHeight / 2)
            };

            for (int i = 0; i < carPoints.Length; i++)
            {
                carPoints[i] = Vector2.Transform(carPoints[i], rotationMatrix);
                carPoints[i] += pos;
            }
            // draw line between front end back pos
            g.DrawLine(Pens.Red, frontCarPos.X, frontCarPos.Y, backCarPos.X, backCarPos.Y);

            Vector2[] frontWheelPositions = new Vector2[]
            {
                        frontCarPos + Vector2.Transform(new Vector2(10, 10), rotationMatrix),
                        frontCarPos - Vector2.Transform(new Vector2(10, -10), rotationMatrix)
            };
            Vector2[] backWheelPositions = new Vector2[]
            {
                        backCarPos + Vector2.Transform(new Vector2(10, -10), rotationMatrix),
                        backCarPos - Vector2.Transform(new Vector2(10, 10), rotationMatrix)
            };

            Vector2[] wheelPoints = new Vector2[]
            {
                        new(-3, -7),
                        new(3, -7),
                        new(3, 7),
                        new(-3, 7)
            };

            foreach (var item in backWheelPositions)
            {
                var copyWheels = wheelPoints.ToArray();
                for (int i = 0; i < wheelPoints.Length; i++)
                {
                    copyWheels[i] = Vector2.Transform(copyWheels[i], rotationMatrix);
                    copyWheels[i] += item;
                }

                g.FillPolygon(Brushes.Black, copyWheels.Select(x => new PointF(x.X, x.Y)).ToArray());
            }

            Matrix3x2 steeringMatrix = Matrix3x2.CreateRotation(agent.SteeringAngle);

            foreach (var item in frontWheelPositions)
            {
                var copyWheels = wheelPoints.ToArray();
                for (int i = 0; i < wheelPoints.Length; i++)
                {
                    copyWheels[i] = Vector2.Transform(copyWheels[i], rotationMatrix);
                    copyWheels[i] = Vector2.Transform(copyWheels[i], steeringMatrix);
                    copyWheels[i] += item;
                }

                g.FillPolygon(Brushes.Black, copyWheels.Select(x => new PointF(x.X, x.Y)).ToArray());
            }

            g.FillPolygon(new SolidBrush(agent.IsAlive ? agent.Color : Color.DarkGray), carPoints.Select(x => new PointF(x.X, x.Y)).ToArray());


        }

        float Remap(float value, float from1, float to1, float from2, float to2)
        {
            return Math.Clamp((value - from1) / (to1 - from1) * (to2 - from2) + from2, 0, 1);
        }

        private void VisualizeNeuralNetworkOnImage(Graphics g, Emmentaler emmentaler, int x, int y, int width, int height)
        {
            Color Lerp(Color a, Color b, float t) => Color.FromArgb((int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));

            float[][] neurons = emmentaler.lastNeurons;

            float maxAbsWeight = 0;
            for (int i = 0; i < emmentaler.Weights.Length; i++)
            {
                for (int j = 0; j < emmentaler.Weights[i].Length; j++)
                {
                    maxAbsWeight = Math.Max(maxAbsWeight, Math.Abs(emmentaler.Weights[i][j]));
                }
            }

            g.DrawRectangle(new Pen(Color.Black), x, y, width, height);

            int layerCount = 2 + emmentaler.HiddenNeuronCounts.Length;

            float neuronRadius = height / 100;

            float spaceBetweenLayers = (width/* - (layerCount * neuronRadius * 2)*/) / layerCount;

            float[] spacesBetweenNeurons = new float[layerCount];
            spacesBetweenNeurons[0] = (height/* - (emmentaler.InputNeuronCount * neuronRadius * 2)*/) / emmentaler.InputNeuronCount;
            for (int i = 1; i < layerCount - 1; i++)
            {
                spacesBetweenNeurons[i] = (height/* - (emmentaler.HiddenNeuronCounts[i - 1] * neuronRadius * 2)*/) / emmentaler.HiddenNeuronCounts[i - 1];
            }
            spacesBetweenNeurons[layerCount - 1] = (height/* - (emmentaler.OutputNeuronCount * neuronRadius * 2)*/) / emmentaler.OutputNeuronCount;

            for (int i = 0; i < layerCount - 1; i++)
            {
                int neuronCount = i == 0 ? emmentaler.InputNeuronCount : i == layerCount - 1 ? emmentaler.OutputNeuronCount : emmentaler.HiddenNeuronCounts[i - 1];
                int nextNeuronCount = i == 0 ? emmentaler.HiddenNeuronCounts[i] : i == layerCount - 2 ? emmentaler.OutputNeuronCount : emmentaler.HiddenNeuronCounts[i];

                for (int j = 0; j < neuronCount; j++)
                {
                    for (int k = 0; k < nextNeuronCount; k++)
                    {
                        float weight = emmentaler.Weights[i][j];

                        Color color = /*Lerp(Color.Red, Color.Green, Remap((float)Math.Pow(weight, 200), -1, 1, 0, 1));*/
                            weight < 0 ? Color.Red : Color.Green;

                        float lineWidth = Remap(weight, -maxAbsWeight, maxAbsWeight, 1, 5);

                        float opacity = Remap(Math.Abs(weight), 0, maxAbsWeight, 0.3f, 1);

                        color = Color.FromArgb((int)(opacity * 255), color);

                        int x1 = (int)(i * spaceBetweenLayers + neuronRadius * 2 + spaceBetweenLayers / 2);
                        int y1 = (int)(j * spacesBetweenNeurons[i] + neuronRadius + spacesBetweenNeurons[i] / 2);
                        int x2 = (int)((i + 1) * spaceBetweenLayers + spaceBetweenLayers / 2);
                        int y2 = (int)(k * spacesBetweenNeurons[i + 1] + neuronRadius + neuronRadius + spacesBetweenNeurons[i + 1] / 2 - neuronRadius);

                        g.DrawLine(new Pen(color, lineWidth), x1 + x, y1 + y, x2 + x, y2 + y);
                    }
                }
            }

            float neuronAbsoluteMax = 0;
            for (int i = 0; i < neurons.Length; i++)
            {
                for (int j = 0; j < neurons[i].Length; j++)
                {
                    neuronAbsoluteMax = Math.Max(neuronAbsoluteMax, Math.Abs(neurons[i][j]));
                }
            }

            for (float i = 0; i < layerCount; i++)
            {
                int neuronCount = i == 0 ? emmentaler.InputNeuronCount : i == layerCount - 1 ? emmentaler.OutputNeuronCount : emmentaler.HiddenNeuronCounts[(int)i - 1];

                for (float j = 0; j < neuronCount; j++)
                {
                    float neuron = neurons[(int)i][(int)j];

                    // last layer binary
                    if (i == layerCount - 1/* || true*/ && false)
                    {
                        neuron = neuron > 0.5f ? neuronAbsoluteMax : -neuronAbsoluteMax;
                    }

                    float opacity = 1;

                    Color baseColor = Lerp(Color.Red, Color.Green, Remap(neuron, -neuronAbsoluteMax, neuronAbsoluteMax, 0, 1));

                    Color color = Color.FromArgb((int)(opacity * 255), baseColor);

                    int xx = (int)(i * spaceBetweenLayers + spaceBetweenLayers / 2);
                    int yx = (int)((int)(j * spacesBetweenNeurons[(int)i]) + spacesBetweenNeurons[(int)i] / 2);

                    g.FillEllipse(new SolidBrush(color), xx + x, yx + y, neuronRadius * 2, neuronRadius * 2);
                }
            }

            g.Flush();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            var thread = new Thread(Loop)
            {
                IsBackground = true
            };
            thread.Start();
        }

        private void Form1_KeyDown(object? sender, KeyEventArgs e)
        {

        }

        private void Form1_KeyUp(object? sender, KeyEventArgs e)
        {

        }
    }
}
