using UnityEngine;

namespace Opus
{
    [CreateAssetMenu(fileName = "SwayParameters", menuName = "Scriptable Objects/SwayParameters")]
    public class SwayParameters : ScriptableObject
    {
        public Vector3 lookSwayPosScale, lookSwayEulerScale;
        public float lookSwayPosDampTime, lookSwayEulerDampTime, maxLookSwayPos, maxLookSwayEuler;

        public Vector3 moveSwayPosScale, moveSwayEulerScale;
        public float moveSwayPosDampTime, moveSwayEulerDampTime, maxMoveSwayPos, maxMoveSwayEuler;

        public float verticalVelocitySwayScale, verticalVelocityEulerScale, verticalVelocitySwayPosTime, verticalVelocitySwayEulerTime, verticalVelocityPosClamp, verticalVelocityEulerClamp;

        public float jumpLerpSpeed, jumpPosLerpSpeed;
    }
}
