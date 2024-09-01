using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

namespace Opus
{
    public class PlayerMotor : NetworkBehaviour
    {
        public Rigidbody rb;
        public InputCollector ic;
        public Vector3 velocity;
        Vector3 movementVelocity;
        Vector3 movementVelocityDamping;
        public Vector2 groundMoveSpeed, airMoveSpeed;
        public float jumpSpeed, jumpLateralForce;
        public float groundDrag, airDrag;
        public LayerMask groundMask;
        public Transform head;
        Vector3 groundNormal;

        public Vector3 groundCheckOrigin;
        public float groundCheckDistance;

        public float gravitySpeed;
        public float gravityAcceleration = -9.81f;
        bool grounded;

        public enum MovementState
        {
            none = 0,
            zipline = 1,
            ladder = 2
        }
        public MovementState moveState;
        public ZiplineMotor zipMotor;
        public override void OnSpawnServer(NetworkConnection connection)
        {
            base.OnSpawnServer(connection);
            if (!IsOwner)
            {
                enabled = false;
                return;
            }
            ic = GetComponent<InputCollector>();
        }
        private void Start()
        {
            ic = GetComponent<InputCollector>();
        }

        private void Update()
        {
            head.localRotation = Quaternion.Euler(-ic.lookInput.y, 0, 0);
            transform.rotation = Quaternion.Euler(0, ic.lookInput.x, 0);

            if(ic.TryConsumeJump())
            {
                TryJump();
            }
        }
        private void FixedUpdate()
        {
            grounded = CheckGround();
            rb.linearDamping = grounded ? groundDrag : airDrag;
            if (zipMotor.currentZipline != null)
                moveState = MovementState.zipline;
            if (moveState == MovementState.none)
            {
                rb.isKinematic = false; 
                if (grounded)
                {
                    Vector3 gravityCounter = -Vector3.ProjectOnPlane(Physics.gravity, groundNormal);
                    rb.AddForce(gravityCounter, ForceMode.Acceleration);
                }
                RegularMovement();
            }
            else
            {
                rb.isKinematic = true;
            }
        }
        void RegularMovement()
        {
            movementVelocity = transform.TransformDirection(ic.moveInput.x * (grounded ? groundMoveSpeed.x : airMoveSpeed.x),
                0, ic.moveInput.y * (grounded ? groundMoveSpeed.y : airMoveSpeed.y));
            //Move using our movement vector
            rb.AddForce(Vector3.ProjectOnPlane(movementVelocity, groundNormal));
        }
        void TryJump()
        {
            if (zipMotor.currentZipline)
            {
                zipMotor.Detach();
            }
            else if(grounded)
            {
                rb.AddForce((Vector3.up * jumpSpeed) + (transform.TransformDirection(ic.moveInput.x, 0, ic.moveInput.y) * jumpLateralForce), ForceMode.Impulse);
            }
        }
        bool CheckGround()
        {
            if (Physics.SphereCast(transform.position + groundCheckOrigin, .3f, Vector3.down, out RaycastHit hit, groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore))
            {
                float angle = Vector3.Angle(transform.up, hit.normal);
                if(angle < 70)
                {
                    groundNormal = hit.normal;
                    return true;
                }
            }
            return false;
        }
        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireSphere(transform.position + groundCheckOrigin, 0.3f);
            Gizmos.DrawWireSphere(transform.position + groundCheckOrigin + (Vector3.down * groundCheckDistance), 0.3f);
        }
    }
}
