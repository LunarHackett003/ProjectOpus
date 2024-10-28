using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace Opus
{
    public class PlayerManager : NetworkBehaviour
    {
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

        public Color myTeamColour;
        public Vector3 spawnPos;
        public Quaternion spawnRot;

        public Canvas myUI;
        public CanvasGroup gameplayUI, deadUI;

        public Button readyButton;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            teamIndex.OnValueChanged += UpdateTeamIndex;
            UpdateTeamIndex(0, teamIndex.Value);
            playersByID.TryAdd(OwnerClientId, this);
            if (!IsOwner)
            {
                myUI.gameObject.SetActive(false);
            }
            else
            {
                MyTeam = teamIndex.Value;
            }
            UpdateAllPlayerColours();
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
            if(deadUI != null)
            {
                deadUI.alpha = 0;
                deadUI.blocksRaycasts = false;
                deadUI.interactable = false;
            }
            if(gameplayUI != null)
            {
                gameplayUI.alpha = 1;
            }
        }
        public void ReadyUpPressed()
        {
            MatchManager.Instance.RequestSpawn_RPC(OwnerClientId);
            readyButton.interactable = false;
        }
        public void SetPlayerOnSpawn(PlayerController spawnedPlayer)
        {
            LivingPlayer = spawnedPlayer;
            LivingPlayer.transform.SetPositionAndRotation(spawnPos, spawnRot);
        }
    }
}
