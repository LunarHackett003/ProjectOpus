using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace Opus
{
    public class PlayerController : HealthyEntity
    {
        #region Definitions

        public bool Alive;


        public AnimatorCustomParamProxy acpp;

        public PlayerManager MyPlayerManager;

        public Outline outlineComponent;

        public Rigidbody rb;

        public Vector2 aimAngle, oldAimAngle;
        public Vector2 aimDelta;

        public Transform headTransform;
        public Transform weaponOffset;

        public Transform clientRotationRoot;

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
        #endregion


        Vector3 wallrideNormal;
        public Vector3 wallrideBounds;
        public Vector3 wallrideOffset;
        public float wallrideCheckDistance;
        public float wallrideFallForce;
        public float wallrideMoveForce;
        public float wallrideStickForce;
        public float wallrideMaxTime;
        public float wallrideTurnSpeed;
        public float wallrideMaxDeviation;
        float wallrideCurrentDeviation;
        bool wallrideOnRight;
        bool wallriding;

        public Vector3 wallClimbBounds;
        public float wallClimbDistance;
        public float wallClimbForce;
        public float wallClimbMaxTime;
        bool wallClimbing;

        public CinemachineCamera worldCineCam;
        public CinemachineCamera viewCineCam;
        public Camera viewmodelCamera;
        GUIContent content;
        public CharacterRenderable characterRender;
        public WeaponController wc;

        public bool canWallrun;

        public NetworkVariable<bool> SpawnedFromRevive = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        Vector3 lastGroundedPosition;
        #endregion
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            PlayerManager.playersByID.TryGetValue(OwnerClientId, out MyPlayerManager);
            UpdatePlayerColours();

            if (rb == null)
                rb = GetComponent<Rigidbody>();
            //Subscribe the owner to input callbacks
            if (IsOwner)
            {

                if(!Camera.main.TryGetComponent(out CinemachineBrain brain))
                {
                    brain = Camera.main.gameObject.AddComponent<CinemachineBrain>();
                    brain.UpdateMethod = CinemachineBrain.UpdateMethods.LateUpdate;
                }
                content = new(new Texture2D(32, 32));

                MyPlayerManager.onSpawnReceived += SpawnReceived;
                Camera.main.GetUniversalAdditionalCameraData().cameraStack.Add(viewmodelCamera);
                Camera.main.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            }
            else
            {
                worldCineCam.enabled = false;
                viewCineCam.enabled = false;
                viewmodelCamera.enabled = false;

                ticksSinceJump = minJumpTicks;

                rb.isKinematic = true;

            }
            

            if(TryGetComponent(out characterRender))
            {
                //characterRender.InitialiseViewable(this);
            }
            swayInitialRotation = weaponOffset.localRotation;

            wc = GetComponent<WeaponController>();

            currentHealth.OnValueChanged += HealthUpdated;

        }

        void HealthUpdated(float previous, float current)
        {
            if(previous > 0 && current <= 0)
            {
                //the player just died, we need to make sure they're dead.
                if (IsServer)
                {
                    MyPlayerManager.SpawnReviveItem(lastGroundedPosition);
                }
            }
            else
            {
                if(previous <= 0)
                {
                    //the player has just come back to life or has been revived.
                    if (IsServer)
                    {
                        MyPlayerManager.RespawnPlayer(SpawnedFromRevive.Value, lastGroundedPosition);
                    }
                }
            }
        }

        void SpawnReceived()
        {
            aimAngle.x = clientRotationRoot.eulerAngles.y;
        }


        #region Input Callbacks

        #endregion
        /// <summary>
        /// Grabs the player's team colours and updates it based on the teams.
        /// </summary>
        public void UpdatePlayerColours()
        {
            Debug.Log($"Updating client {NetworkManager.LocalClientId}'s perception of this object, on team {MyPlayerManager.teamIndex.Value}", gameObject);
            if (MyPlayerManager)
            {
                
            }
        }
        private void FixedUpdate()
        {

            Alive = CurrentHealth > 0;

            if (IsServer)
            {
                CheckGround();
                rb.isKinematic = !Alive;
                if (Alive)
                {
                    if (ticksSinceJump < minJumpTicks)
                        ticksSinceJump++;
                    if (ticksSinceWallride < minWallrideTicks)
                        ticksSinceWallride++;
                    if (isGrounded || SnapToGround())
                    {
                        rb.linearDamping = groundDrag;
                        if (wallriding)
                        {
                            CancelWallride();
                        }
                    }
                    else
                    {
                        rb.linearDamping = airDrag;
                    }
                    if (MyPlayerManager.jumpInput && jumps > 0)
                    {
                        Jump();
                    }
                    MovePlayer();
                    rb.useGravity = !wallriding;
                }
            }

            headTransform.position = worldCineCam.transform.position;
        }
        RaycastHit groundHit;
        void CheckGround()
        {
            if (ticksSinceJump == minJumpTicks && Physics.SphereCast(clientRotationRoot.TransformPoint(groundCheckOrigin), groundCheckRadius, -clientRotationRoot.up, out groundHit, groundCheckDistance, groundLayermask, QueryTriggerInteraction.Ignore))
            {
                if(groundHit.normal.y >= walkableGroundThreshold)
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
        float speed;
        private void OnGUI()
        {
            if (IsOwner)
            {
                GUI.contentColor = Alive ? Color.green : Color.red;
                GUI.Box(new Rect(0, 0, 32, 32), content);
            }
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
                    if(dot > 0f)
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
        Vector3 moveVec;
        void MovePlayer()
        {
            if (isGrounded)
            {
                Vector3 right = Vector3.Cross(-clientRotationRoot.forward, groundNormal);
                Vector3 forward = Vector3.Cross(right, groundNormal);
                moveVec = groundMoveForce * (MyPlayerManager.sprintInput ? sprintMultiplier : 1) * ((right * MyPlayerManager.moveInput.x) + (forward * MyPlayerManager.moveInput.y));
                rb.AddForce(Vector3.ProjectOnPlane(-Physics.gravity, groundNormal));
            }
            else
            {
                if (canWallrun && WallrideCheck())
                {
                    DoWallride();
                }
                else
                {
                    moveVec = airMoveForce * ((clientRotationRoot.forward * MyPlayerManager.moveInput.y) + (clientRotationRoot.right * MyPlayerManager.moveInput.x));
                }
            }
            rb.AddForce(moveVec, ForceMode.Acceleration);
        }
        RaycastHit wallHit;
        bool WallrideCheck()
        {
            if (ticksSinceJump < minJumpTicks || ticksSinceWallride < minWallrideTicks)
                return false;
            if (!wallClimbing)
            {
                if (WallrideBoxCast(out wallHit, false))
                {
                    wallrideOnRight = true;
                    if (Vector3.Dot(wallHit.normal, -transform.right) > 0)
                    {
                        wallriding = true;
                        wallrideNormal = wallHit.normal;
                        return true;
                    }
                }
                if (WallrideBoxCast(out wallHit, true))
                {
                    wallrideOnRight = false;
                    if (Vector3.Dot(wallHit.normal, transform.right) > 0)
                    {
                        wallriding = true;
                        wallrideNormal = wallHit.normal;
                        Debug.DrawRay(wallHit.point, wallHit.normal, Color.magenta);
                        return true;
                    }
                }
            }
            if (Physics.BoxCast(transform.TransformPoint(wallrideOffset), wallClimbBounds / 2, transform.forward, out wallHit, transform.rotation, wallClimbDistance, groundLayermask))
            {
                if (Vector3.Dot(wallHit.normal, -transform.forward) > 0.5f)
                {
                    wallClimbing = true;
                    wallriding = true;
                    wallrideNormal = wallHit.normal;
                    return true;
                }
            }
            if (wallriding)
            {
                CancelWallride();
            }
            return false;
        }
        float currwallridetime;
        float wallrideLerp;
        Vector3 forwardVec;
        void DoWallride()
        {
            if (currwallridetime < (wallClimbing ? wallClimbMaxTime : wallrideMaxTime))
            {
                jumps = jumpsAllowed;
                wallrideLerp = Mathf.Clamp01(Mathf.InverseLerp(0, wallrideMaxTime, currwallridetime));
                currwallridetime += Time.fixedDeltaTime;
                rb.AddForce((wallrideFallForce * wallrideLerp * -clientRotationRoot.up) + (-wallrideNormal * wallrideStickForce), ForceMode.Acceleration);
                if (wallClimbing)
                {
                    forwardVec = -wallrideNormal;
                    clientRotationRoot.forward = Vector3.Lerp(clientRotationRoot.forward, forwardVec, wallrideTurnSpeed * Time.fixedDeltaTime);
                    if (MyPlayerManager.moveInput.y < -0.02f)
                    {
                        CancelWallride();
                        
                        return;
                    }
                    moveVec = (MyPlayerManager.moveInput.y * wallClimbForce * clientRotationRoot.up) + (MyPlayerManager.moveInput.x * (wallClimbForce * 0.5f) * Vector3.Cross(-wallrideNormal, clientRotationRoot.up));
                }
                else
                {
                    forwardVec = Vector3.Cross(-wallrideNormal, wallrideOnRight ? clientRotationRoot.up : -clientRotationRoot.up);
                    clientRotationRoot.forward = Vector3.Lerp(clientRotationRoot.forward, forwardVec, wallrideTurnSpeed * Time.fixedDeltaTime);
                    if ((wallrideOnRight && MyPlayerManager.moveInput.x < -0.1f) || (MyPlayerManager.moveInput.x > 0.1f))
                    {
                        CancelWallride();
                        return;
                    }
                    moveVec = MyPlayerManager.moveInput.y * wallrideMoveForce * Vector3.Cross(clientRotationRoot.right, clientRotationRoot.up);
                }

            }
            else
            {
                CancelWallride();
                ticksSinceWallride = 0;
            }
        }
        bool WallrideBoxCast(out RaycastHit hit, bool leftSide = false)
        {
            Debug.DrawRay(clientRotationRoot.position, leftSide ? - clientRotationRoot.right : clientRotationRoot.right, Color.green, 0.1f);
            return Physics.BoxCast(clientRotationRoot.TransformPoint(wallrideOffset), wallrideBounds / 2, leftSide ? -clientRotationRoot.right : clientRotationRoot.right, 
                out hit, clientRotationRoot.rotation, wallrideCheckDistance, groundLayermask);
        }
        void CancelWallride()
        {
            Debug.Log("Cancelling wallride");
            wallriding = false;
            wallClimbing = false;
            currwallridetime = 0;
            wallrideNormal = Vector3.zero;
            MyPlayerManager.lookInput = new(0.00001f, 0.00001f);
            aimAngle.x = clientRotationRoot.eulerAngles.y + wallrideCurrentDeviation;
            wallrideCurrentDeviation = 0;
            ticksSinceJump = 0;
        }
        void Jump()
        {
            MyPlayerManager.jumpInput = false;
            jumps--;
            if (wallriding)
            {
                wallriding = false;
                rb.AddForce((clientRotationRoot.up + wallrideNormal) * jumpForce, ForceMode.VelocityChange);
                ticksSinceWallride = minWallrideTicks;
                CancelWallride();
            }
            else
            {
                rb.AddForce((clientRotationRoot.up * jumpForce) + (Vector3.up * -rb.linearVelocity.y) +
                    (isGrounded ? Vector3.zero : ((MyPlayerManager.moveInput.y * jumpForce * 0.5f * clientRotationRoot.forward)
                    + (MyPlayerManager.moveInput.x * jumpForce * 0.5f * clientRotationRoot.right))), ForceMode.VelocityChange);
            }
            ticksSinceJump = 0;
        }
        private void Update()
        {
            if(Alive)
                UpdateLook();
        }
        void UpdateLook()
        {
            oldAimAngle = aimAngle;
            if(MyPlayerManager.lookInput != Vector2.zero)
            {

                if (wallriding && wallrideCurrentDeviation < wallrideMaxDeviation)
                {
                    wallrideCurrentDeviation += MyPlayerManager.lookInput.x * PlayerSettings.Instance.settingsContainer.mouseLookSpeedX * Time.deltaTime;
                    aimAngle.y += MyPlayerManager.lookInput.y * PlayerSettings.Instance.settingsContainer.mouseLookSpeedY * Time.deltaTime;
                }
                else
                {
                    //Consume the current deviation and add it to the local rotation
                    clientRotationRoot.localRotation = Quaternion.Euler(0, aimAngle.x + wallrideCurrentDeviation, 0);
                    aimAngle += MyPlayerManager.lookInput * new Vector2(PlayerSettings.Instance.settingsContainer.mouseLookSpeedX, PlayerSettings.Instance.settingsContainer.mouseLookSpeedY) * Time.deltaTime;
                    aimAngle.y = Mathf.Clamp(aimAngle.y, -85f, 85f);
                    wallrideCurrentDeviation = 0;
                }
                if (headTransform)
                {
                    headTransform.localRotation = Quaternion.Euler(-aimAngle.y, wallriding ? wallrideCurrentDeviation : 0, 0);
                }
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
                new Vector3(MyPlayerManager.moveInput.x * moveSwayPosScale.x, 0, MyPlayerManager.moveInput.y * moveSwayPosScale.y).ClampMagnitude(maxMoveSwayPos), 
                ref v_moveswaypos, moveSwayPosDampTime);
            moveSwayEuler = Vector3.SmoothDamp(moveSwayEuler, 
                new Vector3(0, MyPlayerManager.moveInput.x * moveSwayEulerScale.y, MyPlayerManager.moveInput.x * moveSwayEuler.z).ClampMagnitude(maxMoveSwayEuler), 
                ref v_moveswayeuler, moveSwayEulerDampTime);


            weaponOffset.SetLocalPositionAndRotation(lookSwayPos + moveSwayPos + new Vector3(0, verticalVelocitySwayPos, 0) + (wc != null ? wc.linearMoveBob : Vector3.zero),
                swayInitialRotation * Quaternion.Euler(lookSwayEuler + moveSwayEuler + new Vector3(verticalVelocitySwayEuler, 0, 0) + (wc != null ? wc.angularMoveBob : Vector3.zero)));
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = clientRotationRoot.localToWorldMatrix;
            Gizmos.color = Color.green;
            Gizmos.DrawRay(groundCheckOrigin, Vector3.down * groundCheckDistance);
            Gizmos.DrawWireSphere(groundCheckOrigin, groundCheckRadius);
            Gizmos.DrawWireSphere(groundCheckOrigin + Vector3.down * groundCheckDistance, groundCheckRadius);

            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(Vector3.zero, Vector3.down * groundStickDistance);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(wallrideOffset, wallrideBounds);
            Gizmos.DrawWireCube(wallrideOffset + (Vector3.right * wallrideCheckDistance), wallrideBounds);
            Gizmos.DrawWireCube(wallrideOffset - (Vector3.right * wallrideCheckDistance), wallrideBounds);

            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(wallrideOffset, wallClimbBounds);
            Gizmos.DrawWireCube(wallrideOffset + Vector3.forward * wallClimbDistance, wallClimbBounds);
        }
        private void OnCollisionEnter(Collision collision)
        {

        }
    }
}
