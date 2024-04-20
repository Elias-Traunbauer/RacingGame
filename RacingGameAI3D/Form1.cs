using ClosedGL;
using ClosedGL.InputSystem;
using EmmentalerModel;
using GameLogic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using VRageMath;

namespace RacingGameAI3D
{
    public partial class Form1 : Form
    {
        PictureBox pb;

        public Semaphore criticalState = new Semaphore(1, 1);

        public GameController GameController { get; set; }
        public GameAgent[] Agents { get; set; } = new GameAgent[100];
        public Emmentaler[] Emmentalers { get; set; } = new Emmentaler[100];
        public bool TrackChanged { get; set; } = false;

        public Form1()
        {
            InitializeComponent();

            //Input.Update();

            // hide cursor
            //Cursor.Hide();
            this.BackColor = System.Drawing.Color.Black;
            // make form fullscreen
            //FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            this.KeyUp += Form1_KeyUp;

            // set form to be double buffered
            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);

            this.FormClosing += (s, e) =>
            {
                Running = false;
                criticalState.WaitOne();
            };

            pb = new PictureBox()
            {
                Dock = DockStyle.Fill,
                //SizeMode = PictureBoxSizeMode.StretchImage,
            };

            Controls.Add(pb);
        }

        float DegToRad(float deg)
        {
            return deg * (float)Math.PI / 180.0f;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            GameController = new GameController();
            InitializeAgents();

            IRenderer camera = new CameraGPUFragmentedTiledExplicitGrouping()
            {
                FieldOfView = 70f,
                Position = new Vector3(0, 30, 29),
                Rotation = Quaternion.CreateFromYawPitchRoll(0, DegToRad(360 - 80), 0),
                RenderResolution = new Vector2I(Width, Height),
            };

            float scale = 0.1f;

            GameController.GenerateTrack(100, Random.Shared.Next());
            TrackChanged = true;

            List<GameObject> track = new();

            List<GameObject> players = new();
            List<GameObject> playerTires = new();

            for (int i = 0; i < Agents.Length; i++)
            {
                players.Add(new Cube()
                {
                    Position = new Vector3(0, 0, 0),
                    Scale = new Vector3(3, 3, 15),
                });

                for (int j = 0; j < 4; j++)
                {
                    playerTires.Add(new Cube()
                    {
                        Position = new Vector3(0, 0, 0),
                        Scale = new Vector3(2, 2, 2),
                    });
                }
            }
            camera.Initialize([players.First().Texture!]);

            var t = new Thread(() =>
            {
                criticalState.WaitOne();
                Vector3 cameraVelocity = Vector3.Zero;
                Vector2 MouseDelta = Vector2.Zero;
                while (Running)
                {
                    if (TrackChanged)
                    {
                        float lastY = 0;
                        foreach (var trackSegment in GameController.TrackWithResolutionModifier(3))
                        {
                            track.Add(new Cube()
                            {
                                Position = new Vector3(trackSegment.X * scale + ((GameController.TrackWidth * scale) / 2), 0, trackSegment.Y * scale),
                                Scale = new Vector3(GameController.TrackWidth * scale, scale * 100, scale * (trackSegment.Y - lastY))
                            });
                            lastY = trackSegment.Y;
                        }
                        TrackChanged = false;
                    }

                    //Input.Update();
                    //float speed = 2;
                    //if (Input.IsKeyDown(Keys.W))
                    //{
                    //    cameraVelocity += Vector3.Forward * camera.Rotation * speed;
                    //}
                    //if (Input.IsKeyDown(Keys.S))
                    //{
                    //    cameraVelocity += Vector3.Backward * camera.Rotation * speed;
                    //}
                    //if (Input.IsKeyDown(Keys.A))
                    //{
                    //    cameraVelocity += Vector3.Left * camera.Rotation * speed;
                    //}
                    //if (Input.IsKeyDown(Keys.D))
                    //{
                    //    cameraVelocity += Vector3.Right * camera.Rotation * speed;
                    //}
                    //if (Input.IsKeyDown(Keys.Space))
                    //{
                    //    cameraVelocity += Vector3.Up * camera.Rotation * speed;
                    //}
                    //if (Input.IsKeyDown(Keys.LShiftKey))
                    //{
                    //    cameraVelocity += Vector3.Down * camera.Rotation * speed;
                    //}

                    //if (Input.IsKeyDown(Keys.Escape))
                    //{
                    //    Running = false;
                    //    new Thread(() =>
                    //    {
                    //        Invoke(Close);
                    //    }).Start();
                    //}

                    //MouseDelta.X = Input.GetMouseDeltaX();
                    //MouseDelta.Y = Input.GetMouseDeltaY();

                    //MouseDelta /= 70;
                    ////camera.Rotation = lerpyQ.GetValue(x / 10);

                    //cameraVelocity *= 0.9f;

                    //camera.Position += cameraVelocity * 0.1f;

                    //ClosedGL.SMath.YawPitchRoll cameraRotation = ClosedGL.SMath.YawPitchRoll.FromQuaternion(camera.Rotation);

                    //cameraRotation.Yaw -= MouseDelta.X;
                    //cameraRotation.Pitch += MouseDelta.X;

                    //// clamp pitch
                    ////cameraRotation.Y = (float)Math.Max(-Math.PI / 2, Math.Min(Math.PI / 2, cameraRotation.Y));

                    //float roll = 0;

                    //if (Input.IsKeyDown(Keys.Q))
                    //{
                    //    roll = 0.01f;
                    //}
                    //if (Input.IsKeyDown(Keys.E))
                    //{
                    //    roll = -0.01f;
                    //}

                    //var quaternion = Quaternion.CreateFromYawPitchRoll(-MouseDelta.X, -MouseDelta.Y, roll);

                    //camera.Rotation *= quaternion;
                    camera.RenderResolution = new Vector2I(Width, Height);

                    for (int i = 0; i < Agents.Length; i++) 
                    {
                        players[i].Position = new Vector3(Agents[i].Position.X * scale, 2.5f + 5f, Agents[i].Position.Y * scale);
                        players[i].Rotation = Quaternion.CreateFromYawPitchRoll(Agents[i].Rotation, 0, 0);
                    }

                    GameAgent? topAgent = Agents.Where(x => x.IsAlive).OrderByDescending(x => x.FinalScore).FirstOrDefault();

                    if (topAgent != null)
                    {
                        Vector3 agent3dPositon = new Vector3(topAgent.Position.X * scale, 2.5f, topAgent.Position.Y * scale);
                        Vector3 targetPosition = agent3dPositon;
                        targetPosition += Vector3.Forward * 40 + Vector3.Up * 25;
                        camera.Position = Vector3.Lerp(camera.Position, targetPosition, 0.1f);

                        Vector3 lookDirectionTarget = agent3dPositon - camera.Position;
                        lookDirectionTarget = lookDirectionTarget *= new Vector3(1, 1, 1);

                        camera.Rotation = Quaternion.Lerp(camera.Rotation, Quaternion.CreateFromRotationMatrix(Matrix.CreateLookAt(Vector3.Zero, lookDirectionTarget, Vector3.Up)), 0.1f);
                    }

                    var res = camera.Render([..track, ..players/*, ..playerTires*/]);
                }
                criticalState.Release();
            });
            t.IsBackground = true;
            t.Name = "RenderThread";
            t.Start();

            var swapper = new Thread(() =>
            {
                try
                {
                    int lastSwapTime = 1;
                    int frameCnt = 0;
                    Stopwatch stopwatch = new Stopwatch();
                    Stopwatch crauy = new Stopwatch();
                    while (Running)
                    {
                        stopwatch.Restart();
                        int unsuccessfulSwapAttempts = 0;
                        crauy.Restart();
                        int queueLength = camera.GetFrame(out Bitmap? frame);
                        crauy.Stop();

                        if (pb != null && frame != null)
                        {
                            frameCnt++;
                            Graphics g = Graphics.FromImage(frame);

                            Metadata(g,
                                ("Frame #", frameCnt.ToString()),
                                ("Last swap time", lastSwapTime),
                                ("Swaps per second", 1000 / (lastSwapTime == 0 ? 1 : lastSwapTime)),
                                ("FieldOfView", camera.FieldOfView),
                                ("camPos", camera.Position),
                                ("res", camera.RenderResolution),
                                ("getFrame", crauy.ElapsedMilliseconds),
                                ("unsuccessFulSwapAttempts", unsuccessfulSwapAttempts),
                                ("swapChainLength", queueLength));
                            Invoke(() =>
                            {
                                //pb.Image?.Dispose();
                                pb.Image = frame;
                            });
                        }
                        else
                        {
                            unsuccessfulSwapAttempts++;
                        }
                        stopwatch.Stop();
                        lastSwapTime = (int)stopwatch.ElapsedMilliseconds;
                        //Thread.Sleep(Math.Max(0, (1000 / 60) - lastSwapTime));
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            })
            {
                IsBackground = true,
                Name = "SwapperThread"
            };
            swapper.Start();

            var thread = new Thread(Loop)
            {
                IsBackground = true
            };
            thread.Start();
        }

        private void Metadata(Graphics g, params (string, object)[] values)
        {
            int y = 0;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            foreach (var value in values)
            {
                g.DrawString(value.Item1 + ": " + value.Item2.ToString(), new Font("Arial", 12), Brushes.White, 0, y);
                y += 20;
            }
            //foreach (var item in debugValues.ToDictionary())
            //{
            //    g.DrawString(item.Key + ": " + item.Value.ToString(), new Font("Arial", 12), Brushes.White, 0, y);
            //    y += 20;
            //}
        }

        public void Save(Emmentaler[] emmentalers)
        {
            string projectPath = Path.GetDirectoryName(Path.GetDirectoryName(Directory.GetCurrentDirectory()))[0..^3] + "save.emmentaler";
            string SaveFile = projectPath;

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
            string projectPath = Path.GetDirectoryName(Path.GetDirectoryName(Directory.GetCurrentDirectory()))[0..^3] + "save.emmentaler";
            string SaveFile = projectPath;

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

            this.Emmentalers = emmentalers;
        }

        public void InitializeAgents()
        {
            // local project path not absolute
            string projectPath = Path.GetDirectoryName(Path.GetDirectoryName(Directory.GetCurrentDirectory()))[0..^3] + "save.emmentaler";
            string SaveFile = projectPath;
            bool emmentalersLoaded = false;
            if (File.Exists(SaveFile))
            {
                LoadEmmentalers();
                emmentalersLoaded = true;
            }
            for (int i = 0; i < Agents.Length; i++)
            {
                Agents[i] = new GameAgent(GameController);
                if (!emmentalersLoaded)
                {
                    Emmentalers[i] = new Emmentaler(18, 4, new int[] { 15 });
                }
                GameController.AddAgent(Agents[i]);
            }
        }
        float lastScoreAverage = 0;
        float lastTopScore = 0;
        int generation = 0;
        public void Evolution()
        {
            generation++;
            int populationSize = Emmentalers.Length;

            // Evaluate fitness for each agent
            double[] fitnessScores = new double[populationSize];
            for (int i = 0; i < populationSize; i++)
            {
                fitnessScores[i] = Agents[i].FinalScore;  // Assume this method returns a fitness score
            }

            // Select the top performers
            int numberOfSurvivors = populationSize / 3;  // Keep top 30%
            Emmentaler[] survivors = SelectTopPerformers(Emmentalers, fitnessScores, numberOfSurvivors);

            // Create a new generation
            for (int i = 0; i < populationSize; i++)
            {
                if (i < numberOfSurvivors)
                {
                    // Keep the survivors
                    Emmentalers[i] = survivors[i];
                }
                else
                {
                    // Generate new offspring through crossover and mutation
                    Emmentaler parent1 = survivors[Random.Shared.Next(numberOfSurvivors)];
                    Emmentaler parent2 = survivors[Random.Shared.Next(numberOfSurvivors)];
                    Emmentalers[i] = Crossover(parent1, parent2);
                    Mutate(Emmentalers[i], 0.1f, 0.05f);
                }
            }

            foreach (var item in Agents)
            {
                item.Restart();
            }

            // Optionally save the new generation's models
            Save(Emmentalers);
            if (generation % 30 == 0)
            {
                GameController.GenerateTrack(100, Random.Shared.Next());
                TrackChanged = true;
            }
        }

        private static Emmentaler[] SelectTopPerformers(Emmentaler[] networks, double[] fitnessScores, int count)
        {
            return networks.Zip(fitnessScores, (net, score) => new { Network = net, Score = score })
                           .OrderByDescending(ns => ns.Score)
                           .Take(count)
                           .Select(ns => ns.Network)
                           .ToArray();
        }

        private Emmentaler CCrossover(Emmentaler parent1, Emmentaler parent2)
        {
            Emmentaler child = new Emmentaler(parent1.InputNeuronCount, parent1.OutputNeuronCount, parent1.HiddenNeuronCounts);
            Random rnd = new Random();
            int crossoverPoint = rnd.Next(parent1.Weights.Length);  // Choose a crossover point

            for (int i = 0; i < child.Weights.Length; i++)
            {
                if (i < crossoverPoint)
                {
                    child.Weights[i] = parent1.Weights[i].ToArray();  // Use parent1 weights before crossover point
                }
                else
                {
                    child.Weights[i] = parent2.Weights[i].ToArray();  // Use parent2 weights after crossover point
                }
            }

            for (int i = 0; i < child.Biases.Length; i++)
            {
                if (i < crossoverPoint)
                {
                    child.Biases[i] = parent1.Biases[i].ToArray();  // Use parent1 biases before crossover point
                }
                else
                {
                    child.Biases[i] = parent2.Biases[i].ToArray();  // Use parent2 biases after crossover point
                }
            }
            return child;
        }

        private Emmentaler Crossover(Emmentaler parent1, Emmentaler parent2)
        {
            Emmentaler child = new Emmentaler(parent1.InputNeuronCount, parent1.OutputNeuronCount, parent1.HiddenNeuronCounts);
            Random rnd = new Random();
            float weightForCrossoverCombination = Remap((float)rnd.NextDouble(), 0, 1, 0.2f, 0.8f);

            for (int i = 0; i < child.Weights.Length; i++)
            {
                float[] resultWeight = parent1.Weights[i].Select(x => x * weightForCrossoverCombination).ToArray();
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

        private float Remap(float value, int v2, int v3, float v4, float v5)
        {
            return Math.Clamp((value - v2) / (v3 - v2) * (v5 - v4) + v4, v4, v5);
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

        struct ThreadParams
        {
            public int start;
            public int cnt;
        }
        public bool DoRender { get; set; }
        public bool Realtime { get; set; }
        public bool Running { get; private set; } = true;

        public void Loop()
        {
            //GameController.TrackMaxDeltaX = 250;
            //GameController.TrackWidth = 150;
            //GameController.GenerateTrack(100, Random.Shared.Next());
            bool soloRun = false;
            bool first = true;
            int actionCnt = 0;

            double maxThreads = 6;
            int agentsPerThread = (int)Math.Ceiling(Agents.Length / maxThreads);

            CountdownEvent cntDown = new CountdownEvent((int)maxThreads);
            ManualResetEvent manualResetEvent = new ManualResetEvent(false);
            Barrier barrier = new Barrier((int)maxThreads + 1);

            void Process(object? obj)
            {
                if (obj is not ThreadParams threadParams)
                {
                    return;
                }

                while (Running)
                {
                    manualResetEvent.WaitOne();
                    // query the neural network
                    int i = threadParams.start;
                    for (int j = 0; j < threadParams.cnt; j++)
                    {
                        if (i + j >= Agents.Length)
                        {
                            continue;
                        }
                        if (Agents[i + j].IsAlive == false)
                        {
                            continue;
                        }

                        var res = Emmentalers[i + j].Predict(Agents[i + j].State);
                        if (res != null)
                        {
                            Agents[i + j].ForwardControl = res[0] > 0.5;
                            Agents[i + j].BackwardControl = res[1] > 0.5;
                            Agents[i + j].LeftControl = res[2] > 0.5;
                            Agents[i + j].RightControl = res[3] > 0.5;
                        }
                        Interlocked.Add(ref actionCnt, 1);
                    }
                    cntDown.Signal();
                    barrier.SignalAndWait();
                }
            }

            for (int i = 0; i < maxThreads; i++)
            {
                Thread t = new Thread(Process);
                t.IsBackground = true;
                t.Start(new ThreadParams { start = i * agentsPerThread, cnt = agentsPerThread });
            }

            Stopwatch w = new Stopwatch();
            float speed = 0.1f;
            while (Running)
            {
                w.Stop();
                float deltaTime = (float)w.Elapsed.TotalSeconds;
                w.Restart();

                GameController.Update(/*(float)w.Elapsed.TotalSeconds*/Realtime ? deltaTime : speed);
                // lerp camera
                //var followAgent = Agents.Where(x => x.IsAlive).Any() ? Agents.Where(x => x.IsAlive).OrderByDescending(x => x.Position.Y).First() : null;
                //CameraPosition = Vector2.Lerp(CameraPosition, followAgent?.Position ?? new Vector2(0, 0), speed * 2);

                //// update the neural network
                //var res = emmentaler.Predict(GameAgent.State);

                //// update the agent
                //GameAgent.ForwardControl = res[0] > 0.5;
                //GameAgent.BackwardControl = res[1] > 0.5;
                //GameAgent.LeftControl = res[2] > 0.5;
                //GameAgent.RightControl = res[3] > 0.5;

                //void ProcessN(object obj)
                //{
                //    int i = (int)obj;
                //    if (agents[i].IsAlive == false)
                //    {
                //        return;
                //    }
                //    var res = emmentalers[i].Predict(agents[i].State);

                //    agents[i].ForwardControl = res[0] > 0.5;
                //    agents[i].BackwardControl = res[1] > 0.5;
                //    agents[i].LeftControl = res[2] > 0.5;
                //    agents[i].RightControl = res[3] > 0.5;
                //}
                manualResetEvent.Set();
                cntDown.Wait();
                manualResetEvent.Reset();
                cntDown.Reset();
                barrier.SignalAndWait();

                var d = actionCnt;
                //Parallel.For(0, agents.Length, (i) => Process(i));
                //for (int i = 0; i < agents.Length; i++)
                //{
                //    Process(i);

                Application.DoEvents();

                if (Agents.All(x => !x.IsAlive))
                {
                    if (first || !soloRun)
                        Evolution();

                    if (soloRun && first)
                    {
                        Agents[0].Restart();
                        var topAgent = SelectTopPerformers(Emmentalers, Agents.Select(x => (double)x.FinalScore).ToArray(), 1).First();
                        int topAgentIndex = Emmentalers.ToList().IndexOf(topAgent);
                        Agents = new GameAgent[] { Agents[topAgentIndex] };
                        Emmentalers = new Emmentaler[] { Emmentalers[topAgentIndex] };
                        Agents[0].Restart();
                    }
                    else if (soloRun)
                    {
                        Agents[0].Restart();
                    }

                    first = false;
                }
            }
        }

        private void Form1_KeyUp(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
            {
                DoRender = !DoRender;
            }

            if (e.KeyCode == Keys.R)
            {
                Realtime = !Realtime;
            }
        }
    }
}
