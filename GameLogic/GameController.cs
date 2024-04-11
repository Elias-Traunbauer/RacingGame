using System.Numerics;

namespace GameLogic
{
    public class GameController
    {
        public HashSet<GameAgent> Agents = [];
        public HashSet<Vector2> Track = [];
        public HashSet<Vector2> TrackRaw = [];

        public int TrackLength { get; set; }

        public int TrackWidth { get; set; } = 200;

        public int TrackMaxDeltaX { get; set; } = 300;
        public int TrackMaxDeltaY { get; set; } = 20;

        public void AddAgent(GameAgent agent)
        {
            Agents.Add(agent);
        }

        public void RemoveAgent(GameAgent agent)
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
            float minDistance = 100;
            foreach (GameAgent agent in Agents)
            {
                agent.Update(deltaTime);

                Vector2 agentDirection = new Vector2((float)Math.Cos(agent.Rotation), (float)Math.Sin(agent.Rotation));
                Vector2 agentDirectionNormal = new Vector2(agentDirection.Y, -agentDirection.X);
                Vector2 agentDirectionNormalOther = new Vector2(-agentDirection.Y, agentDirection.X);

                Vector2 agentFront = agent.Position + agentDirection * agent.FrontCarPosition.Y;

                bool hitRight = Raycast(agentFront, agentDirectionNormal, out float distanceRight);
                bool hitLeft = Raycast(agentFront, agentDirectionNormalOther, out float distanceLeft);

                if (hitRight && distanceRight < minDistance)
                {
                    //agent.Reset();
                }

                if (hitLeft && distanceLeft < minDistance)
                {
                    //agent.Reset();
                }

                if (!hitLeft || !hitRight)
                {
                    //agent.Reset();
                }
            }
        }

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
                deltaY = 400;

                Vector2 newPoint = lastPoint + new Vector2(deltaX, deltaY);

                int segments = (int)(newPoint.Length() / 10);

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
            float maxDistance = 500;
            int resolutionModifier = 100;
            // collision detection for track bounds
            float minDistance = float.MaxValue;

            Vector2[] trackPointsArray = TrackWithResolutionModifierAndDistance(resolutionModifier, maxDistance, origin);

            for (int i = 0; i < trackPointsArray.Length - 1; i++)
            {
                Vector2 a1 = trackPointsArray[i];
                Vector2 b1 = trackPointsArray[i + 1];

                Vector2 a2 = trackPointsArray[i] + new Vector2(TrackWidth, 0);
                Vector2 b2 = trackPointsArray[i + 1] + new Vector2(TrackWidth, 0);

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
            intersection = Vector2.Zero;
            Vector2 ray = direction;
            Vector2 rayOrigin = origin;
            Vector2 line = b - a;
            Vector2 lineOrigin = a;

            float cross = ray.X * line.Y - ray.Y * line.X;
            if (cross == 0)
            {
                return false;
            }

            float t = ((lineOrigin.Y - rayOrigin.Y) * line.X + (rayOrigin.X - lineOrigin.X) * line.Y) / cross;
            float u = ((rayOrigin.Y - lineOrigin.Y) * ray.X + (lineOrigin.X - rayOrigin.X) * ray.Y) / cross;

            if (t >= 0 && u >= 0 && u <= 1)
            {
                intersection = lineOrigin + t * line;
                return true;
            }
            return false;
        }
    }
}
