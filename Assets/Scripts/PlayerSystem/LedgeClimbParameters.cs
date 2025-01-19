using UnityEngine;

namespace Opus
{
    [CreateAssetMenu(fileName = "LedgeClimbParameters", menuName = "Scriptable Objects/LedgeClimbParameters")]
    public class LedgeClimbParameters : ScriptableObject
    {
        public AnimationCurve verticalPath, lateralPath;
        public float climbSpeed, vaultDistance;
        public Vector3 climbCheckBounds;
        public Vector3 climbCheckOffset;
        public float maxVaultHeight;
        public float lateralPositionForwardOffset;
        public bool canEjectFromClimb;
        public float minEjectTime, maxEjectTime;
        public float ejectRearForce, ejectUpForce;

        public float vaultEnableTime;
        public bool debugMode;
    }
}
