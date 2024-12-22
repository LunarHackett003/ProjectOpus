using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Opus
{
    public class PlayerManager : NetworkBehaviour
    {
        public delegate void OnSpawnReceived();
        public OnSpawnReceived onSpawnReceived;

        public static Dictionary<ulong, PlayerManager> playersByID = new();

        public static uint MyTeam;

        public NetworkObject playerPrefab;
        public PlayerController LivingPlayer;
        public NetworkVariable<uint> teamIndex = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<uint> kills = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<uint> deaths = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<uint> assists = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<uint> revives = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<uint> supportPoints = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<uint> combatPoints = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<float> specialPercentage = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<bool> mechDeployed = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public float specialPercentage_noSync;

        public Color myTeamColour;
        public Vector3 spawnPos;
        public Quaternion spawnRot;

        public Canvas myUI;

        public Button readyButton;
        bool requestingSpawn = true;

        public PlayerHUD hud;

        public Vector2 moveInput, lookInput;
        public bool jumpInput;
        public bool crouchInput;
        public bool sprintInput;
        public bool fireInput;
        public bool secondaryInput;

        public ControlScheme controls;


        public int primaryWeaponIndex = -1, gadget1Index = -1, gadget2Index = -1, gadget3Index = -1, specialIndex = -1;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            teamIndex.OnValueChanged += UpdateTeamIndex;
            UpdateTeamIndex(0, teamIndex.Value);
            playersByID.TryAdd(OwnerClientId, this);
            if (IsOwner)
            {
                MyTeam = teamIndex.Value;

                specialPercentage.OnValueChanged += SpecialPercentageChanged;
                if (LoadoutUI.Instance != null)
                {
                    LoadoutUI.Instance.pm = this;
                }



                controls = new();
                controls.Player.Move.performed += Move_performed;
                controls.Player.Move.canceled += Move_performed;

                controls.Player.Look.performed += Look_performed;
                controls.Player.Look.canceled += Look_performed;

                controls.Player.Jump.performed += Jump_performed;
                controls.Player.Jump.canceled += Jump_performed;

                controls.Player.Crouch.performed += Crouch_performed;
                controls.Player.Crouch.canceled += Crouch_performed;

                controls.Player.Sprint.performed += Sprint_performed;
                controls.Player.Sprint.canceled += Sprint_performed;

                controls.Player.Fire.performed += Fire_performed;
                controls.Player.Fire.canceled += Fire_performed;

                controls.Player.Reload.performed += Reload_performed;
                controls.Player.Reload.canceled += Reload_performed;

                controls.Player.SecondaryInput.performed += SecondaryInput_performed;
                controls.Player.SecondaryInput.canceled += SecondaryInput_performed;

                controls.Player.CycleWeapon.performed += CycleWeapon_performed;

                controls.Player.Special.performed += Special_performed;
                controls.Enable();

            }
            else
            {
                myUI.gameObject.SetActive(false);
            }
            UpdateAllPlayerColours();
        }

        private void Special_performed(InputAction.CallbackContext obj)
        {
            if(LivingPlayer != null && LivingPlayer.wc != null && LivingPlayer.wc.special != null)
            {
                LivingPlayer.wc.TrySwitchWeapon((int)Slot.special);
            }
        }

        private void CycleWeapon_performed(InputAction.CallbackContext obj)
        {
            if (LivingPlayer != null && LivingPlayer.wc != null)
            {
                LivingPlayer.wc.TrySwitchWeapon(Mathf.FloorToInt(obj.ReadValue<float>()));
            }
        }
        private void Reload_performed(InputAction.CallbackContext obj)
        {
            if (LivingPlayer.wc != null)
            {
                if (obj.ReadValueAsButton() && !LivingPlayer.wc.networkAnimator.Animator.GetCurrentAnimatorStateInfo(0).IsTag("Reload"))
                {
                    LivingPlayer.wc.TryReload();
                }
            }
        }
        private void Sprint_performed(InputAction.CallbackContext obj)
        {
            sprintInput = obj.ReadValueAsButton();
        }
        private void Crouch_performed(InputAction.CallbackContext obj)
        {
            crouchInput = obj.ReadValueAsButton();
        }

        private void Jump_performed(InputAction.CallbackContext obj)
        {
            jumpInput = obj.ReadValueAsButton();
        }

        private void Look_performed(InputAction.CallbackContext obj)
        {
            lookInput = obj.ReadValue<Vector2>();
        }

        private void Move_performed(InputAction.CallbackContext obj)
        {
            moveInput = obj.ReadValue<Vector2>();
        }
        private void SecondaryInput_performed(InputAction.CallbackContext obj)
        {
            secondaryInput = obj.ReadValueAsButton();
        }

        private void Fire_performed(InputAction.CallbackContext obj)
        {
            fireInput = obj.ReadValueAsButton();
        }








        void SpecialPercentageChanged(float previous, float current)
        {
            specialPercentage_noSync = current;
        }
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            if (playersByID.ContainsKey(OwnerClientId))
                playersByID.Remove(OwnerClientId);
        }
        public void UpdateTeamIndex(uint previous, uint current)
        {
            myTeamColour = PlayerSettings.Instance.teamColours[current];
            if (IsOwner)
            {
                MyTeam = current;
            }

            UpdateAllPlayerColours();
        }

        void UpdateAllPlayerColours()
        {
            foreach (var item in playersByID)
            {
                if (item.Value.LivingPlayer != null)
                {
                    item.Value.LivingPlayer.UpdatePlayerColours();
                }
            }
        }
        [Rpc(SendTo.Owner)]
        public void SpawnPlayer_RPC(Vector3 pos, Quaternion rot)
        {
            print("received spawn message, attempting to find us somewhere to spawn!");
            requestingSpawn = false;

            onSpawnReceived?.Invoke();

            LivingPlayer.GetComponent<NetworkTransform>().Teleport(pos, rot, Vector3.one);

            if (hud != null)
            {
                hud.InitialiseHUD();
            }
        }
        public void ReadyUpPressed()
        {
            MatchManager.Instance.RequestSpawn_RPC(OwnerClientId, primaryWeaponIndex, gadget1Index, gadget2Index, gadget3Index, specialIndex);
        }
        public void SetPlayerOnSpawn(PlayerController spawnedPlayer)
        {
            LivingPlayer = spawnedPlayer;
            LivingPlayer.transform.SetPositionAndRotation(spawnPos, spawnRot);
        }
        private void FixedUpdate()
        {
            //We don't want to execute this if we are the host, as we already do this maths on the game manager.
            if (MatchManager.Instance != null && !IsHost)
            {
                if (specialPercentage_noSync < 1)
                {
                    specialPercentage_noSync += Time.fixedDeltaTime * (mechDeployed.Value ? MatchManager.Instance.mechSpecialSpeed : MatchManager.Instance.mechReadySpeed);
                    specialPercentage_noSync = Mathf.Clamp01(specialPercentage_noSync);
                } 
            }

            if (IsServer)
            {
                if(LivingPlayer != null)
                {
                    if(LivingPlayer.transform.position.y < -40 && LivingPlayer.CurrentHealth > 0)
                    {
                        LivingPlayer.CurrentHealth = 0;
                    }
                }
            }

            if (IsOwner)
            {
                readyButton.interactable = LivingPlayer == null || LivingPlayer.CurrentHealth <= 0;

            }
        }
    }
}
