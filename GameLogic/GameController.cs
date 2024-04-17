using System;
using System.Numerics;

namespace GameLogic
{
    public class GameController
    {
        private const int TrackResolutionModifier = 3;
        public HashSet<IGameAgent> Agents = [];
        public HashSet<Vector2> Track = [];
        public HashSet<Vector2> TrackRaw = [];

        public int TrackLength { get; set; }

        public int TrackWidth { get; set; } = 180;

        public int TrackMaxDeltaX { get; set; } = 300;
        public int TrackMaxDeltaY { get; set; } = 400;

        public void AddAgent(IGameAgent agent)
        {
            Agents.Add(agent);
        }

        public void RemoveAgent(IGameAgent agent)
        {
            Agents.Remove(agent);
        }

        public void ClearAgents()
        {
            Agents.Clear();
        }

        public int GetAgentCount()
        {
            return Agents.Count;
        }

        public void Update(float deltaTime)
        {
            rays.Clear();
            float minDistance = 10;
            foreach (IGameAgent agent in Agents)
            {
                agent.Update(deltaTime);

                Vector2 agentDirection = new((float)Math.Cos(agent.Rotation), (float)Math.Sin(agent.Rotation));
                agentDirection = new Vector2(agentDirection.Y, agentDirection.X);
                Vector2 agentDirectionNormal = new Vector2(agentDirection.Y, -agentDirection.X);
                Vector2 agentDirectionNormalOther = new Vector2(-agentDirection.Y, agentDirection.X);

                Vector2 Rotate(Vector2 v, float angle)
                {
                    angle = DegToRad(angle);
                    float sin = (float)Math.Sin(angle);
                    float cos = (float)Math.Cos(angle);
                    return new Vector2(v.X * cos - v.Y * sin, v.X * sin + v.Y * cos);
                }

                float DegToRad(float deg)
                {
                    return deg * (float)Math.PI / 180;
                }

                Vector2[] rayDirections = [

                    agentDirection,

                    Rotate(agentDirection, 10),
                    Rotate(agentDirection, -10),

                    Rotate(agentDirection, 20),
                    Rotate(agentDirection, -20),

                    Rotate(agentDirection, 30),
                    Rotate(agentDirection, -30),

                    Rotate(agentDirection, 45),
                    Rotate(agentDirection, -45),

                    Rotate(agentDirection, 60),
                    Rotate(agentDirection, -60),

                    Rotate(agentDirection, 75),
                    Rotate(agentDirection, -75),
                ];

                Vector2 agentFront = agent.Position + agentDirection * agent.FrontCarPosition.Y;

                if (agent.IsAlive)
                {
                    //foreach (var item in rayDirections)
                    //{
                    //    bool hit = Raycast(agentFront, item, out float distance);

                    //    rays.Add((agentFront, agentFront + item * distance));
                    //}
                }

                bool hitRight = Raycast(agentFront, agentDirectionNormal, out float distanceRight);
                bool hitLeft = Raycast(agentFront, agentDirectionNormalOther, out float distanceLeft);

                //rays.Add((agentFront, agentFront + agentDirection * 100));

                //rays.Add((agentFront, agentFront + Vector2.Normalize(agentDirectionNormal) * (hitRight? distanceRight : 1000)));

                //rays.Add((agentFront, agentFront + Vector2.Normalize(agentDirectionNormalOther) * (hitLeft ? distanceLeft : 1000)));

                if (hitRight && distanceRight < minDistance)
                {
                    agent.Die();
                    //GenerateTrack(100, Random.Shared.Next());
                }

                if (hitLeft && distanceLeft < minDistance)
                {
                    agent.Die();
                    //GenerateTrack(100, Random.Shared.Next());
                }

                if (!hitLeft || !hitRight)
                {
                    agent.Die();
                    //GenerateTrack(100, Random.Shared.Next());
                }
            }
        }

        public HashSet<(Vector2, Vector2)> rays = new();

