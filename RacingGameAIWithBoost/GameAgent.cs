using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using GameLogic;
using static GameLogic.IGameAgent;

namespace RacingGameAIWithBoost
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

        public Color Color { get; set; } = Color.Red;

        public GameController GameController { get; set; }

        public bool ForwardControl { get; set; }
        public bool BackwardControl { get; set; }
        public bool LeftControl { get; set; }
        public bool RightControl { get; set; }
        public bool BoostControl { get; set; }

        public float CurrentBoost { get; set; } = 50f;
        public float MaxBoost { get; set; } = 50f;
        public float BoostGrowRate { get; set; } = 1.1f;

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

        public float[] State { get; set; } = new float[15 + 2 + 2 + 1 + 1];

        public bool IsAlive { get; set; } = true;

        public int[] InputCounts { get; set; } = new int[] { 0, 0, 0, 0, 0 };

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
            CurrentBoost = 10;

            InputCounts = new int[] { 0, 0, 0, 0, 0 };

            //Rotation = (float)((MaxSteeringAngle / 2) - Random.Shared.NextDouble() * 2 * (MaxSteeringAngle / 2));
            PreviousForwardControl = false;
            PreviousBackwardControl = false;
            PreviousLeftControl = false;
            PreviousRightControl = false;
            PreviousBoostControl = false;

            Position = StartPosition + new Vector2(Random.Shared.Next(0, GameController.TrackWidth / 2), 0);
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

        public bool PreviousForwardControl { get; set; }
        public bool PreviousBackwardControl { get; set; }
        public bool PreviousLeftControl { get; set; }
        public bool PreviousRightControl { get; set; }
        public bool PreviousBoostControl { get; set; }

        public float GetReward()
        {
            float reward = 0;

            if (InputCounts.Any(count => count == 0))
            {
                reward *= 0.5f; // Apply penalty less drastically
            }

            // Reward for progress and maintaining speed
            reward += (Position.Y / (GameController.TrackLength * GameController.TrackMaxDeltaY));
            float avgSpeed = Velocities.Count != 0 ? Velocities.Average(v => v.Length()) : 0;
            reward += avgSpeed / (MaxSpeed * 2);

            // Encourage using all inputs effectively
            float inputUsageReward = 0;
            foreach (int count in InputCounts)
            {
                inputUsageReward += (Math.Clamp(count, 0, 100) / 100.0f);
            }
            inputUsageReward = inputUsageReward / InputCounts.Length; // Normalize by number of inputs

            // Adjust reward calculations
            reward += reward * inputUsageReward * 1.3f; // Scaled importance of input usage

            return reward;
        }

        public bool HasBoost {  get; set; }

        public Queue<Vector2> Velocities { get; set; } = new Queue<Vector2>();

        public void UpdateState()
        {
            FinalScore = GetReward();
            var rotationInRad = Rotation;
            var steeringAngleInRad = SteeringAngle;

            int index = 0;

            State[index++] = CurrentBoost / MaxBoost;

            State[index++] = Velocity.Length() / (MaxSpeed * 2);

            //State[index++] = (float)Math.Sin(rotationInRad);
            //State[index++] = (float)Math.Sin(rotationInRad);
            State[index++] = 0.5f;
            State[index++] = 0.5f;

            float minSteeringRad = -MaxSteeringAngle;
            float maxSteeringRad = MaxSteeringAngle;

            float steeringAngleNormalized = (steeringAngleInRad - minSteeringRad) / (maxSteeringRad - minSteeringRad);

            State[index++] = steeringAngleNormalized;
            State[index++] = HasBoost ? 1 : 0;

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
                Rotate(agentDirection, -90),
                Rotate(agentDirection, -75),
                Rotate(agentDirection, -60),
                Rotate(agentDirection, -45),
                Rotate(agentDirection, -30),
                Rotate(agentDirection, -20),
                Rotate(agentDirection, -10),
                agentDirection,
                Rotate(agentDirection, 10),
                Rotate(agentDirection, 20),
                Rotate(agentDirection, 30),
                Rotate(agentDirection, 45),
                Rotate(agentDirection, 60),
                Rotate(agentDirection, 75),
                Rotate(agentDirection, 90),
            ];

            float maxDistance = GameController.TrackWidth * 4;

            foreach (var item in rayDirections)
            {
                var hit = GameController.Raycast(agentFront, item, out float distance);

                State[index++] = hit ? distance / maxDistance : 1;
            }

            Experience experience = new Experience
            {
                State = State,
                Actions = new float[] { ForwardControl ? 1 : 0, BackwardControl ? 1 : 0, LeftControl ? 1 : 0, RightControl ? 1 : 0, BoostControl ? 1 : 0 }
            };

            AddExperience(experience);

            Lifetime++;
            Velocities.Enqueue(Velocity);
            Speeds.Enqueue(Velocity.Y);

            if (Speeds.Count > 100)
            {
                Speeds.Dequeue();
            }

            AverageSpeeds.Enqueue(Speeds.Average());

            if (ForwardControl && !PreviousForwardControl)
            {
                InputCounts[0]++;
            }
            if (BackwardControl && !PreviousBackwardControl)
            {
                InputCounts[1]++;
            }
            if (LeftControl && !PreviousLeftControl)
            {
                InputCounts[2]++;
            }
            if (RightControl && !PreviousRightControl)
            {
                InputCounts[3]++;
            }
            if (BoostControl && !PreviousBoostControl)
            {
                InputCounts[4]++;
            }

            PreviousLeftControl = LeftControl;
            PreviousRightControl = RightControl;
            PreviousForwardControl = ForwardControl;
            PreviousBackwardControl = BackwardControl;
            PreviousBoostControl = BoostControl;
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

            if ((Speeds.Count != 0 && Speeds.Average() < MaxSpeed / 50) && Lifetime > 100)
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

            if (BoostControl)
            {
                if (CurrentBoost > 0)
                {
                    HasBoost = true;
                    accelerationNow *= 2;
                    CurrentBoost -= 20 * deltaTime;

                    if (CurrentBoost < 0)
                    {
                        CurrentBoost = 0;
                    }
                }
                else
                {
                    HasBoost = false;
                }
            }
            else
            {
                HasBoost = false;
                CurrentBoost += CurrentBoost * BoostGrowRate * BoostGrowRate * deltaTime + 3f * deltaTime;

                if (CurrentBoost < 0)
                {
                    CurrentBoost = 0;
                }

                if (CurrentBoost > MaxBoost)
                {
                    CurrentBoost = MaxBoost;
                }
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
                if (SteeringAngle > 0)
                {
                    SteeringAngle -= SteeringSpeed * deltaTime;
                }
                if (SteeringAngle < 0)
                {
                    SteeringAngle += SteeringSpeed * deltaTime;
                }

                if (Math.Abs(SteeringAngle) < SteeringSpeed * deltaTime)
                {
                    SteeringAngle = 0;
                }
            }

            if (Velocity.Length() > (HasBoost ? MaxSpeed * 2 : MaxSpeed))
            {
                // lerp towards max speed
                Velocity = Vector2.Lerp(Velocity, Vector2.Normalize(Velocity) * (HasBoost ? MaxSpeed * 2 : MaxSpeed), 0.1f);
            }

            if (Velocity.Y < -50)
            {
                Velocity = new Vector2(0, -50);
            }

            SteeringAngle = Math.Clamp(SteeringAngle, -MaxSteeringAngle, MaxSteeringAngle);
            float originalSteeringAngle = SteeringAngle;
            //if (HasBoost && Velocity.Length() > MaxSpeed)
            //{
            //    SteeringAngle /= 2;
            //}
            float Remap(float value, float from1, float to1, float from2, float to2)
            {
                return Math.Clamp((value - from1) / (to1 - from1) * (to2 - from2) + from2, from2, to2);
            }


            float steeringReduction = Remap(Velocity.Length(), MaxSpeed / 2, MaxSpeed, 0, 0.5f);

            SteeringAngle *= (1 - steeringReduction);

            //Vector2 carDirection = new((float)Math.Cos(Rotation), (float)Math.Sin(Rotation));
            //carDirection = new Vector2(carDirection.Y, carDirection.X);

            Vector2 steeringDirection = new Vector2((float)Math.Cos(Rotation + SteeringAngle), (float)Math.Sin(Rotation + SteeringAngle));
            steeringDirection = new Vector2(steeringDirection.Y, steeringDirection.X);

            Velocity += accelerationNow/* * deltaTime*/;

            
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

            SteeringAngle = originalSteeringAngle;

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

        public override string ToString()
        {
            return Id.ToString();
        }
    }
}
