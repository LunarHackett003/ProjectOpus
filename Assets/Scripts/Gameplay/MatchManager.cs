using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor.PackageManager;
using UnityEngine;

namespace Opus
{
    public class MatchManager : NetworkBehaviour
    {

        public static MatchManager Instance;

        public NetworkVariable<Dictionary<ulong, uint>> clientsOnTeams = new(new(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public int numberOfTeamsAllowed;
        public NetworkVariable<Dictionary<int, int>> teamScores = new(new(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public SpawnpointHolder spawnpointHolder;
        public override void OnNetworkSpawn()
        {
            Instance = this;
            base.OnNetworkSpawn();
            NetworkManager.OnConnectionEvent += ConnectionEvent;
            NetworkManager.SceneManager.OnSceneEvent += SceneManager_OnSceneEvent;
        }
        public override void OnNetworkDespawn()
        {
            NetworkManager.OnConnectionEvent -= ConnectionEvent;
            NetworkManager.SceneManager.OnSceneEvent -= SceneManager_OnSceneEvent;
            Instance = null;
            base.OnNetworkDespawn();
        }
        private void SceneManager_OnSceneEvent(SceneEvent sceneEvent)
        {
            if(sceneEvent.SceneEventType == SceneEventType.LoadComplete)
            {
                if (IsServer)
                {
                    spawnpointHolder = FindAnyObjectByType<SpawnpointHolder>();
                    SetPlayerTeam(sceneEvent.ClientId);
                }
            }
        }
        [Rpc(SendTo.Server)]
        public void RequestSpawn_RPC(ulong clientID)
        {
            if (PlayerManager.playersByID.TryGetValue(clientID, out PlayerManager p))
            {
                if (p.LivingPlayer == null)
                {
                    p.LivingPlayer = NetworkManager.SpawnManager.InstantiateAndSpawn(p.playerPrefab, clientID).GetComponent<PlayerController>();
                }
                (Vector3 pos, Quaternion rot) = spawnpointHolder.FindSpawnpoint();

                p.LivingPlayer.transform.SetPositionAndRotation(pos, rot);
                p.SpawnPlayer_RPC();
            }
        }
        void SetPlayerTeam(ulong clientID)
        {
            uint team = FindSmallestTeam();
            clientsOnTeams.Value.TryAdd(clientID, team);
            print($"added client {clientID} to {team}");

            NetworkObject n = NetworkManager.ConnectedClients[clientID].PlayerObject;
            PlayerManager p = n.GetComponent<PlayerManager>();
            if (team == 0)
                p.UpdateTeamIndex(0, 0);
            else
                p.teamIndex.Value = team;
        }
        private void ConnectionEvent(NetworkManager manager, ConnectionEventData eventData)
        {
            if (!IsServer)
                return;

            if(eventData.EventType == Unity.Netcode.ConnectionEvent.ClientConnected)
            {

            }
            else if(eventData.EventType == Unity.Netcode.ConnectionEvent.ClientDisconnected)
            {
                if (clientsOnTeams.Value.ContainsKey(eventData.ClientId))
                {
                    clientsOnTeams.Value.Remove(eventData.ClientId);
                }
            }
        }
        uint FindSmallestTeam()
        {
            if(NetworkManager.ConnectedClients.Count == 0)
            {
                return 0;
            }
            else
            {
                //Where key is the team and value is the number of players on the team
                Dictionary<uint, uint> playersOnTeams = new();
                uint smallestTeamIndex = 0;
                uint smallestTeamPlayers = 100;
                for (uint i = 0; i < numberOfTeamsAllowed; i++)
                {
                    playersOnTeams.Add(i, 0);
                }
                foreach (KeyValuePair<ulong, uint> item in clientsOnTeams.Value)
                {
                    playersOnTeams[item.Value]++;
                }
                for (uint i = 0; i < numberOfTeamsAllowed; i++)
                {
                    if (playersOnTeams[i] < smallestTeamPlayers)
                    {
                        smallestTeamIndex = i;
                        smallestTeamPlayers = playersOnTeams[i];
                    }
                }
                return smallestTeamIndex;
            }
        }
    }
}
