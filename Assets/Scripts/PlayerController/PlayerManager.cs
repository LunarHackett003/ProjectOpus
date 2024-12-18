using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
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
            }
            else
            {
                myUI.gameObject.SetActive(false);
            }
            UpdateAllPlayerColours();
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
        public void SpawnPlayer_RPC()
        {
            requestingSpawn = false;

            onSpawnReceived?.Invoke();

            if(hud != null)
            {
                hud.InitialiseHUD();
            }
        }
        public void ReadyUpPressed()
        {
            MatchManager.Instance.RequestSpawn_RPC(OwnerClientId, primaryWeaponIndex, gadget1Index, gadget2Index, gadget3Index, specialIndex);
            readyButton.interactable = false;
        }
        public void SetPlayerOnSpawn(PlayerController spawnedPlayer)
        {
            LivingPlayer = spawnedPlayer;
            LivingPlayer.transform.SetPositionAndRotation(spawnPos, spawnRot);
        }
        private void FixedUpdate()
        {

            if (!IsOwner)
                return;
            if(LivingPlayer != null)
            {

                if(LivingPlayer.transform.position.y < -20)
                {
                    if (!requestingSpawn)
                    {
                        MatchManager.Instance.RequestSpawn_RPC(OwnerClientId);
                        requestingSpawn = true;
                    }
                    //The player has fallen out of bounds, we need to do something about this.
                }
                else
                {
                    if(requestingSpawn)
                        requestingSpawn = false;
                }
            }
            //We don't want to execute this if we are the host, as we already do this maths on the game manager.
            if (MatchManager.Instance != null && !IsHost)
            {
                if (specialPercentage_noSync < 1)
                {
                    specialPercentage_noSync += Time.fixedDeltaTime * (mechDeployed.Value ? MatchManager.Instance.mechSpecialSpeed : MatchManager.Instance.mechReadySpeed);
                    specialPercentage_noSync = Mathf.Clamp01(specialPercentage_noSync);
                } 
            }
        }
    }
}
