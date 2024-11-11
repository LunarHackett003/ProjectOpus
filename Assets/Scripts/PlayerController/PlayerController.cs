using Unity.Cinemachine;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

namespace Opus
{
    public class PlayerController : NetworkBehaviour
    {
        #region Definitions
        public PlayerManager MyPlayerManager;
        public Renderer[] renderers;
        public Renderer[] hideOnHostRenderers;
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
        public int minJumpTicks;
        int ticksSinceGrounded;
        public float groundStickDistance;
        public Vector3 groundStickOffset;
        bool jumped;
        #endregion

        public CinemachineCamera worldCineCam;
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
                    brain = Camera.main.AddComponent<CinemachineBrain>();
                    brain.UpdateMethod = CinemachineBrain.UpdateMethods.LateUpdate;
                }

            }
            else
            {
                worldCineCam.enabled = false;
            }
            foreach (var item in hideOnHostRenderers)
            {
                if (item != null)
                {
                    item.enabled = false;
                }
            }
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
                if (outlineComponent)
                {
                    if(!IsOwner)
                    {
                        if(MyPlayerManager.teamIndex.Value != PlayerManager.MyTeam)
                        {
                            outlineComponent.enabled = true;
                            outlineComponent.OutlineMode = Outline.Mode.OutlineVisible;
                        }
                        else
                        {
                            outlineComponent.enabled = true;
                            outlineComponent.OutlineMode = Outline.Mode.OutlineAll;
                            outlineComponent.OutlineColor = MyPlayerManager.myTeamColour;
                        }
                    }
                }
                foreach (Renderer renderer in renderers)
                {
                    if(renderer != null && renderer.enabled)
                        renderer.material.color = MyPlayerManager.myTeamColour;
                }
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
                if (isGrounded || SnapToGround())
                {

                    rb.linearDamping = groundDrag;
                    if (jumpInput)
                    {
                        Jump();
                    }
                }
                else
                {
                    rb.linearDamping = airDrag;
                }
                MovePlayer();
            }
        }
        void CheckGround()
        {
            if (Physics.SphereCast(transform.TransformPoint(groundCheckOrigin), groundCheckRadius, -transform.up, out RaycastHit hit, groundCheckDistance, groundLayermask, QueryTriggerInteraction.Ignore))
            {
                if(hit.normal.y >= walkableGroundThreshold)
                {
                    groundNormal = hit.normal;
                    isGrounded = true && ticksSinceJump >= minJumpTicks;
                    if (hit.distance > (groundCheckDistance + groundCheckRadius))
                        SnapToGround();
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
            Debug.DrawRay(transform.position, Vector3.down * groundStickDistance, Color.red, .25f);
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
                    return true;
                }
            }
            return false;
        }
        void MovePlayer()
        {
            Vector3 moveVec;
            if (isGrounded)
            {
                Vector3 right = Vector3.Cross(-transform.forward, groundNormal);
                Vector3 forward = Vector3.Cross(right, groundNormal);
                Debug.DrawRay(transform.position, right, Color.red);
                Debug.DrawRay(transform.position, forward, Color.blue);
                Debug.DrawRay(transform.position, groundNormal, Color.green);
                moveVec = groundMoveForce * ((right * moveInput.x) + (forward * moveInput.y));
                rb.AddForce(Vector3.ProjectOnPlane(-Physics.gravity, groundNormal));
            }
            else
            {
                moveVec = airMoveForce * ((transform.forward * moveInput.y) + (transform.right * moveInput.x));
            }
            rb.AddForce(moveVec, ForceMode.Acceleration);
        }
        void Jump()
        {
            jumpInput = false;
            rb.AddForce(transform.up * jumpForce, ForceMode.VelocityChange);
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
                aimAngle += lookInput * new Vector2(PlayerSettings.Instance.settingsContainer.mouseLookSpeedX, PlayerSettings.Instance.settingsContainer.mouseLookSpeedY) * Time.deltaTime;
                aimAngle.x %= 360;
                aimAngle.y = Mathf.Clamp(aimAngle.y, -85f, 85f);
                if (headTransform)
                {
                    headTransform.localRotation = Quaternion.Euler(-aimAngle.y, 0, 0);
                }
                transform.localRotation = Quaternion.Euler(0, aimAngle.x, 0);
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
        }
        private void OnCollisionEnter(Collision collision)
        {
            Debug.Log(collision.impulse);
        }
    }
}
