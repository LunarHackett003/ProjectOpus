using UnityEngine;

namespace Opus
{
    public static class RigidbodyHelper
    {
        public static Vector3 GetRelativeVelocity(this Rigidbody rb)
        {
            return rb.transform.InverseTransformVector(rb.linearVelocity);
        }
    }
}
