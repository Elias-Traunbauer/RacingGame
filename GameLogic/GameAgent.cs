﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace GameLogic
{
    public class GameAgent
    {
        public Guid Id { get; private set; }

        public GameAgent(GameController gameController)
        {
            Id = Guid.NewGuid();

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

        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public float Rotation { get; set; }
        public float SteeringAngle { get; set; }

        public Vector2 StartPosition { get; set; } = new Vector2(30, 0);

        public float MaxSpeed { get; set; } = 300;
        public float MaxSteeringAngle { get; set; } = 30 * (float)Math.PI / 180;

        public Vector2 FrontCarPosition { get; set; } = new Vector2(0, 30);

        public float Acceleration { get; set; } = 70f;
        public float SteeringSpeed { get; set; } = 1f;

        public float[] State { get; set; } = new float[15 + 2 + 2 + 1];

        public void Reset()
        {
            Position = StartPosition;
            Velocity = Vector2.Zero;
            Rotation = 0;
            SteeringAngle = 0;
        }

        public void UpdateState()
        {
            var rotationInRad = Rotation;
            var steeringAngleInRad = SteeringAngle;

            int index = 0;

            State[index++] = Velocity.Length() / MaxSpeed;

            State[index++] = (float)Math.Sin(rotationInRad);
            State[index++] = (float)Math.Sin(rotationInRad);

            State[index++] = (float)Math.Sin(steeringAngleInRad);
            State[index++] = (float)Math.Sin(steeringAngleInRad);

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

            float maxDistance = 500;

            foreach (var item in rayDirections)
            {
                var hit = GameController.Raycast(agentFront, item, out float distance);

                State[index++] = hit ? distance / maxDistance : 1;
            }
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

        public override string ToString()
        {
            return Id.ToString();
        }
    }
}
