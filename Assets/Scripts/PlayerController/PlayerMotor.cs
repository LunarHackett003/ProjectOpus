
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Opus
{
    public class PlayerMotor : NetworkBehaviour
    {
        public Rigidbody rb;
        public InputCollector ic;
        public PlayerManager pm;
        public Vector3 velocity;
        Vector3 movementVelocity;
        Vector3 movementVelocityDamping;
        public Vector2 groundMoveSpeed, airMoveSpeed;
        [Tooltip("X axis is sprinting sideways and backwards, y is sprinting forwards")]
        public Vector2 sprintMultiplier = Vector2.one;
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

        public Vector2 movementVector;
        public PlayerAnimator animator;
        public enum MovementState
        {
            none = 0,
            zipline = 1,
            ladder = 2
        }
        public MovementState moveState;
        public ZiplineMotor zipMotor;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            pm = PlayerManager.playerManagers.First(m => m.OwnerClientId == OwnerClientId);
            ic = pm.InputCollector;
            pm.playerMotor = this;
            if (!IsOwner)
            {
                rb.isKinematic = true;
                enabled = false;
                return;
            }
            SpawnPlayer();
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
            Vector2 moveMultiplier = ic.sprintInput ? new()
            {
                x = sprintMultiplier.x,
                y = ic.moveInput.y < 0 ? sprintMultiplier.x : sprintMultiplier.y,
            } : Vector2.one;

            movementVector = ic.moveInput * moveMultiplier;
            movementVelocity = transform.TransformDirection(ic.moveInput.x * (grounded ? groundMoveSpeed.x * moveMultiplier.x : airMoveSpeed.x),
                0, ic.moveInput.y * (grounded ? groundMoveSpeed.y * moveMultiplier.y : airMoveSpeed.y));
            //Move using our movement vector
            rb.AddForce(Vector3.ProjectOnPlane(movementVelocity, groundNormal));
        }
        void TryJump()
        {
            if (zipMotor.currentZipline)
            {
                zipMotor.Detach(false);
            }
            else if(grounded)
            {
                rb.AddForce((Vector3.up * jumpSpeed) + (transform.TransformDirection(ic.moveInput.x, 0, ic.moveInput.y) * jumpLateralForce), ForceMode.Impulse);
            }
            SendJump();
        }
        bool CheckGround()
        {
            if (Physics.SphereCast(transform.position + groundCheckOrigin, .3f, Vector3.down, out RaycastHit hit, groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore))
            {
                float angle = Vector3.Angle(transform.up, hit.normal);
                if(angle < 70)
                {
                    if (!grounded)
                    {
                        animator.LandTrigger();
                    }
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

        public void SpawnPlayer()
        {
            SceneData sd = FindAnyObjectByType<SceneData>();
            Transform t = sd.GetSpawnPoint(MatchController.Instance.teamMembers.Value.FindIndex(x => x.playerID == OwnerClientId));
            transform.SetPositionAndRotation(t.position, t.rotation);
        }
        public void SendJump()
        {
            if (grounded)
                animator.JumpTrigger();
        }
    }
}
