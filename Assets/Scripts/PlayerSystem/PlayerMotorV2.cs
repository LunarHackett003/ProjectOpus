using Unity.Netcode;
using UnityEngine;

namespace Opus
{
    public class PlayerMotorV2 : NetworkBehaviour
    {
        public Rigidbody rb;
        public Transform headTransform;
        public PlayerEntity entity;
        Vector2 oldAimAngle, aimAngle, aimDelta;

        Vector3 lookSwayPos, lookSwayEuler, v_lookswaypos, v_lookswayeuler;
        //Sway and rotation based on movement
        Vector3 moveSwayPos, moveSwayEuler, v_moveswaypos, v_moveswayeuler;
        Quaternion swayInitialRotation;
        public Vector3 lookSwayPosScale, lookSwayEulerScale;
        public float lookSwayPosDampTime, lookSwayEulerDampTime, maxLookSwayPos, maxLookSwayEuler;

        public Vector3 moveSwayPosScale, moveSwayEulerScale;
        public float moveSwayPosDampTime, moveSwayEulerDampTime, maxMoveSwayPos, maxMoveSwayEuler;

        //Sway and rotation based on vertical velocity
        public float verticalVelocitySwayScale, verticalVelocityEulerScale, verticalVelocitySwayPosTime, verticalVelocitySwayEulerTime, verticalVelocityPosClamp, verticalVelocityEulerClamp;
        float v_verticalvelocityswaypos, v_verticalvelocityswayeuler;
        float verticalVelocitySwayPos, verticalVelocitySwayEuler;

        public float groundMoveForce, airMoveForce, jumpForce, sprintMultiplier;
        public float groundDrag, airDrag;
        #region Ground Checking
        public bool isGrounded;
        public float groundCheckDistance;
        public float groundCheckRadius;
        [Tooltip("The current normal of the ground we're walking on")]
        public Vector3 groundNormal;
        [Tooltip("The layermask to spherecast against when checking the ground")]
        public LayerMask groundLayermask;
        [Tooltip("where the ground check starts, relative to the player")]
        public Vector3 groundCheckOrigin;
        [Tooltip("ground normal y values less than this value will be unwalkable.")]
        public float walkableGroundThreshold;
        int ticksSinceJump;
        int ticksSinceWallride;
        public int minJumpTicks;
        public int minWallrideTicks;
        int ticksSinceGrounded;
        public float groundStickDistance;
        public Vector3 groundStickOffset;
        bool jumped;
        public int jumpsAllowed;
        int jumps;

        RaycastHit groundHit;
        Vector3 lastGroundedPosition;
        #endregion Ground Checking
        public Transform weaponOffset;
        public MovementState moveState;
        Vector3 moveVec;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if(rb == null)
            {
                rb = GetComponent<Rigidbody>();
            }
            

        }

        private void Update()
        {
            if (IsOwner && entity.Alive)
            {
                UpdateAim();
            }   
        }
        private void FixedUpdate()
        {
            if(IsOwner)
            {
                rb.isKinematic = !entity.Alive;
                if (entity.Alive)
                {
                    CheckGround();
                    MovePlayer();
                    if (ticksSinceJump < minJumpTicks)
                        ticksSinceJump++;
                    if (entity.playerManager.jumpInput && jumps > 0)
                    {
                        Jump();
                    }
                }
            }
        }

