using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static GameLogic.IGameAgent;

namespace GameLogic
{
    public class GameAgent : IGameAgent
    {
        public Guid Id { get; private set; }

        public GameAgent(GameController gameController)
        {
            Id = Guid.NewGuid();
            Position = StartPosition;

            // assign random color
            Random random = new();
            Color = Color.FromArgb(random.Next(0, 255), random.Next(0, 255), random.Next(0, 255));
            GameController = gameController;
        }

        public override string? ToString()
        {
            return "Pos: " + Position + " Vel: " + Velocity + " Rot: " + Rotation + " Steer: " + SteeringAngle;
        }

        public Color Color { get; set; } = Color.Red;

        public GameController GameController { get; set; }

        public bool ForwardControl { get; set; }
        public bool BackwardControl { get; set; }
        public bool LeftControl { get; set; }
        public bool RightControl { get; set; }

        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public float Rotation { get; set; }
        public float SteeringAngle { get; set; }

        public Vector2 StartPosition { get; set; } = new Vector2(50, 20);

        public float MaxSpeed { get; set; } = 300;
        public float MaxSteeringAngle { get; set; } = 30 * (float)Math.PI / 180;

        public Vector2 FrontCarPosition { get; set; } = new Vector2(0, 30);

        public float Acceleration { get; set; } = 70f;
        public float SteeringSpeed { get; set; } = 1f;

        public float[] State { get; set; } = new float[15 + 2 /*+ 2*/ + 1];

        public bool IsAlive { get; set; } = true;

        public void Die()
        {
            FinalScore = GetReward();
            Distance = Position.Y;
            Velocity = Vector2.Zero/* + new Vector2(0, -100)*/;
            IsAlive = false;
        }

        public void Restart()
        {
            Position = StartPosition + new Vector2();
            Velocity = Vector2.Zero;
            Rotation = 0;
            SteeringAngle = 0;
            IsAlive = true;
            Lifetime = 0;
            Speeds.Clear();
            Velocities.Clear();

            //Rotation = (float)((MaxSteeringAngle / 2) - Random.Shared.NextDouble() * 2 * (MaxSteeringAngle / 2));

            Position = StartPosition/* + new Vector2(Random.Shared.Next(0, GameController.TrackWidth / 2), 0)*/;
        }

        public int Lifetime { get; set; }
        public Queue<float> Speeds { get; set; } = new Queue<float>();
        public Queue<float> AverageSpeeds { get; set; } = new Queue<float>();

        public float Distance { get; set; }

        public float FinalScore { get; set; } = 0;

        public float Score
        {
            get
            {
                return FinalScore;
                return GetReward() + (IsAlive ? 0 : 10);
                if (Distance <= 0)
                {
                    return 0;
                }
                return (Distance / (Lifetime / 10)) * (AverageSpeeds.Count > 0 ? AverageSpeeds.Average() : 0);
            }
        }

        public Queue<Experience> Experiences { get; set; } = new Queue<Experience>();

        public void AddExperience(Experience experience)
        {
            Experiences.Enqueue(experience);
        }

        public void ClearExperiences()
        {
            Experiences.Clear();
        }

        public Experience[] GetTrainingBatch()
        {
            var batch = Experiences.ToArray();
            Experiences.Clear();
            return batch;
        }

        public byte[] TrainingBatchToByte()
        {
            var batch = GetTrainingBatch();
            var sampleBatch = batch.First().ToBytes();
            var bytes = batch.SelectMany(x => x.ToBytes()).ToArray();
            byte[] res = new byte[sampleBatch.Length * batch.Length];

            for (int i = 0; i < batch.Length; i++)
            {
                Array.Copy(bytes, i * sampleBatch.Length, res, i * sampleBatch.Length, sampleBatch.Length);
            }

            return res;
        }

        public float GetReward()
        {
            float reward = 0;
            //reward += Velocity.Y / MaxSpeed;
            //reward -= (float)Math.Sin(Rotation);
            //reward -= IsAlive ? 0 : 1 * 10;
            reward += Position.Y / GameController.TrackLength + (Velocities.Count != 0 ? Velocities.Average(x => x.Y) : 0);
            return reward;
        }

        public Queue<Vector2> Velocities { get; set; } = new Queue<Vector2>();

