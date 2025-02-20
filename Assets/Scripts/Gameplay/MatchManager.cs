using JetBrains.Annotations;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Opus
{
    public class MatchManager : ONetBehaviour
    {

        public static MatchManager Instance;
        public NetworkVariable<Dictionary<ulong, uint>> clientsOnTeams = new(new(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public int numberOfTeamsAllowed;
        public NetworkVariable<Dictionary<uint, uint>> teamScores = new(new(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkList<int> playersOnTeam = new(new int[20], NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public SpawnpointHolder spawnpointHolder;
        public EquipmentList weapons;
        public EquipmentList gadgets;
        public int maxRespawnTime = 10;
        public bool[] lockedSlots = new bool[5];
        public float stunMoveSpeedMultiplier, stunLookSpeedMultiplier;
        Texture2D tex;

        public NetworkVariable<bool> GameInProgress = new(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public float burnDamagePerStack = 1, burnDamageTickTime = 0.33f;

        public override void OnNetworkSpawn()
        {
            Instance = this;

            base.OnNetworkSpawn();
            NetworkManager.OnConnectionEvent += ConnectionEvent;
            NetworkManager.SceneManager.OnSceneEvent += SceneManager_OnSceneEvent;

            if (IsServer)
            {

                TeamsChanged(new(), clientsOnTeams.Value);
                var n = NetworkManager.Singleton.SpawnManager.InstantiateAndSpawn(SessionManager.Instance.selectedGameModePrefab, 0, false, false, false, default, default);

                for (uint i = 0; i < numberOfTeamsAllowed; i++)
                {
                    teamScores.Value.TryAdd(i, 0);
                }
            }
        }
        public override void OnNetworkDespawn()
        {
            NetworkManager.OnConnectionEvent -= ConnectionEvent;
            NetworkManager.SceneManager.OnSceneEvent -= SceneManager_OnSceneEvent;

            Instance = null;
            base.OnNetworkDespawn();
        }

        

        public void TeamsChanged(Dictionary<ulong, uint> previous, Dictionary<ulong, uint> current)
        {
            playersOnTeam.Clear();
            for (int i = 0; i < numberOfTeamsAllowed; i++)
            {
                playersOnTeam.Add(0);
            };
            print("Updating teams!");
            foreach (var item in current)
            {
                print($"found a player on team {item.Value}");
                playersOnTeam[(int)item.Value]++;
            }
            for (int i = 0; i < numberOfTeamsAllowed; i++)
            {

            }
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
        public void RequestSpawn_RPC(ulong clientID, int primaryWeaponIndex = -1, int gadgetOneIndex = -1, int gadgetTwoIndex = -1, int gadgetThreeIndex = -1, int specialIndex = -1, bool revived = false, Vector3 position = default)
        {
            if (PlayerManager.playersByID.TryGetValue(clientID, out PlayerManager p))
            {
                if (p.Character == null)
                {
                    p.Character = NetworkManager.SpawnManager.InstantiateAndSpawn(p.playerPrefab, clientID).GetComponent<PlayerEntity>();
                }
                p.Character.currentHealth.Value = p.Character.MaxHealth;
                p.timeUntilSpawn.Value = maxRespawnTime;
                if (!revived)
                {
                    (Vector3 pos, Quaternion rot) = spawnpointHolder.FindSpawnpoint(p.teamIndex.Value);

                    for (int i = 0; i < p.Character.wc.slots.Length; i++)
                    {
                        if (p.Character.wc.slots[i] != null)
                            p.Character.wc.slots[i].NetworkObject.Despawn();
                    }

                    SpawnWeaponsForPlayer(clientID, p, primaryWeaponIndex, gadgetOneIndex, gadgetTwoIndex, gadgetThreeIndex, specialIndex);
                    p.Character.Teleport_RPC(pos, Quaternion.identity);

                    p.SpawnPlayer_RPC();
                }
                else
                {
                    p.Character.Teleport_RPC(position, Quaternion.identity);
                    p.SpawnPlayer_RPC();
                }
            }
        }
        void SpawnWeaponsForPlayer(ulong clientID, PlayerManager p, int primaryWeaponIndex = -1, int gadgetOneIndex = -1, int gadgetTwoIndex = -1, int gadgetThreeIndex = -1, int specialIndex = -1)
        {
            if (primaryWeaponIndex > -1 && primaryWeaponIndex < weapons.equipment.Length)
            {
                if(p.Character != null)
                {
                    p.Character.wc.slots[0] = SpawnWeapon(clientID, weapons.equipment[primaryWeaponIndex].equipmentPrefab, Slot.primary);
                }
            }
            else
            {
            }
            if (gadgetThreeIndex > -1 && gadgetThreeIndex < gadgets.equipment.Length)
            {
                if (p.Character != null)
                {
                    //p.LivingPlayer.wc.gadget3Ref.Value = SpawnWeapon(clientID, gadgets.equipment[gadgetThreeIndex].equipmentPrefab, Slot.gadget3);
                }
            }
            else
            {
            }
            if (gadgetOneIndex > -1 && gadgetOneIndex < gadgets.equipment.Length)
            {
                if (p.Character != null)
                {
                    //p.LivingPlayer.wc.gadget1Ref.Value = SpawnWeapon(clientID, gadgets.equipment[gadgetOneIndex].equipmentPrefab, Slot.gadget1);
                }
            }
            else
            {
            }
            if (gadgetTwoIndex > -1 && gadgetTwoIndex < gadgets.equipment.Length)
            {
                if (p.Character != null)
                {
                    //p.LivingPlayer.wc.gadget2Ref.Value = SpawnWeapon(clientID, weapons.equipment[gadgetTwoIndex].equipmentPrefab, Slot.gadget2);
                }
            }
            else
            {
            }
            if (specialIndex > -1 && specialIndex < gadgets.equipment.Length)
            {
                if (p.Character != null)
                {
                    //p.LivingPlayer.wc.specialRef.Value = SpawnWeapon(clientID, gadgets.equipment[specialIndex].equipmentPrefab, Slot.special);
                }
            }
            else
            {
            }
        }
        BaseEquipment SpawnWeapon(ulong clientID, NetworkObject netPrefab, Slot weaponSlot)
        {
            netPrefab = NetworkManager.SpawnManager.InstantiateAndSpawn(netPrefab, clientID, false, false, false, Vector3.zero, Quaternion.identity);
            if (netPrefab.TryGetComponent(out BaseEquipment be))
            {
                PlayerManager.playersByID[clientID].Character.wc.SetEquipmentSlot_RPC(be, (int)weaponSlot);
                return be;
            }
            else
            {
                return null;
            }
        }
        void SetPlayerTeam(ulong clientID)
        {
            uint team = FindSmallestTeam();
            if (clientsOnTeams.Value.TryAdd(clientID, team))
            {
                print($"added client {clientID} to {team}");
            }
            else
            {
                print($"failed to add client {clientID} to team {team}");
            }

            NetworkObject n = NetworkManager.ConnectedClients[clientID].PlayerObject;
            PlayerManager p = n.GetComponent<PlayerManager>();

            p.teamIndex.Value = team;
            if (team == 0)
                p.UpdateTeamIndex(0, 0);
            else
                p.teamIndex.Value = team;
            TeamsChanged(new(), clientsOnTeams.Value);

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
        public override void OFixedUpdate()
        {
            if (!IsHost && !IsServer)
                return;
        }

        private void OnGUI()
        {
            GUILayout.BeginVertical();
            foreach (var item in teamScores.Value)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Box($"{item.Key} --- {item.Value}");
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        public void SetScoreForTeam(uint teamIndex = 0, uint teamScore = 10, bool additive = false, uint clientID = 0, uint extraPlayerScore = 0)
        {
            if (teamScores.Value.ContainsKey(teamIndex))
            {
                Debug.Log($"Awarding {(additive ? "+" : "")} {teamScore} points to team {teamIndex}! {(extraPlayerScore > 0 ? $"Awarding points to {clientID}" : "")}");
                if (additive)
                {
                    teamScores.Value[teamIndex] += teamScore;
                    
                }
                else
                {
                    teamScores.Value[teamIndex] = teamScore;
                }
                foreach (var item in PlayerManager.playersByID)
                {
                    if (item.Value.teamIndex.Value == teamIndex)
                    {
                        item.Value.objectivePoints.Value += extraPlayerScore;
                    }
                }
            }
            else
            {
                Debug.LogWarning($"Invalid team index given!\nTeam Index {teamIndex} does not exist, adding this team to the scores!");
                teamScores.Value.TryAdd(teamIndex, teamScore);
                teamScores.SetDirty(true);
            }
        }
    }
}
