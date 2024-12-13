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
        public static Vector3 ScaleReturn(this Vector3 vec, Vector3 scale)
        {
            return new(vec.x * scale.x, vec.y * scale.y, vec.z * scale.z);
        }
    }

    [System.Serializable]
    public struct VectorAnimationCurve
    {
        public AnimationCurve x, y, z;
        public readonly Vector3 Evaluate(float t)
        {
            return new(x.Evaluate(t), y.Evaluate(t), z.Evaluate(t));
        }
    }
}