        public void UpdateState()
        {
            FinalScore = GetReward();
            var rotationInRad = Rotation;
            var steeringAngleInRad = SteeringAngle;

            int index = 0;

            State[index++] = Velocity.Length() / MaxSpeed;

            float steeringRight = steeringAngleInRad > 0 ? steeringAngleInRad / MaxSteeringAngle : 0;
            float steeringLeft = steeringAngleInRad < 0 ? -steeringAngleInRad / MaxSteeringAngle : 0;

            State[index++] = steeringRight;
            State[index++] = steeringLeft;

            float minSteeringRad = -MaxSteeringAngle;
            float maxSteeringRad = MaxSteeringAngle;

            Vector2 agentDirection = new((float)Math.Cos(Rotation), (float)Math.Sin(Rotation));
            agentDirection = new Vector2(agentDirection.Y, agentDirection.X);

            Vector2 agentFront = Position + agentDirection * FrontCarPosition.Y;

            static Vector2 Rotate(Vector2 v, float angle)
            {
                angle = DegToRad(angle);
                float sin = (float)Math.Sin(angle);
                float cos = (float)Math.Cos(angle);
                return new Vector2(v.X * cos - v.Y * sin, v.X * sin + v.Y * cos);
            }

            Vector2[] rayDirections = [

                agentDirection,

                Rotate(agentDirection, 10),
                Rotate(agentDirection,-10),

                Rotate(agentDirection, 20),
                Rotate(agentDirection,-20),

                Rotate(agentDirection, 30),
                Rotate(agentDirection,-30),

                Rotate(agentDirection, 45),
                Rotate(agentDirection,-45),

                Rotate(agentDirection, 60),
                Rotate(agentDirection,-60),

                Rotate(agentDirection, 75),
                Rotate(agentDirection,-75),

                Rotate(agentDirection, 90),
                Rotate(agentDirection,-90),
            ];

            float maxDistance = GameController.TrackWidth * 2;
            
            foreach (var item in rayDirections)
            {
                var hit = GameController.Raycast(agentFront, item, out float distance);

                State[index++] = hit ? distance / maxDistance : 1;
            }

            Lifetime++;
            Velocities.Enqueue(Velocity);
            Speeds.Enqueue(Velocity.Y);

            if (Speeds.Count > 100)
            {
                Speeds.Dequeue();
            }

            AverageSpeeds.Enqueue(Speeds.Average());
        }

        public static float RadToDeg(float rad)
        {
            return rad * 180 / (float)Math.PI;
        }

        public static float DegToRad(float deg)
        {
            return deg * (float)Math.PI / 180;
        }

        public void Update(float deltaTime)
        {
            if (!IsAlive)
            {
                return;
            }

            if ((Speeds.Count != 0 && Speeds.Average() < MaxSpeed / 20) && Lifetime > 100)
            {
                IsAlive = false;
            }

            Vector2 accelerationNow = Vector2.Zero;

            if (ForwardControl)
            {
                accelerationNow += new Vector2(0, Acceleration) * deltaTime;
            }
            if (BackwardControl)
            {
                accelerationNow += new Vector2(0, -Acceleration) * 3 * deltaTime;
            }

            if (!ForwardControl && !BackwardControl)
            {
                if (Velocity.Length() > 0)
                {
                    accelerationNow = -Vector2.Normalize(Velocity) * Acceleration * deltaTime;
                }
            }

            if (LeftControl)
            {
                SteeringAngle -= SteeringSpeed * deltaTime;
            }
            if (RightControl)
            {
                SteeringAngle += SteeringSpeed * deltaTime;
            }

            if (!LeftControl && !RightControl)
            {
                //if (SteeringAngle > 0)
                //{
                //    SteeringAngle -= SteeringSpeed * deltaTime;
                //}
                //if (SteeringAngle < 0)
                //{
                //    SteeringAngle += SteeringSpeed * deltaTime;
                //}

                if (Math.Abs(SteeringAngle) < SteeringSpeed * deltaTime)
                {
                    SteeringAngle = 0;
                }
            }

            SteeringAngle = Math.Clamp(SteeringAngle, -MaxSteeringAngle, MaxSteeringAngle);

            //Vector2 carDirection = new((float)Math.Cos(Rotation), (float)Math.Sin(Rotation));
            //carDirection = new Vector2(carDirection.Y, carDirection.X);

            Vector2 steeringDirection = new Vector2((float)Math.Cos(Rotation + SteeringAngle), (float)Math.Sin(Rotation + SteeringAngle));
            steeringDirection = new Vector2(steeringDirection.Y, steeringDirection.X);

            Velocity += accelerationNow/* * deltaTime*/;

            if (Velocity.Length() > MaxSpeed)
            {
                Velocity = Vector2.Normalize(Velocity) * MaxSpeed;
            }

            if (Velocity.Y < -50)
            {
                Velocity = new Vector2(0, -50);
            }

            
            Vector2 frontPositionOffsetRotated = Vector2.Transform(FrontCarPosition, Matrix3x2.CreateRotation(-Rotation));
            Vector2 previousFrontOfCarPosition = Position + frontPositionOffsetRotated;
            Vector2 oldBackOfCarPosition = Position - frontPositionOffsetRotated;

            Vector2 newFrontOfCarPosition = previousFrontOfCarPosition + steeringDirection * Velocity.Y * deltaTime;

            Vector2 newFrontToOldBack = Vector2.Normalize(oldBackOfCarPosition - newFrontOfCarPosition);

            Vector2 newBackOfCarPosition = newFrontOfCarPosition + newFrontToOldBack * FrontCarPosition.Length() * 2;

            Position = newFrontOfCarPosition + newFrontToOldBack * FrontCarPosition.Length();
            SteeringDirection = steeringDirection;

            // calculate rotation from back and front of car
            Vector2 directionOfCar = newFrontOfCarPosition - newBackOfCarPosition;
            // angle between car direction and x axis
            Rotation = -(float)Math.Atan2(directionOfCar.Y, directionOfCar.X);
            Rotation += (float)Math.PI / 2;

            UpdateState();
        }
        public Vector2 SteeringDirection { get; set; }
        public Vector2 FrontOfCarPositionV { get; set; }
        public Vector2 BackOfCarPositionV { get; set; }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override bool Equals(object? obj)
        {
            return obj is GameAgent agent && Id == agent.Id;
        }
    }
}
