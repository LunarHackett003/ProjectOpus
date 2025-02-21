using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Opus
{
    public class PlayerManager : ONetBehaviour
    {

        public static Dictionary<ulong, PlayerManager> playersByID = new();

        public static uint MyTeam;

        public NetworkObject playerPrefab;
        public PlayerEntity Character;
        public NetworkVariable<uint> teamIndex = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<uint> kills = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<uint> deaths = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<uint> assists = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<uint> revives = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<uint> supportPoints = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<uint> combatPoints = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<uint> objectivePoints = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public float specialPercentage_noSync;

        public Color myTeamColour;
        public Vector3 spawnPos;
        public Quaternion spawnRot;

        public Canvas myUI;

        public Button readyButton;
        bool requestingSpawn = true;

        public PlayerHUD hud;

        public Vector2 moveInput, lookInput;
        public bool jumpInput, crouchInput, sprintInput, fireInput, secondaryInput, reloadInput, interactInput, pickupInput;

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

                if (LoadoutUI.Instance != null)
                {
                    LoadoutUI.Instance.pm = this;
                }

                #region Input Subscription
                controls = new();

                controls.Player.Pause.performed += Pause_performed;

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

                controls.Player.PickUp.performed += PickUp_performed;
                controls.Player.PickUp.canceled += PickUp_performed;

                controls.Player.Interact.performed += Interact_performed;
                controls.Player.Interact.canceled += Interact_performed;

                controls.Enable();
                #endregion

                timeUntilSpawn.OnValueChanged += RespawnTimeChanged;

                PauseMenu.Instance.PauseGame(false);
                PauseMenu.Instance.FreeCursor(true);

            }
            else
            {
                myUI.gameObject.SetActive(false);
            }
            UpdateAllPlayerColours();
        }

        private void Pause_performed(InputAction.CallbackContext obj)
        {
            PauseMenu.Instance.gamePaused = !PauseMenu.Instance.gamePaused;

            if (PauseMenu.Instance.gamePaused)
            {
                moveInput = lookInput = Vector2.zero;
                jumpInput = crouchInput = sprintInput = fireInput = secondaryInput = interactInput = reloadInput = pickupInput = false;
            }
        }

        private void Interact_performed(InputAction.CallbackContext obj)
        {
            interactInput = obj.ReadValueAsButton() && !PauseMenu.Instance.IsPaused;
        }

        private void PickUp_performed(InputAction.CallbackContext obj)
        {
            pickupInput = obj.ReadValueAsButton() && !PauseMenu.Instance.IsPaused;
        }

        void RespawnTimeChanged(int previous, int current)
        {
            canRespawn = current <= 0;
        }
        private void Special_performed(InputAction.CallbackContext obj)
        {
            if (Character != null)
            {

            }
        }

        private void CycleWeapon_performed(InputAction.CallbackContext obj)
        {
            if (Character != null)
            {
                int index = (Character.wc.weaponIndex.Value + Mathf.RoundToInt(obj.ReadValue<float>())) % Character.wc.slots.Count;
                if (index < 0)
                    index = Character.wc.slots.Count + index;
                Character.wc.TrySwitchWeapon(index);
            }
        }
        private void Reload_performed(InputAction.CallbackContext obj)
        {
            reloadInput = obj.ReadValueAsButton() && !PauseMenu.Instance.IsPaused;
        }
        private void Sprint_performed(InputAction.CallbackContext obj)
        {
            sprintInput = obj.ReadValueAsButton() && !PauseMenu.Instance.IsPaused;

        }
        private void Crouch_performed(InputAction.CallbackContext obj)
        {
            crouchInput = obj.ReadValueAsButton() && !PauseMenu.Instance.IsPaused;

        }

        private void Jump_performed(InputAction.CallbackContext obj)
        {
            jumpInput = obj.ReadValueAsButton() && !PauseMenu.Instance.IsPaused;

        }



        private void Look_performed(InputAction.CallbackContext obj)
        {
            lookInput = PauseMenu.Instance.IsPaused ? Vector2.zero : obj.ReadValue<Vector2>();
        }

        private void Move_performed(InputAction.CallbackContext obj)
        {
            moveInput = PauseMenu.Instance.IsPaused ? Vector2.zero : obj.ReadValue<Vector2>();

        }
        private void SecondaryInput_performed(InputAction.CallbackContext obj)
        {
            secondaryInput = obj.ReadValueAsButton() && !PauseMenu.Instance.IsPaused;

            if (Character != null && spectating)
            {
                Spectate_RPC(true, -1);
            }
        }

        private void Fire_performed(InputAction.CallbackContext obj)
        {
            fireInput = obj.ReadValueAsButton() && !PauseMenu.Instance.IsPaused;
            if (Character != null && spectating)
            {
                Spectate_RPC(true, 1);
            }
        }

        public void SpawnReviveItem(Vector3 lastPos = default)
        {
            if (reviveItemInstance == null)
            {
                reviveItemInstance = NetworkManager.SpawnManager.InstantiateAndSpawn(reviveItemPrefab, OwnerClientId, position: lastPos);
            }
            Spectate_RPC(true, 0);

            StartCoroutine(SpawnCountdown());
        }

        [Rpc(SendTo.Owner)]
        public void Spectate_RPC(bool spectating, int indexChange)
        {
            if (!this.spectating)
            {
                currentSpectateIndex = (int)OwnerClientId;
            }
            this.spectating = spectating;
            spectatorCamera.enabled = spectating && !firstPersonSpectating;
            if (!spectating && Character != null)
            {
                Character.viewmodelCamera.enabled = true;
                Character.viewCineCam.enabled = true;

                Character.worldCineCam.enabled = true;
                Character.worldCineCam.Target.TrackingTarget = originalTrackingTarget;
                spectatorCamera.enabled = false;
                return;
            }




            currentSpectateIndex += indexChange;
            currentSpectateIndex %= MatchManager.Instance.playersOnTeam[(int)teamIndex.Value];
            PlayerManager target = playersByID.ToArray()[currentSpectateIndex].Value;
            if (target.Character == null)
            {
                return;
            }

            if (target.Character.Alive)
            {
                if (firstPersonSpectating && Character != null)
                {
                    spectatorCamera.enabled = false;
                    Character.worldCineCam.Target.TrackingTarget = target.Character.worldCineCam.Target.TrackingTarget;
                    return;
                }
                else
                {
                    spectatorCamera.Target.TrackingTarget = target.Character.transform;
                }
            }
            else
            {

            }
            Character.viewmodelCamera.enabled = false;
            Character.viewCineCam.enabled = false;
            Character.worldCineCam.enabled = false;
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
            if (Character)
            {
                Character.SetCollidersEnabledState_RPC(true);
                Character.SetRenderersEnabledState_RPC(true);
            }
            Spectate_RPC(false, 0);
            PauseMenu.Instance.FreeCursor(false);
            if (reviveItemInstance)
            {
                reviveItemInstance.Despawn();
            }
        }

        [Rpc(SendTo.Owner)]
        public void SendHitmarker_RPC(DamageType dt)
        {
            hud.PlayHitmarker(dt);
        }

        public void ClientDied()
        {
            PauseMenu.Instance.FreeCursor(true);
            PlayerDied_RPC();
            Character.SetRenderersEnabledState_RPC(false);
            Spectate_RPC(true, 0);
        }
        [Rpc(SendTo.Server)]
        public void PlayerDied_RPC()
        {
            SendSpawnCooldown();
            Character.SetCollidersEnabledState_RPC(false);
            SpawnReviveItem(Character.LastGroundedPosition);
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
                if (item.Value.Character != null)
                {

                }
            }
        }
        [Rpc(SendTo.Owner, DeferLocal = true)]
        public void SpawnPlayer_RPC()
        {
            print("spawn received");
            if (hud != null)
            {
                print("initialising hud");
                hud.InitialiseHUD();
            }
            print("spawn complete");
            ResetCanSpawn_RPC();
        }
        [Rpc(SendTo.Server)]
        void ResetCanSpawn_RPC()
        {
            tryingToRespawn = false;
        }
        public void ReadyUpPressed()
        {
            RespawnPlayer(false);
        }
        public void SetPlayerOnSpawn(PlayerEntity spawnedPlayer)
        {
            Character = spawnedPlayer;

            originalTrackingTarget = Character.worldCineCam.Target.TrackingTarget;
        }
        public override void OFixedUpdate()
        {
            if (IsServer)
            {
                if (Character != null)
                {
                    if (Character.transform.position.y < -40 && Character.CurrentHealth > 0)
                    {
                        Character.currentHealth.Value = 0;
                    }
                }
            }

            if (IsOwner)
            {
                readyButton.interactable = canRespawn;
            }
            
        }

        bool tryingToRespawn;
        public void SendSpawnCooldown()
        {
            if(!tryingToRespawn)
                StartCoroutine(SpawnCountdown());
        }

        IEnumerator SpawnCountdown()
        {
            tryingToRespawn = true;
            yield return null;
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
