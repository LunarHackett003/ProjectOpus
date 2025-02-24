using System.Collections;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

namespace Opus
{
    public class PlayerMotorV2 : ONetBehaviour
    {
        public Rigidbody rb;
        public Transform headTransform;
        public PlayerEntity entity;
        Vector2 oldAimAngle, aimAngle, aimDelta;

        Vector3 lookSwayPos, lookSwayEuler, v_lookswaypos, v_lookswayeuler;
        //Sway and rotation based on movement
        Vector3 moveSwayPos, moveSwayEuler, v_moveswaypos, v_moveswayeuler;
        Quaternion swayInitialRotation;
        float v_verticalvelocityswaypos, v_verticalvelocityswayeuler;
        float verticalVelocitySwayPos, verticalVelocitySwayEuler;

        public SwayParameters swayParams;
        public JumpAnimParameters jumpAnimParams, landAnimParams;
        float jumpAnimTime;
        public bool playingJumpAnim;
        Vector3 jumpAnimPos, jumpAnimRot, targetJumpAnimPos, targetJumpAnimRot;
        public float jumpAnimCameraInfluence, animTimeLerpSpeed;
        float velocityLastAirTick;

        public bool specialMovement;

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
        public int minJumpTicks;
        int ticksSinceGrounded;
        public float groundStickDistance;
        public Vector3 groundStickOffset;
        bool jumped;
        public int jumpsAllowed;
        int jumps;

        RaycastHit groundHit;
        #endregion Ground Checking
        public Transform weaponOffset;
        public MovementState moveState;
        Vector3 moveVec;

        public LedgeClimbParameters vaultParams;
        float vaultTime, vaultDistance, vaultSpeed;
        Vector3 vaultStart, vaultEnd;
        public bool vaulting;
        float currentVaultEnableTime;

        [BurstCompile(CompileSynchronously = true, OptimizeFor = OptimizeFor.Performance)]
        public struct SwayJob : IJob
        {
            public float3 last, next;
            public Vector3 velocity;
            public float delta, time;

            [WriteOnly]
            public float3 output;

            
            public void Execute()
            {
                output = Vector3.SmoothDamp(last, next, ref velocity, time, 5, delta);
            }
        }





        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if(rb == null)
            {
                rb = GetComponent<Rigidbody>();
            }
            swayInitialRotation = Quaternion.identity;

        }

