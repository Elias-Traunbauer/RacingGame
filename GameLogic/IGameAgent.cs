using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace GameLogic
{
    public interface IGameAgent
    {
        public Guid Id { get; }

        public Color Color { get; set; }

        public GameController GameController { get; set; }

        public bool ForwardControl { get; set; }
        public bool BackwardControl { get; set; }
        public bool LeftControl { get; set; }
        public bool RightControl { get; set; }

        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public float Rotation { get; set; }
        public float SteeringAngle { get; set; }

        public Vector2 StartPosition { get; set; }

        public float MaxSpeed { get; set; }
        public float MaxSteeringAngle { get; set; }

        public Vector2 FrontCarPosition { get; set; }

        public float Acceleration { get; set; }
        public float SteeringSpeed { get; set; }

        public float[] State { get; set; }

        public bool IsAlive { get; set; }

        public void Reset();

        public void Restart();

        public int Lifetime { get; set; }
        public Queue<float> Speeds { get; set; }
        public Queue<float> AverageSpeeds { get; set; }

        public float Distance { get; set; }

        public float FinalScore { get; set; }

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

        public struct Experience
        {
            public float[] State { get; set; }
            public float[] Actions { get; set; }
            public float Reward { get; set; }
            public float[] NextState { get; set; }
        }

        public float GetReward();

        public Queue<Vector2> Velocities { get; set; }

        public void UpdateState();

        public static float RadToDeg(float rad)
        {
            return rad * 180 / (float)Math.PI;
        }

        public static float DegToRad(float deg)
        {
            return deg * (float)Math.PI / 180;
        }

        public void Update(float deltaTime);
        public Vector2 SteeringDirection { get; set; }
        public Vector2 FrontOfCarPositionV { get; set; }
        public Vector2 BackOfCarPositionV { get; set; }
    }
}
