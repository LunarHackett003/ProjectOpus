using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

namespace Opus
{
    public class PlayerController : HealthyEntity
    {
        #region Definitions
        public PlayerManager MyPlayerManager;

        public Outline outlineComponent;

        public ControlScheme controls;

        public Rigidbody rb;

        public Vector2 moveInput, lookInput;
        public bool jumpInput;
        public bool crouchInput;
        public Vector2 aimAngle, oldAimAngle;
        public Vector2 aimDelta;

        public Transform headTransform;



        public float groundMoveForce, airMoveForce, jumpForce;
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
        public PlayerHUD hud;
        GUIContent content;
        public CharacterRenderable characterRender;
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
                controls = new();
                controls.Player.Move.performed += Move_performed;
                controls.Player.Move.canceled += Move_performed;

                controls.Player.Look.performed += Look_performed;
                controls.Player.Look.canceled += Look_performed;

                controls.Player.Jump.performed += Jump_performed;
                controls.Player.Jump.canceled += Jump_performed;

                controls.Player.Crouch.performed += Crouch_performed;
                controls.Player.Crouch.canceled += Crouch_performed;
                controls.Enable();

                if(!Camera.main.TryGetComponent(out CinemachineBrain brain))
                {
                    brain = Camera.main.gameObject.AddComponent<CinemachineBrain>();
                    brain.UpdateMethod = CinemachineBrain.UpdateMethods.LateUpdate;
                }
                content = new(new Texture2D(32, 32));

                MyPlayerManager.onSpawnReceived += SpawnReceived;

            }
            else
            {
                worldCineCam.enabled = false;
            }


            if(hud != null)
            {
                if (IsOwner)
                {
                    hud.InitialiseHUD();
                }
                else
                {
                    hud.gameObject.SetActive(false);
                }
            }

