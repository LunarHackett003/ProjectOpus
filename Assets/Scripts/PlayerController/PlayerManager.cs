
using Steamworks;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Opus
{
    public class PlayerManager : NetworkBehaviour
    {
        public static HashSet<PlayerManager> playerManagers = new HashSet<PlayerManager>();
        public NetworkObject playerPrefab;
        public NetworkVariable<string> playerName = new(writePerm: NetworkVariableWritePermission.Owner);
        private void Start()
        {
            playerManagers.Add(this);
        }
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsOwner)
            {
                NetworkManager.SceneManager.OnLoadComplete += SceneManager_OnLoadComplete;
                playerName.Value = SteamClient.Name;
            }
        }

        private void SceneManager_OnLoadComplete(ulong clientId, string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode)
        {
            AskForPlayer_RPC();
        }

        [Rpc(SendTo.Server)]
        void AskForPlayer_RPC()
        {
            NetworkManager.SpawnManager.InstantiateAndSpawn(playerPrefab, OwnerClientId, false, false, false, transform.position, Quaternion.identity);
        }
    }
}
