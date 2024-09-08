using UnityEngine;

namespace Opus
{
    public class JumpPad : MonoBehaviour
    {
        public float jumpForce;
        public Transform jumpDirection;
        public float trajectoryPreviewTime = 10, trajectoryPreviewDelta = 0.1f;
        public Vector3[] points = new Vector3[0];
        private void OnTriggerEnter(Collider other)
        {
            if (other.attachedRigidbody)
            {
                other.attachedRigidbody.AddForce(jumpDirection.up * jumpForce, ForceMode.VelocityChange);
                if (other.attachedRigidbody.TryGetComponent(out PlayerMotor pm))
                {
                    pm.SendJump();
                }
            }
        }
        [ContextMenu("Calculate Points")]
        void CalculatePoints()
        {
            points = MathFunctions.TrajectoryPoints(jumpDirection.position, jumpDirection.up, trajectoryPreviewTime, trajectoryPreviewDelta, 1, jumpForce);
        }
        private void OnDrawGizmosSelected()
        {
            if(points.Length > 0)
            {
                Gizmos.DrawLineList(points);
            }
        }
    }
}
