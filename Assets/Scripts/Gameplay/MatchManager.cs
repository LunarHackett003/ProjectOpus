using JetBrains.Annotations;
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

        public NetworkList<int> playersOnTeam = new(new int[20], NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public SpawnpointHolder spawnpointHolder;

        public EquipmentList weapons;
        public EquipmentList gadgets;

        public int maxRespawnTime = 10;

        public bool[] lockedSlots = new bool[5];

        [Tooltip("The amount added to the mech readiness every tick. This is synchronised with the players every 10 seconds.")]
        public float mechReadySpeed;
        [Tooltip("The amount added to the mech's special readiness every tick. This is synchronised with the players every 10 seconds.")]
        public float mechSpecialSpeed;
        public override void OnNetworkSpawn()
        {
            Instance = this;
            base.OnNetworkSpawn();
            NetworkManager.OnConnectionEvent += ConnectionEvent;
            NetworkManager.SceneManager.OnSceneEvent += SceneManager_OnSceneEvent;

            TeamsChanged(new(), clientsOnTeams.Value);
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
                if (p.LivingPlayer == null)
                {
                    p.LivingPlayer = NetworkManager.SpawnManager.InstantiateAndSpawn(p.playerPrefab, clientID).GetComponent<PlayerEntity>();
                }
                p.LivingPlayer.currentHealth.Value = p.LivingPlayer.MaxHealth;
                p.timeUntilSpawn.Value = maxRespawnTime;
                if (!revived)
                {
                    (Vector3 pos, Quaternion rot) = spawnpointHolder.FindSpawnpoint(p.teamIndex.Value);

                    SpawnWeaponsForPlayer(clientID, p, primaryWeaponIndex, gadgetOneIndex, gadgetTwoIndex, gadgetThreeIndex, specialIndex);
                    p.LivingPlayer.Teleport_RPC(pos, Quaternion.identity);

                    p.SpawnPlayer_RPC();
                }
                else
                {
                    p.LivingPlayer.Teleport_RPC(position, Quaternion.identity);
                    p.SpawnPlayer_RPC();
                }
            }
        }
        void SpawnWeaponsForPlayer(ulong clientID, PlayerManager p, int primaryWeaponIndex = -1, int gadgetOneIndex = -1, int gadgetTwoIndex = -1, int gadgetThreeIndex = -1, int specialIndex = -1)
        {
            if (primaryWeaponIndex > -1 && primaryWeaponIndex < weapons.equipment.Length)
            {
                if(p.LivingPlayer != null)
                {
                    //p.LivingPlayer.wc.weaponRef.Value = SpawnWeapon(clientID, weapons.equipment[primaryWeaponIndex].equipmentPrefab, Slot.primary);
                }
            }
            else
            {
            }
            if (gadgetThreeIndex > -1 && gadgetThreeIndex < gadgets.equipment.Length)
            {
                if (p.LivingPlayer != null)
                {
                    //p.LivingPlayer.wc.gadget3Ref.Value = SpawnWeapon(clientID, gadgets.equipment[gadgetThreeIndex].equipmentPrefab, Slot.gadget3);
                }
            }
            else
            {
            }
            if (gadgetOneIndex > -1 && gadgetOneIndex < gadgets.equipment.Length)
            {
                if (p.LivingPlayer != null)
                {
                    //p.LivingPlayer.wc.gadget1Ref.Value = SpawnWeapon(clientID, gadgets.equipment[gadgetOneIndex].equipmentPrefab, Slot.gadget1);
                }
            }
            else
            {
            }
            if (gadgetTwoIndex > -1 && gadgetTwoIndex < gadgets.equipment.Length)
            {
                if (p.LivingPlayer != null)
                {
                    //p.LivingPlayer.wc.gadget2Ref.Value = SpawnWeapon(clientID, weapons.equipment[gadgetTwoIndex].equipmentPrefab, Slot.gadget2);
                }
            }
            else
            {
            }
            if (specialIndex > -1 && specialIndex < gadgets.equipment.Length)
            {
                if (p.LivingPlayer != null)
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
        float specialSyncTime;
        bool syncingSpecialTime;
        private void FixedUpdate()
        {
            if (!IsHost && !IsServer)
                return;

            specialSyncTime += Time.fixedDeltaTime;
            if (specialSyncTime > 10)
            {
                specialSyncTime = 0;
                syncingSpecialTime = true;
            }
            else
                syncingSpecialTime = false;
            if(PlayerManager.playersByID.Count > 0)
            {
                foreach (KeyValuePair<ulong, PlayerManager> item in PlayerManager.playersByID)
                {
                    if (item.Value.specialPercentage_noSync < 1)
                    {
                        item.Value.specialPercentage_noSync += (item.Value.mechDeployed.Value ? mechSpecialSpeed : mechReadySpeed) * Time.fixedDeltaTime;
                    }
                    if (item.Value.specialPercentage_noSync > 1)
                    {
                        item.Value.specialPercentage_noSync = 1;
                    }
                    if (syncingSpecialTime)
                    {
                        item.Value.specialPercentage.Value = item.Value.specialPercentage_noSync;
                    }
                }
            }
        }
    }
}
