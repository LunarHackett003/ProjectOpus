using UnityEngine;
namespace opus.utility
{
    public static class UtilityMethods
    {
        public static float SquaredDistance(Vector3 a, Vector3 b)
        {
            return (b - a).sqrMagnitude;
        }
    }
}