        void UpdateAim()
        {
            oldAimAngle = aimAngle;
            if (entity.playerManager.lookInput != Vector2.zero)
            {
                aimAngle += entity.playerManager.lookInput * new Vector2(PlayerSettings.Instance.settingsContainer.mouseLookSpeedX, PlayerSettings.Instance.settingsContainer.mouseLookSpeedY) * Time.deltaTime;
                aimAngle.y = Mathf.Clamp(aimAngle.y, -85f, 85f);
                
                if (headTransform)
                {
                    headTransform.localRotation = Quaternion.Euler(-aimAngle.y, 0, 0);
                }
                transform.localRotation = Quaternion.Euler(0, aimAngle.x, 0);
            }
            aimDelta = oldAimAngle - aimAngle;
            aimDelta.x %= 360;
            aimAngle.x %= 360;

            UpdateSway();
        }
        void UpdateSway()                                                                                                                                                                                                                                    //Fish was here 8----D
        {
            verticalVelocitySwayPos = Mathf.SmoothDamp(verticalVelocitySwayPos, rb.linearVelocity.y * verticalVelocitySwayScale,
                ref v_verticalvelocityswaypos, verticalVelocitySwayPosTime).Clamp(-verticalVelocityPosClamp, verticalVelocityPosClamp);
            verticalVelocitySwayEuler = Mathf.SmoothDampAngle(verticalVelocitySwayEuler, (rb.linearVelocity.y * verticalVelocityEulerScale).Clamp(-verticalVelocityEulerClamp, verticalVelocityEulerClamp),
                ref v_verticalvelocityswayeuler, verticalVelocitySwayEulerTime);



            lookSwayPos = Vector3.SmoothDamp(lookSwayPos,
                new Vector3(aimDelta.x * lookSwayPosScale.x, aimDelta.y * lookSwayPosScale.y).ClampMagnitude(maxLookSwayPos),
                ref v_lookswaypos, lookSwayPosDampTime);
            lookSwayEuler = Vector3.SmoothDamp(lookSwayEuler,
                new Vector3(aimDelta.y * lookSwayEulerScale.x, aimDelta.x * lookSwayEulerScale.y, aimDelta.x * lookSwayEulerScale.z).ClampMagnitude(maxLookSwayEuler),
                ref v_lookswayeuler, lookSwayEulerDampTime);

            moveSwayPos = Vector3.SmoothDamp(moveSwayPos,
                new Vector3(entity.playerManager.moveInput.x * moveSwayPosScale.x, 0, entity.playerManager.moveInput.y * moveSwayPosScale.y).ClampMagnitude(maxMoveSwayPos),
                ref v_moveswaypos, moveSwayPosDampTime);
            moveSwayEuler = Vector3.SmoothDamp(moveSwayEuler,
                new Vector3(0, entity.playerManager.moveInput.x * moveSwayEulerScale.y, entity.playerManager.moveInput.x * moveSwayEuler.z).ClampMagnitude(maxMoveSwayEuler),
                ref v_moveswayeuler, moveSwayEulerDampTime);


            weaponOffset.SetLocalPositionAndRotation(lookSwayPos + moveSwayPos + new Vector3(0, verticalVelocitySwayPos, 0),
                swayInitialRotation * Quaternion.Euler(lookSwayEuler + moveSwayEuler + new Vector3(verticalVelocitySwayEuler, 0, 0)));
        }
        void CheckGround()
        {
            if (ticksSinceJump == minJumpTicks && Physics.SphereCast(transform.TransformPoint(groundCheckOrigin), groundCheckRadius, -transform.up, out groundHit, groundCheckDistance, groundLayermask, QueryTriggerInteraction.Ignore))
            {
                if (groundHit.normal.y >= walkableGroundThreshold)
                {
                    groundNormal = groundHit.normal;
                    isGrounded = true && ticksSinceJump >= minJumpTicks;
                    if (groundHit.distance > (groundCheckDistance + groundCheckRadius))
                        SnapToGround();
                    jumps = jumpsAllowed;
                    lastGroundedPosition = transform.position;
                    return;
                }
            }
            groundNormal = Vector3.zero;
            isGrounded = false;
        }
        bool SnapToGround()
        {
            if (ticksSinceGrounded > 1 || ticksSinceJump < minJumpTicks)
                return false;
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, groundStickDistance, groundLayermask, QueryTriggerInteraction.Ignore))
            {
                if (hit.normal.y > walkableGroundThreshold)
                {
                    Vector3 velocity = rb.linearVelocity;
                    float speed = velocity.magnitude;
                    float dot = Vector3.Dot(velocity, hit.normal);
                    if (dot > 0f)
                    {
                        velocity = (velocity - hit.normal * dot).normalized * speed;
                    }
                    rb.linearVelocity = velocity;
                    rb.MovePosition(hit.point + groundStickOffset);
                    isGrounded = true;
                    return true;
                }
            }
            return false;
        }
        void MovePlayer()
        {
            moveState = isGrounded ? MovementState.walking : MovementState.airborne;
            Vector3 right = Vector3.Cross(-transform.forward, groundNormal);
            Vector3 forward = Vector3.Cross(right, groundNormal);
            switch (moveState)
            {
                case MovementState.none:
                    break;
                case MovementState.walking:
                    rb.linearDamping = groundDrag;


                    moveVec = groundMoveForce * (entity.playerManager.sprintInput ? sprintMultiplier : 1) * ((right * entity.playerManager.moveInput.x) + (forward * entity.playerManager.moveInput.y)).normalized;

                    rb.AddForce(Vector3.ProjectOnPlane(-Physics.gravity, groundNormal));
                    break;
                case MovementState.sliding:
                    rb.linearDamping = airDrag;
                    break;
                case MovementState.airborne:
                    rb.linearDamping = airDrag;
                    moveVec = airMoveForce * ((right * entity.playerManager.moveInput.x) + (forward * entity.playerManager.moveInput.y)).normalized;

                    break;
                default:
                    break;
            }
            rb.AddForce(moveVec, ForceMode.Acceleration);
        }


        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.green;
            Gizmos.DrawRay(groundCheckOrigin, Vector3.down * groundCheckDistance);
            Gizmos.DrawWireSphere(groundCheckOrigin, groundCheckRadius);
            Gizmos.DrawWireSphere(groundCheckOrigin + Vector3.down * groundCheckDistance, groundCheckRadius);

            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(Vector3.zero, Vector3.down * groundStickDistance);

        }

        void Jump()
        {
            entity.playerManager.jumpInput = false;
            jumps--;
            {
                rb.AddForce((transform.up * jumpForce) + (Vector3.up * -rb.linearVelocity.y) +
                    (isGrounded ? Vector3.zero : ((entity.playerManager.moveInput.y * jumpForce * 0.5f * transform.forward)
                    + (entity.playerManager.moveInput.x * jumpForce * 0.5f * transform.right))), ForceMode.VelocityChange);
            }
            ticksSinceJump = 0;
        }
    }
}
