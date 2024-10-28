using Unity.Netcode;
using UnityEngine;

namespace Opus
{
    public class PlayerController : NetworkBehaviour
    {
        public PlayerManager MyPlayerManager;
        public Renderer[] renderers;
        public Outline outlineComponent;

        public ControlScheme controls;

        public Vector2 moveInput, lookInput;
        public bool jumpInput;
        public bool crouchInput;
        public Vector2 aimAngle, oldAimAngle;
        public Vector2 aimDelta;

        public Transform headTransform;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            PlayerManager.playersByID.TryGetValue(OwnerClientId, out MyPlayerManager);
            UpdatePlayerColours();

            if (IsOwner)
            {
                controls = new();
                controls.Player.Move.performed += Move_performed;
                controls.Player.Move.canceled += Move_performed;

                controls.Player.Look.performed += Look_performed;
                controls.Player.Look.canceled += Look_performed;

                controls.Enable();
            }
        }

        private void Look_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            lookInput = obj.ReadValue<Vector2>();
        }

        private void Move_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            moveInput = obj.ReadValue<Vector2>();
        }

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
                    if(MyPlayerManager.teamIndex.Value != PlayerManager.MyTeam || IsOwner)
                    {
                        outlineComponent.enabled = false;
                    }
                    else
                    {
                        outlineComponent.enabled = true;
                        outlineComponent.OutlineColor = MyPlayerManager.myTeamColour;
                    }
                }
                foreach (Renderer renderer in renderers)
                {
                    renderer.material.color = MyPlayerManager.myTeamColour;
                }
                MyPlayerManager.SetPlayerOnSpawn(this);
            }
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
    }
}
