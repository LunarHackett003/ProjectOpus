using UnityEngine;

namespace Opus
{
    [CreateAssetMenu(fileName = "JumpAnimCurves", menuName = "Scriptable Objects/JumpAnimCurves")]
    public class JumpAnimParameters : ScriptableObject
    {
        public float animSpeed;
        /// <summary>
        /// The maximum rotation the character's hands will reach when jumping
        /// </summary>
        public Vector3 maxRotation;
        public AnimationCurve XRotationCurve, YRotationCurve, ZRotationCurve;
        public Vector3 MaxPosition;
        public AnimationCurve XPositionCurve, YPositionCurve, ZPositionCurve;
        public float maxYVelocityOnLand;
    }
}
