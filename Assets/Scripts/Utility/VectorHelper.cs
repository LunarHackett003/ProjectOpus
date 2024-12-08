using UnityEngine;

namespace Opus
{
    public static class VectorHelper 
    {
        public static Vector3 ClampMagnitude(this Vector3 vec, float maxMagnitude)
        {
            return Vector3.ClampMagnitude(vec, maxMagnitude);
        }
    }
}
