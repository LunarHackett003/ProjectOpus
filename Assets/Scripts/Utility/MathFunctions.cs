using UnityEngine;

namespace Opus
{
    public static class MathFunctions
    {
        public static Vector3[] TrajectoryPoints(Vector3 start, Vector3 direction, float duration, float deltaTime, float mass, float force)
        {
            Vector3[] points = new Vector3[Mathf.FloorToInt(duration / deltaTime)];
            float velocity = force / mass;
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = start + direction * (deltaTime * i * velocity) + ((Physics.gravity * Mathf.Pow(i * deltaTime, 2)) * 0.5f);
            }
            return points;
        }
        public static float InverseLerp(Vector3 a, Vector3 b, Vector3 value)
        {
            Vector3 AB = b - a;
            Vector3 AV = value - a;
            return Vector3.Dot(AV, AB) / Vector3.Dot(AB, AB);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="referencePosition"></param>
        /// <returns>The nearest position on the line between Start and End</returns>
        public static float GetNearestPoint(Vector3 start, Vector3 end, Vector3 referencePosition)
        {
            return Mathf.Clamp01(Vector3.Distance(start, referencePosition) / Vector3.Distance(start, end));
        }
    }
}
