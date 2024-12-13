using UnityEngine;

namespace Opus
{
    [CreateAssetMenu(fileName = "SwayContainerSO", menuName = "Scriptable Objects/SwayContainerSO")]
    public class SwayContainerSO : ScriptableObject
    {
        public VectorAnimationCurve linearMoveBob, angularMoveBob;
        public Vector3 linearMoveScale, angularMoveScale;

        public float speed;
        public float sprintMultiplier = 1.2f;
    }
}