            if(TryGetComponent(out characterRender))
            {
                characterRender.InitialiseViewable(this);
            }
        }

        void SpawnReceived()
        {
            aimAngle.x = transform.eulerAngles.y;
        }
        #region Input Callbacks
        private void Crouch_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            crouchInput = obj.ReadValueAsButton();
        }

        private void Jump_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            jumpInput = obj.ReadValueAsButton();
        }

        private void Look_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            lookInput = obj.ReadValue<Vector2>();
        }

        private void Move_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            moveInput = obj.ReadValue<Vector2>();
        }
        #endregion
        /// <summary>
        /// Grabs the player's team colours and updates it based on the teams.
        /// </summary>
        public void UpdatePlayerColours()
        {
            Debug.Log($"Updating client {NetworkManager.LocalClientId}'s perception of this object, on team {MyPlayerManager.teamIndex.Value}", gameObject);
            if (MyPlayerManager)
            {
                
                MyPlayerManager.SetPlayerOnSpawn(this);
            }
        }
        private void FixedUpdate()
        {
            if (IsOwner)
            {
                CheckGround();
                if (ticksSinceJump < minJumpTicks)
                    ticksSinceJump++;
                if(ticksSinceWallride < minWallrideTicks)
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
                if (jumpInput && jumps > 0)
                {
                    Jump();
                }
                MovePlayer();
                rb.useGravity = !wallriding;
            }
        }
        RaycastHit groundHit;
        void CheckGround()
        {
            if (Physics.SphereCast(transform.TransformPoint(groundCheckOrigin), groundCheckRadius, -transform.up, out groundHit, groundCheckDistance, groundLayermask, QueryTriggerInteraction.Ignore))
            {
                if(groundHit.normal.y >= walkableGroundThreshold)
                {
                    groundNormal = groundHit.normal;
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
        float speed;
        private void OnGUI()
        {
            GUI.contentColor = wallriding ? Color.green : Color.red;
            GUI.Box(new Rect(0, 0, 32, 32), content);
            GUI.contentColor = wallClimbing ? Color.green : Color.red;
            GUI.Box(new Rect(32, 0, 32, 32), content);
            GUI.contentColor = wallrideOnRight ? Color.green : Color.red;
            GUI.Box(new Rect(64, 0, 32, 32), content);
            GUI.contentColor = ticksSinceJump >= minJumpTicks ? Color.green : Color.red;
            GUI.Box(new Rect(96, 0, 32, 32), $"{ticksSinceJump}/{minJumpTicks}");
            GUI.contentColor = ticksSinceWallride >= minWallrideTicks ? Color.green : Color.red;
            GUI.Box(new Rect(128, 0, 32, 32), $"{ticksSinceWallride}/{minWallrideTicks}");
            speed = rb.linearVelocity.magnitude;
            GUI.contentColor = Color.Lerp(Color.red, Color.green, speed / 100);
            GUI.Box(new(0, 32, 64, 32), $"speed: {speed:0.0}");
            GUI.Box(new(64, 32, 128, 32), $"{rb.linearVelocity:0.0}");
            GUI.contentColor = Color.Lerp(Color.green, Color.red, Mathf.InverseLerp(0, wallrideMaxDeviation, Mathf.Abs(wallrideCurrentDeviation)));
            GUI.Box(new(0, 64, 32, 32), $"{wallrideCurrentDeviation:0.0}");
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
                Vector3 right = Vector3.Cross(-transform.forward, groundNormal);
                Vector3 forward = Vector3.Cross(right, groundNormal);
                moveVec = groundMoveForce * ((right * moveInput.x) + (forward * moveInput.y));
                rb.AddForce(Vector3.ProjectOnPlane(-Physics.gravity, groundNormal));
            }
            else
            {
                if (WallrideCheck())
                {
                    DoWallride();
                }
                else
                {
                    moveVec = airMoveForce * ((transform.forward * moveInput.y) + (transform.right * moveInput.x));
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
                rb.AddForce((wallrideFallForce * wallrideLerp * -transform.up) + (-wallrideNormal * wallrideStickForce), ForceMode.Acceleration);
                if (wallClimbing)
                {
                    forwardVec = -wallrideNormal;
                    transform.forward = Vector3.Lerp(transform.forward, forwardVec, wallrideTurnSpeed * Time.fixedDeltaTime);
                    if (moveInput.y < -0.02f)
                    {
                        CancelWallride();
                        return;
                    }
                    moveVec = (moveInput.y * wallClimbForce * transform.up) + (moveInput.x * (wallClimbForce * 0.5f) * Vector3.Cross(-wallrideNormal, transform.up));
                }
                else
                {
                    forwardVec = Vector3.Cross(-wallrideNormal, wallrideOnRight ? transform.up : -transform.up);
                    transform.forward = Vector3.Lerp(transform.forward, forwardVec, wallrideTurnSpeed * Time.fixedDeltaTime);
                    if ((wallrideOnRight && moveInput.x < -0.1f) || (moveInput.x > 0.1f))
                    {
                        CancelWallride();
                        return;
                    }
                    moveVec = moveInput.y * wallrideMoveForce * Vector3.Cross(transform.right, transform.up);
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
            Debug.DrawRay(transform.position, leftSide ? - transform.right : transform.right, Color.green, 0.1f);
            return Physics.BoxCast(transform.TransformPoint(wallrideOffset), wallrideBounds / 2, leftSide ? -transform.right : transform.right, 
                out hit, transform.rotation, wallrideCheckDistance, groundLayermask);
        }
        void CancelWallride()
        {
            Debug.Log("Cancelling wallride");
            wallriding = false;
            wallClimbing = false;
            currwallridetime = 0;
            wallrideNormal = Vector3.zero;
            lookInput = new(0.00001f, 0.00001f);
            aimAngle.x = transform.eulerAngles.y + wallrideCurrentDeviation;
            wallrideCurrentDeviation = 0;
            ticksSinceJump = 0;
        }
        void Jump()
        {
            jumpInput = false;
            jumps--;
            if (wallriding)
            {
                wallriding = false;
                rb.AddForce((transform.up + wallrideNormal) * jumpForce, ForceMode.VelocityChange);
                ticksSinceWallride = minWallrideTicks;
                CancelWallride();
            }
            else
            {
                rb.AddForce((transform.up * jumpForce) + (Vector3.up * -rb.linearVelocity.y) +
                    (isGrounded ? Vector3.zero : ((moveInput.y * jumpForce * 0.5f * transform.forward)
                    + (moveInput.x * jumpForce * 0.5f * transform.right))), ForceMode.VelocityChange);
            }
            ticksSinceJump = 0;
        }
        private void Update()
        {
            UpdateLook();
        }
        void UpdateLook()
        {
            oldAimAngle = aimAngle;
            if(lookInput != Vector2.zero)
            {

                if (wallriding && wallrideCurrentDeviation < wallrideMaxDeviation)
                {
                    wallrideCurrentDeviation += lookInput.x * PlayerSettings.Instance.settingsContainer.mouseLookSpeedX * Time.deltaTime;
                    aimAngle.y += lookInput.y * PlayerSettings.Instance.settingsContainer.mouseLookSpeedY * Time.deltaTime;
                }
                else
                {
                    //Consume the current deviation and add it to the local rotation
                    transform.localRotation = Quaternion.Euler(0, aimAngle.x + wallrideCurrentDeviation, 0);
                    aimAngle += lookInput * new Vector2(PlayerSettings.Instance.settingsContainer.mouseLookSpeedX, PlayerSettings.Instance.settingsContainer.mouseLookSpeedY) * Time.deltaTime;
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
