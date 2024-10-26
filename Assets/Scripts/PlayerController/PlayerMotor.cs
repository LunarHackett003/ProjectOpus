using Unity.Netcode;
using UnityEngine;

namespace Opus
{
    public class PlayerMotor : MonoBehaviour
    {
        int stepsSinceLastGrounded;
        Vector3 groundNormal;
        bool grounded;
        Vector3 velocity;
        Rigidbody rb;
        public float groundStickDistance, groundCheckDistance = 1.05f;
        public Vector3 groundNormalCheckOffset;
        public int groundNormalCheckIterations;
        
        private void Start()
        {
            rb = GetComponent<Rigidbody>();
        }
        void GroundCheck()
        {
            Debug.DrawRay(transform.position, Vector3.down * groundCheckDistance, Color.green, 0.25f);
            grounded = Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, groundCheckDistance);
            groundNormal = grounded ? hit.normal : Vector3.up;
        }
        bool SnapToGround()
        {
            if (stepsSinceLastGrounded > 1)
                return false;
            Debug.DrawRay(transform.position, Vector3.down * groundStickDistance, Color.red, .25f);
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, groundStickDistance))
            {
                if(hit.normal.y > 0.3f)
                {
                    float speed = velocity.magnitude;
                    float dot = Vector3.Dot(velocity, hit.normal);
                    velocity = (velocity - hit.normal * dot).normalized * speed;
                    rb.linearVelocity = velocity;
                    rb.MovePosition(hit.point + Vector3.up);
                    return true;
                }
            }
            return false;
        }

        private void FixedUpdate()
        {
            GroundCheck();
            velocity = rb.linearVelocity;
            if (grounded || SnapToGround())
            {
                stepsSinceLastGrounded = 0;
            }
            else
            {
                stepsSinceLastGrounded++;
            }
        }
    }
}