        public void GenerateTrack(int length, int seed)
        {
            var rnd = new Random(seed);
            TrackLength = length;
            Track.Clear();
            Vector2 pointBeforeLastPoint = new Vector2(0, -10);
            Vector2 lastPoint = new Vector2(0, 0);
            Track.Add(lastPoint);
            
            for (int i = 0; i < length; i++)
            {
                int deltaX = rnd.Next(-TrackMaxDeltaX, TrackMaxDeltaX + 1);
                int deltaY = (int)Math.Pow(
                    rnd.Next((int)Math.Sqrt(Math.Abs(deltaX)), Math.Max(TrackMaxDeltaY + 1, deltaX) + 1)
                    , 2);
                deltaY = TrackMaxDeltaY;

                Vector2 newPoint = lastPoint + new Vector2(deltaX, deltaY);

                int segments = (int)(new Vector2(deltaX, deltaY).Length() / TrackResolutionModifier);

                Vector2 fromLastPointToHalf = Vector2.Normalize(lastPoint - pointBeforeLastPoint) * new Vector2(deltaX, deltaY).Length() / 2;
                Vector2 half = fromLastPointToHalf + lastPoint;

                for (int j = 0; j < segments; j++)
                {
                    Vector2 a, b;
                    a = Vector2.Lerp(lastPoint, half, (float)j / segments);
                    b = Vector2.Lerp(half, newPoint, (float)j / segments);
                    Track.Add(Vector2.Lerp(a, b, (float)j / segments));
                }
                TrackRaw.Add(newPoint);
                pointBeforeLastPoint = Track.Last();
                lastPoint = newPoint;
            }
        }

        public Vector2[] TrackWithResolutionModifier(int resolutionModifier)
        {
            Vector2[] track = Track.ToArray();
            HashSet<Vector2> trackPoints = new HashSet<Vector2>();

            for (int i = 0; i < track.Length; i++)
            {
                if (i % resolutionModifier == 0)
                {
                    trackPoints.Add(track[i]);
                }
            }

            return trackPoints.ToArray();
        }

        public Vector2[] TrackWithResolutionModifierAndDistance(int resolutionModifier, float maxDistance, Vector2 origin)
        {
            Vector2[] track = Track.Where(x => Vector2.Distance(origin, x) < maxDistance).ToArray();
            HashSet<Vector2> trackPoints = new HashSet<Vector2>();

            for (int i = 0; i < track.Length; i++)
            {
                if (i % resolutionModifier == 0)
                {
                    trackPoints.Add(track[i]);
                }
            }

            return trackPoints.ToArray();
        }

        public bool Raycast(Vector2 origin, Vector2 direction, out float distance)
        {
            bool hit = false;
            float maxDistance = TrackWidth * 4f;
            int resolutionModifier = 10;
            // collision detection for track bounds
            float minDistance = maxDistance;

            Vector2[] trackPointsArray = TrackWithResolutionModifierAndDistance(resolutionModifier, maxDistance, origin);

            for (int i = 0; i < trackPointsArray.Length - 1; i++)
            {
                Vector2 a1 = trackPointsArray[i];
                Vector2 b1 = trackPointsArray[i + 1];

                Vector2 a2 = trackPointsArray[i] + new Vector2(TrackWidth, 0);
                Vector2 b2 = trackPointsArray[i + 1] + new Vector2(TrackWidth, 0);

                //rays.Add((a1, b1));
                //rays.Add((a2, b2));

                bool hit1 = RaycastLine(origin, direction, a1, b1, out Vector2 intersection1);
                bool hit2 = RaycastLine(origin, direction, a2, b2, out Vector2 intersection2);

                if (hit1)
                {
                    minDistance = Math.Min(minDistance, (origin - intersection1).Length());
                }

                if (hit2)
                {
                    minDistance = Math.Min(minDistance, (origin - intersection2).Length());
                }

                hit = hit1 || hit2 || hit;
            }

            distance = minDistance;
            return hit;
        }

        private bool RaycastLine(Vector2 origin, Vector2 direction, Vector2 a, Vector2 b, out Vector2 intersection)
        {
            // Normalize the direction vector
            direction = Vector2.Normalize(direction);

            Vector2 a_b = b - a;
            Vector2 origin_a = a - origin;
            float denominator = Cross(direction, a_b);

            intersection = new Vector2();

            // Check for parallel or coincident lines
            if (Math.Abs(denominator) < 1e-10)
            {
                return false;
            }

            float t = Cross(origin_a, a_b) / denominator;
            float u = Cross(origin_a, direction) / denominator;

            // Check if intersection occurs within the line segment and in the ray's direction
            if (0 <= u && u <= 1 && t > 0)
            {
                intersection = origin + t * direction;
                return true;
            }

            return false;
        }

        // vector2 cross product
        private float Cross(Vector2 a, Vector2 b)
        {
            return a.X * b.Y - a.Y * b.X;
        }
    }
}
