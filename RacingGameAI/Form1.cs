using GameLogic;
using System.Diagnostics;
using System.Numerics;
using EmmentalerModel;

namespace RacingGameAI
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

            foreach (var agent in GameController.Agents.Where(x => x.IsAlive))
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

                g.FillPolygon(new SolidBrush(agent.Color), carPoints.Select(x => new PointF(x.X, x.Y)).ToArray());

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
            float speedsAvgOfTopAgent = GameController.Agents.Where(x => x.IsAlive).Any() ? GameController.Agents.Where(x => x.IsAlive).OrderByDescending(x => x.Score).First().Speeds.Average() : 0;
            g.DrawString($"Top agent speed: {speedsAvgOfTopAgent}", new Font("Arial", 12), Brushes.White, new PointF(10, 50));
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

        public void InitializeAgents()
        {
            for (int i = 0; i < agents.Length; i++)
            {
                agents[i] = new GameAgent(GameController);
                emmentalers[i] = new Emmentaler(20, 4, new int[] { 30, 30 });
                GameController.AddAgent(agents[i]);
            }
        }

        public void Evolution()
        {
            GameAgent[] topAgents = agents.OrderByDescending(x => x.Score).Take(5).ToArray();

            for (int i = 0; i < topAgents.Length; i++)
            {
                for (int x = 0; x < 20; x++)
                {
                    emmentalers[i * 20 + x] = new Emmentaler(emmentalers[i]);
                    agents[i * 20 + x].Restart();
                }
            }

            GameController.GenerateTrack(100, Random.Shared.Next());
        }

        public void Loop()
        {
            InitializeAgents();

            Stopwatch w = new Stopwatch();
            while (Running)
            {
                if (agents.All(x => !x.IsAlive))
                {
                    Evolution();
                }

                w.Stop();
                GameController.Update(/*(float)w.Elapsed.TotalSeconds*/0.1f);
                w.Restart();
                // lerp camera
                CameraPosition = Vector2.Lerp(CameraPosition, agents.Where(x => x.IsAlive).Any() ? agents.Where(x => x.IsAlive).OrderByDescending(x => x.Score).First().Position : new Vector2(), 0.1f);

                //// update the neural network
                //var res = emmentaler.Predict(GameAgent.State);

                //// update the agent
                //GameAgent.ForwardControl = res[0] > 0.5;
                //GameAgent.BackwardControl = res[1] > 0.5;
                //GameAgent.LeftControl = res[2] > 0.5;
                //GameAgent.RightControl = res[3] > 0.5;

                Parallel.For(0, agents.Length, (i) =>
                {
                    if (agents[i].IsAlive == false)
                    {
                        return;
                    }

                    var res = emmentalers[i].Predict(agents[i].State);

                    if (res != null)
                    {
                        agents[i].ForwardControl = res[0] > 0.5;
                        agents[i].BackwardControl = res[1] > 0.5;
                        agents[i].LeftControl = res[2] > 0.5;
                        agents[i].RightControl = res[3] > 0.5;
                    }
                });

                //for (int i = 0; i < agents.Length; i++)
                //{
                    
                //}

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
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            GameController.GenerateTrack(100, 0);

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
