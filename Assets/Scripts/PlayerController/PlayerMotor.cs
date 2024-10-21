
using System.Linq;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.Universal;

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

        public CinemachineCamera worldCineCam, viewCineCam;
        public Camera viewmodelCamera;
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
                worldCineCam.enabled = false;
                viewCineCam.enabled = false;
                return;
            }
            else
            {
                Camera.main.GetUniversalAdditionalCameraData().cameraStack.Add(viewmodelCamera);
                worldCineCam.Prioritize();
                viewCineCam.Prioritize();
            }
            SpawnPlayer();
        }

        private void Update()
        {
            if (ic != null)
            {
                head.localRotation = Quaternion.Euler(-ic.lookInput.y, 0, 0);
                transform.rotation = Quaternion.Euler(0, ic.lookInput.x, 0);

                if (ic.TryConsumeJump())
                {
                    TryJump();
                }
            }
        }
        private void FixedUpdate()
        {
            grounded = CheckGround();
            rb.linearDamping = grounded ? groundDrag : airDrag;
            if (zipMotor.currentZipline != null)
                moveState = MovementState.zipline;
            if (ic != null && moveState == MovementState.none)
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

            if (IsServer && transform.position.y < -50 && !outofboundsChecked)
            {
                OutOfBounds();
            }
            else
            {
                outofboundsChecked = false;
            }

            if (pm.weaponManager != null && pm.weaponManager.equipmentDict.Count > 0 && pm.weaponManager.equipmentDict[pm.weaponManager.currentSlot.Value] is RangedWeapon r && r.useAim)
            {
                if (IsOwner)
                {
                    viewmodelCamera.transform.localPosition = Vector3.Lerp(Vector3.zero, r.aimViewPosition, r.aimAmount);
                    pm.weaponManager.weaponPoint.transform.SetLocalPositionAndRotation(Vector3.Lerp(Vector3.zero, r.aimLocalWeaponPos, r.aimAmount),
                        Quaternion.Lerp(pm.weaponManager.weaponPoint.transform.localRotation, Quaternion.identity, r.aimAmount * (1 - r.aimedRotationInfluence)) 
                        * Quaternion.Lerp(Quaternion.identity, r.localAimOffsetRotation, r.aimAmount));
                }
                else
                {
                    pm.weaponManager.weaponPoint.transform.SetLocalPositionAndRotation(Vector3.Lerp(Vector3.zero, r.aimRemoteWeaponPos, r.aimAmount),
                        Quaternion.Lerp(pm.weaponManager.weaponPoint.transform.localRotation, Quaternion.identity, r.aimAmount * (1 - r.aimedRotationInfluence))
                        * Quaternion.Lerp(Quaternion.identity, r.remoteAimOffsetRotation, r.aimAmount));
                }
            }
            else
            {
                if(IsOwner)
                    viewmodelCamera.transform.localPosition = Vector3.zero;
                pm.weaponManager.weaponPoint.transform.localPosition = Vector3.zero;
            }
        }
        void RegularMovement()
        {
            if (PauseMenu.Instance.GamePaused)
                return;
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
            else if (grounded)
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
                if (angle < 70)
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
        bool outofboundsChecked;
        public void OutOfBounds()
        {
            outofboundsChecked = true;
            OutOfBoundsResetPlayer_RPC();
            
        }
        [Rpc(SendTo.Owner)]
        void OutOfBoundsResetPlayer_RPC()
        {
            SpawnPlayer(true);
        }
        [Rpc(SendTo.Server)]
        void OutOfBoundsReset_RPC()
        {
            outofboundsChecked = false;
        }
        public void SpawnPlayer(bool outOfBounds = false)
        {
            SceneData sd = FindAnyObjectByType<SceneData>();
            Transform t = sd.GetSpawnPoint(MatchController.Instance.teamMembers.Value.FindIndex(x => x.playerID == OwnerClientId));
            transform.SetPositionAndRotation(t.position, t.rotation);

            if (outOfBounds)
            {
                OutOfBoundsReset_RPC();
            }
        }
        public void SendJump()
        {
            if (grounded)
                animator.JumpTrigger();
        }
    }
}
