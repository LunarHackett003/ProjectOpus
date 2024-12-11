using UnityEngine;

namespace Opus
{
    public static class MathHelper 
    {
        public static Vector3 ClampMagnitude(this Vector3 vec, float maxMagnitude)
        {
            return Vector3.ClampMagnitude(vec, maxMagnitude);
        }
        public static float Clamp(this float f, float min, float max)
        {
            return Mathf.Clamp(f, min, max);
        }
    }
}
