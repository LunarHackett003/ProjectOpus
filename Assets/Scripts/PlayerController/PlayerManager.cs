using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
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
        public PlayerEntity LivingPlayer;
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

        public NetworkObject reviveItemPrefab;
        NetworkObject reviveItemInstance;

        public int currentSpectateIndex;
        bool spectating;
        Transform originalTrackingTarget;
        public CinemachineCamera spectatorCamera;
        public Transform spectatorCamParent;
        bool firstPersonSpectating;

        Vector2 spectatorLookAngle;

        public NetworkVariable<int> timeUntilSpawn = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        bool canRespawn = true;
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

                #region Input Subscription
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
                #endregion

                timeUntilSpawn.OnValueChanged += RespawnTimeChanged;
            }
            else
            {
                myUI.gameObject.SetActive(false);
            }
            UpdateAllPlayerColours();
        }
        void RespawnTimeChanged(int previous, int current)
        {
            canRespawn = current <= 0;
        }
        private void Special_performed(InputAction.CallbackContext obj)
        {
            if (LivingPlayer != null)
            {

            }
        }

        private void CycleWeapon_performed(InputAction.CallbackContext obj)
        {
            if (LivingPlayer != null)
            {
                
            }
        }
        private void Reload_performed(InputAction.CallbackContext obj)
        {
            if (LivingPlayer)
            {

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

            if (LivingPlayer != null && spectating)
            {
                Spectate_RPC(true, -1, reviveItemInstance);
            }
        }

        private void Fire_performed(InputAction.CallbackContext obj)
        {
            fireInput = obj.ReadValueAsButton();
            if (LivingPlayer != null && spectating)
            {
                Spectate_RPC(true, 1, reviveItemInstance);
            }
        }

        public void SpawnReviveItem(Vector3 lastPos = default)
        {
            if(reviveItemInstance == null)
            {
                reviveItemInstance = NetworkManager.SpawnManager.InstantiateAndSpawn(reviveItemPrefab, OwnerClientId, position: lastPos);
            }
            Spectate_RPC(true, 0, reviveItemInstance);

            StartCoroutine(SpawnCountdown());
        }

        [Rpc(SendTo.Owner)]
        public void Spectate_RPC(bool spectating, int indexChange, NetworkObjectReference nor)
        {
            if (!this.spectating)
            {
                currentSpectateIndex = (int)OwnerClientId;
            }
            this.spectating = spectating;
            spectatorCamera.enabled = spectating && !firstPersonSpectating;
            if (!spectating && LivingPlayer != null)
            {
                LivingPlayer.viewmodelCamera.enabled = true;
                LivingPlayer.viewCineCam.enabled = true;

                LivingPlayer.worldCineCam.enabled = true;
                LivingPlayer.worldCineCam.Target.TrackingTarget = originalTrackingTarget;
                spectatorCamera.enabled = false;
                return;
            }




            currentSpectateIndex += indexChange;
            currentSpectateIndex %= MatchManager.Instance.playersOnTeam[(int)teamIndex.Value];
            PlayerManager target = playersByID[(uint)currentSpectateIndex];
            if(target.LivingPlayer == null)
            {
                return;
            }

            if (target.LivingPlayer.Alive)
            {
                if (firstPersonSpectating && LivingPlayer != null)
                {
                    spectatorCamera.enabled = false;
                    LivingPlayer.worldCineCam.Target.TrackingTarget = target.LivingPlayer.worldCineCam.Target.TrackingTarget;
                    return;
                }
                else
                {
                    spectatorCamera.Target.TrackingTarget = target.LivingPlayer.transform;
                }
            }
            else
            {
                if (nor.TryGet(out reviveItemInstance))
                {
                    spectatorCamera.Target.TrackingTarget = target.reviveItemInstance.transform;
                }
            }
            LivingPlayer.viewmodelCamera.enabled = false;
            LivingPlayer.viewCineCam.enabled = false;
            LivingPlayer.worldCineCam.enabled = false;
        }

        public void RespawnPlayer(bool revived, Vector3 lastPos = default)
        {
            if (revived)
            {
                MatchManager.Instance.RequestSpawn_RPC(OwnerClientId, revived: true, position: lastPos);
            }
            else
            {
                MatchManager.Instance.RequestSpawn_RPC(OwnerClientId, primaryWeaponIndex, gadget1Index, gadget2Index, gadget3Index, specialIndex);
            }
            Spectate_RPC(false, 0, reviveItemInstance);
            if (reviveItemInstance)
            {
                reviveItemInstance.Despawn();
            }
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

                }
            }
        }
        [Rpc(SendTo.Owner)]
        public void SpawnPlayer_RPC()
        {
            print("received spawn message, attempting to find us somewhere to spawn!");
            requestingSpawn = false;

            onSpawnReceived?.Invoke();

            if (hud != null)
            {
                hud.InitialiseHUD();
            }
        }
        public void ReadyUpPressed()
        {
            RespawnPlayer(false);
        }
        public void SetPlayerOnSpawn(PlayerEntity spawnedPlayer)
        {
            LivingPlayer = spawnedPlayer;

            originalTrackingTarget = LivingPlayer.worldCineCam.Target.TrackingTarget;
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
                        LivingPlayer.currentHealth.Value = 0;
                    }
                }
            }

            if (IsOwner)
            {
                readyButton.interactable = canRespawn;
            }
        }

        IEnumerator SpawnCountdown()
        {
            while (timeUntilSpawn.Value > 0)
            {
                yield return new WaitForSeconds(1);
                timeUntilSpawn.Value--;
            }
        }
        private void Update()
        {
            if (spectating)
            {
                spectatorLookAngle += new Vector2(PlayerSettings.Instance.settingsContainer.mouseLookSpeedY * -lookInput.y, PlayerSettings.Instance.settingsContainer.mouseLookSpeedX * lookInput.x) * Time.smoothDeltaTime;
                spectatorLookAngle.x = Mathf.Clamp(spectatorLookAngle.x, -85, 85);
                spectatorLookAngle.y %= 360;

                spectatorCamParent.SetPositionAndRotation(spectatorCamera.Target.TrackingTarget.position, Quaternion.Euler(spectatorLookAngle.x, spectatorLookAngle.y, 0));
            }
        }
    }
}