        public override void OUpdate()
        {
            if (MatchManager.Instance && !MatchManager.Instance.GameInProgress.Value)
                return;


            if (entity.Alive)
            {
                if (IsOwner)
                {
                    UpdateAim();
                }
                UpdateSway();
            }

        }
        public override void OFixedUpdate()
        {
            if (MatchManager.Instance && !MatchManager.Instance.GameInProgress.Value)
                return;

            if(IsOwner)
            {
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
                    if (isGrounded)
                    {
                        entity.LastGroundedPosition = transform.position;
                        currentVaultEnableTime = 0;
                    }
                    else 
                    {
                        if (!vaulting)
                        {
                            currentVaultEnableTime += Time.fixedDeltaTime;
                            if (currentVaultEnableTime > vaultParams.vaultEnableTime)
                            {
                                CheckVault();
                            }
                        }
                        velocityLastAirTick = rb.linearVelocity.y;
                    }

                    if (vaulting)
                    {
                        currentVaultEnableTime = 0;
                        if (entity.playerManager.crouchInput || entity.playerManager.moveInput.y < -0.1f)
                        {
                            vaulting = false;
                        }
                    }
                }
                rb.isKinematic = !entity.Alive || vaulting;
            }
        }

        void UpdateAim()
        {
            oldAimAngle = aimAngle;
            if (entity.playerManager.lookInput != Vector2.zero)
            {
                aimAngle += (entity.stunned.Value ? MatchManager.Instance.stunLookSpeedMultiplier : 1) * 
                    Time.deltaTime * entity.playerManager.lookInput * 
                    new Vector2(PlayerSettings.Instance.settingsContainer.mouseLookSpeedX, PlayerSettings.Instance.settingsContainer.mouseLookSpeedY);
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

        }
        void UpdateSway()                                                                                                                                                                                                                                    //Fish was here 8----D
        {
            if (IsOwner)
            {
                //verticalVelocitySwayPos = Mathf.SmoothDamp(verticalVelocitySwayPos, rb.linearVelocity.y * swayParams.verticalVelocitySwayScale,
                //    ref v_verticalvelocityswaypos, swayParams.verticalVelocitySwayPosTime).Clamp(-swayParams.verticalVelocityPosClamp, swayParams.verticalVelocityPosClamp);
                //verticalVelocitySwayEuler = Mathf.SmoothDampAngle(verticalVelocitySwayEuler,
                //    (rb.linearVelocity.y * swayParams.verticalVelocityEulerScale).Clamp(-swayParams.verticalVelocityEulerClamp, swayParams.verticalVelocityEulerClamp),
                //    ref v_verticalvelocityswayeuler, swayParams.verticalVelocitySwayEulerTime);

                verticalVelocitySwayPos = Mathf.Lerp(verticalVelocitySwayPos, rb.linearVelocity.y * swayParams.verticalVelocitySwayScale, swayParams.verticalVelocitySwayPosTime * Time.smoothDeltaTime);
                verticalVelocitySwayEuler = Mathf.LerpAngle(verticalVelocitySwayEuler, rb.linearVelocity.y * swayParams.verticalVelocityEulerScale, swayParams.verticalVelocitySwayEulerTime * Time.smoothDeltaTime);

                if (playingJumpAnim)
                {
                    jumpAnimPos = Vector3.Lerp(jumpAnimPos, targetJumpAnimPos, swayParams.jumpPosLerpSpeed * Time.smoothDeltaTime);
                    jumpAnimRot = Vector3.Lerp(jumpAnimRot, targetJumpAnimRot, swayParams.jumpLerpSpeed * Time.smoothDeltaTime);
                    entity.worldCineCam.transform.SetLocalPositionAndRotation(jumpAnimPos * jumpAnimCameraInfluence, Quaternion.Euler(jumpAnimRot * jumpAnimCameraInfluence));   
                }
                else
                {
                    jumpAnimPos = Vector3.Lerp(jumpAnimPos, Vector3.zero, swayParams.jumpPosLerpSpeed * Time.smoothDeltaTime);
                    jumpAnimRot = Vector3.Lerp(jumpAnimRot, Vector3.zero, swayParams.jumpLerpSpeed * Time.smoothDeltaTime);
                    entity.worldCineCam.transform.SetLocalPositionAndRotation(jumpAnimPos * jumpAnimCameraInfluence, Quaternion.Euler(jumpAnimRot * jumpAnimCameraInfluence));
                }

                lookSwayPos = Vector3.Lerp(lookSwayPos,
                    new Vector3(aimDelta.x * swayParams.lookSwayPosScale.x, aimDelta.y * swayParams.lookSwayPosScale.y).ClampMagnitude(swayParams.maxLookSwayPos), swayParams.lookSwayPosDampTime * Time.smoothDeltaTime);
                lookSwayEuler = Vector3.Lerp(lookSwayEuler,
                    new Vector3(aimDelta.y * swayParams.lookSwayEulerScale.x, aimDelta.x * swayParams.lookSwayEulerScale.y, aimDelta.x * swayParams.lookSwayEulerScale.z).ClampMagnitude(swayParams.maxLookSwayEuler),
                    swayParams.lookSwayEulerDampTime * Time.smoothDeltaTime);

                moveSwayPos = Vector3.Lerp(moveSwayPos,
                    new Vector3(entity.playerManager.moveInput.x * swayParams.moveSwayPosScale.x, 0, entity.playerManager.moveInput.y * swayParams.moveSwayPosScale.y).ClampMagnitude(swayParams.maxMoveSwayPos) 
                    + (entity.wc.Reloading || entity.wc.Grabbing ? Vector3.down * 0.1f : Vector3.zero), swayParams.moveSwayPosDampTime * Time.smoothDeltaTime);
                moveSwayEuler = Vector3.Lerp(moveSwayEuler,
                    new Vector3(0, entity.playerManager.moveInput.x * swayParams.moveSwayEulerScale.y, entity.playerManager.moveInput.x * moveSwayEuler.z).ClampMagnitude(swayParams.maxMoveSwayEuler), 
                    swayParams.moveSwayEulerDampTime * Time.smoothDeltaTime);

            }
            weaponOffset.SetLocalPositionAndRotation(lookSwayPos + moveSwayPos + new Vector3(0, verticalVelocitySwayPos, 0) + jumpAnimPos,
                swayInitialRotation * Quaternion.Euler(lookSwayEuler + moveSwayEuler + new Vector3(verticalVelocitySwayEuler, 0, 0) + jumpAnimRot));
        }
        void CheckGround()
        {
            if (ticksSinceJump == minJumpTicks && Physics.SphereCast(transform.TransformPoint(groundCheckOrigin), groundCheckRadius, -transform.up, out groundHit, groundCheckDistance, groundLayermask, QueryTriggerInteraction.Ignore))
            {
                if (groundHit.normal.y >= walkableGroundThreshold)
                {
                    groundNormal = groundHit.normal;
                    if (!isGrounded && velocityLastAirTick < -1)
                    {
                        PlayJumpOrLandAnim_RPC(true, Mathf.InverseLerp(0, landAnimParams.maxYVelocityOnLand, Mathf.Abs(velocityLastAirTick)));
                    }
                    isGrounded = true && ticksSinceJump >= minJumpTicks;
                    if (groundHit.distance > (groundCheckDistance + groundCheckRadius))
                        SnapToGround();
                    jumps = jumpsAllowed;
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
                    if (!specialMovement)
                    {
                        rb.linearDamping = groundDrag;


                        moveVec = (entity.playerManager.sprintInput && !entity.stunned.Value ? sprintMultiplier : 1)
                            * (entity.stunned.Value ? MatchManager.Instance.stunMoveSpeedMultiplier : 1)
                            * groundMoveForce * ((right * entity.playerManager.moveInput.x)
                            + (forward * entity.playerManager.moveInput.y)).normalized;
                    }
                    rb.AddForce(Vector3.ProjectOnPlane(-Physics.gravity, groundNormal));
                    break;
                case MovementState.sliding:
                    if (!specialMovement)
                        rb.linearDamping = airDrag;
                    break;
                case MovementState.airborne:
                    if (!specialMovement)
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
            if(!entity.stunned.Value)
            {
                jumps--;
                if(vaulting && vaultParams.canEjectFromClimb &&
                vaultTime > vaultParams.minEjectTime && vaultTime < vaultParams.maxEjectTime)
                {
                    rb.AddForce(-transform.forward * vaultParams.ejectRearForce + Vector3.up * vaultParams.ejectUpForce, ForceMode.VelocityChange);
                }
                else
                {
                    rb.AddForce((transform.up * jumpForce) + (Vector3.up * -rb.linearVelocity.y) +
                        (isGrounded ? Vector3.zero : ((entity.playerManager.moveInput.y * jumpForce * 0.5f * transform.forward)
                        + (entity.playerManager.moveInput.x * jumpForce * 0.5f * transform.right))), ForceMode.VelocityChange);
                    ticksSinceJump = 0;
                }
                PlayJumpOrLandAnim_RPC(false);
            }
        }
        Coroutine jumpCoroutine;
        [Rpc(SendTo.ClientsAndHost)]
        void PlayJumpOrLandAnim_RPC(bool landing, float intensity = 1)
        {
            if(jumpCoroutine != null)
            {
                StopCoroutine(jumpCoroutine);
            }
            jumpCoroutine = StartCoroutine(PlayJumpOrLandAnim(landing ? landAnimParams : jumpAnimParams, intensity));
        }

        IEnumerator PlayJumpOrLandAnim(JumpAnimParameters anim, float intensity)
        {
            WaitForFixedUpdate wff = new();
            playingJumpAnim = true;
            jumpAnimTime = 0;
            while (jumpAnimTime < 1)
            {
                jumpAnimTime += Time.fixedDeltaTime * anim.animSpeed;
                targetJumpAnimPos = new Vector3()
                {
                    x = anim.XPositionCurve.Evaluate(jumpAnimTime),
                    y = anim.YPositionCurve.Evaluate(jumpAnimTime),
                    z = anim.ZPositionCurve.Evaluate(jumpAnimTime)
                }.ScaleReturn(anim.MaxPosition) * intensity;
                targetJumpAnimRot = new Vector3()
                {
                    x = anim.XRotationCurve.Evaluate(jumpAnimTime),
                    y = anim.YRotationCurve.Evaluate(jumpAnimTime),
                    z = anim.ZRotationCurve.Evaluate(jumpAnimTime)
                }.ScaleReturn(anim.maxRotation) * intensity;
                yield return wff;
            }
            yield return new WaitForSeconds(anim.endWaitTime);
            playingJumpAnim = false;
        }
        void CheckVault()
        {
            /*
            Checking for vaulting is a complex process. It should only happen in certain circumstances.
            It should not happen on the ground (we cannot enter CheckVault while grounded)
            We should not check for vaulting if we are:
                - already vaulting
                - not moving forwards or pressing the jump button to vault
            We will 
                1. cast a box forwards to see if we hit a surface
                2. cast a ray downwards from the maximum vaultable height to ground level
                3. cast a slightly offset ray back upwards to see if we have enough space to stand up
            If 1 & 2 hit and 3 does not, we can vault into this position.
             */

            if (!(entity.playerManager.moveInput.y > 0 || entity.playerManager.jumpInput))
                return;
            if (Physics.BoxCast(transform.position + vaultParams.climbCheckOffset, vaultParams.climbCheckBounds / 2, transform.forward, 
                out RaycastHit hit, transform.rotation, vaultParams.vaultDistance, groundLayermask, QueryTriggerInteraction.Ignore))
            {
                Vector3 nextRayOrigin = new Vector3(hit.point.x, transform.position.y + vaultParams.maxVaultHeight, hit.point.z) + transform.forward * vaultParams.lateralPositionForwardOffset;
                if (Physics.Raycast(nextRayOrigin, Vector3.down, out hit, vaultParams.maxVaultHeight-0.1f, groundLayermask, QueryTriggerInteraction.Ignore))
                {
                    if(!Physics.Raycast(hit.point, Vector3.up, vaultParams.maxVaultHeight + 0.02f, groundLayermask, QueryTriggerInteraction.Ignore))
                    {
                        vaultStart = transform.position;
                        vaultEnd = hit.point;
                        entity.playerManager.jumpInput = false;
                        StartCoroutine(VaultWall());
                    }
                }
            }
        }
        IEnumerator VaultWall()
        {
            vaulting = true;
            vaultTime = 0;
            vaultDistance = Vector3.Distance(vaultEnd, vaultStart);
            vaultSpeed = vaultParams.climbSpeed / vaultDistance;
            
            print($"Started vault from {vaultStart} to {vaultEnd}");
            WaitForFixedUpdate wff = new();
            Vector2 latPos;
            
            while (vaultTime < 1 && vaulting)
            {
                vaultTime += Time.fixedDeltaTime * vaultSpeed;
                latPos = Vector2.Lerp(new(vaultStart.x, vaultStart.z), new(vaultEnd.x, vaultEnd.z), vaultParams.lateralPath.Evaluate(vaultTime));
                float vertPos = Mathf.Lerp(vaultStart.y, vaultEnd.y, vaultTime);
                transform.position = new(latPos.x, vertPos, latPos.y);
                yield return wff;
            }

            vaulting = false;
        }
        private void OnDrawGizmos()
        {
            if(vaultParams != null && vaultParams.debugMode)
            {
                Gizmos.color = Color.cyan;
                Vector3 vec = (transform.forward * vaultParams.vaultDistance);
                Vector3 vec2 = vec + (transform.forward * vaultParams.lateralPositionForwardOffset);
                Gizmos.DrawWireCube(transform.position + vaultParams.climbCheckOffset, vaultParams.climbCheckBounds);
                Gizmos.DrawWireCube(transform.position + vaultParams.climbCheckOffset + vec, vaultParams.climbCheckBounds);

                Gizmos.color = Color.red;
                Gizmos.DrawRay(transform.position + vec2 + (Vector3.up * vaultParams.maxVaultHeight), Vector3.down * vaultParams.maxVaultHeight);


                Gizmos.DrawRay(transform.position + (vec2 * 1.02f), Vector3.up * vaultParams.maxVaultHeight);
            }
        }

    }
}
