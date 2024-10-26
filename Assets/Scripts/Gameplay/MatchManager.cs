using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
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
            base.OnNetworkDespawn();
        }
        private void SceneManager_OnSceneEvent(SceneEvent sceneEvent)
        {
            if(sceneEvent.SceneEventType == SceneEventType.LoadComplete)
            {
                if (IsServer)
                {
                    spawnpointHolder = FindAnyObjectByType<SpawnpointHolder>();
                    PlayerSpawn(sceneEvent.ClientId, true);
                }
            }
        }
        void PlayerSpawn(ulong clientID, bool updateSpawnpoint)
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
            if (updateSpawnpoint)
            {
                (Vector3 pos, Quaternion rot) = spawnpointHolder.FindSpawnpoint();
                n.GetComponent<NetworkTransform>().Teleport(pos, rot, Vector3.one);
            }
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
