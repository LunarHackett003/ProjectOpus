
using Steamworks;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Opus
{
    public class PlayerManager : NetworkBehaviour
    {
        public static HashSet<PlayerManager> playerManagers = new();
        public NetworkObject playerPrefab;
        public NetworkVariable<FixedString32Bytes> playerName = new(writePerm: NetworkVariableWritePermission.Owner);

        public InputCollector InputCollector { get; private set; }
        public PlayerWeaponManager weaponManager;
        public PlayerMotor playerMotor;
        public PauseMenu pauseMenu;
        public LoadoutManager loadoutManager;

        public float spawnDelay = 1f;
        public NetworkVariable<int> primaryIndex = new(writePerm: NetworkVariableWritePermission.Owner), secondaryIndex = new(writePerm:NetworkVariableWritePermission.Owner),
            gadgetOneIndex = new(writePerm:NetworkVariableWritePermission.Owner), gadgetTwoIndex = new(writePerm:NetworkVariableWritePermission.Owner), specialIndex = new(writePerm:NetworkVariableWritePermission.Owner);
        bool pendingRespawn;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            transform.position = Vector3.up;
            playerManagers.Add(this);
            InputCollector = GetComponent<InputCollector>();
            loadoutManager = FindAnyObjectByType<LoadoutManager>();
            print($"owner: {OwnerClientId}");
            if (IsOwner)
            {
                playerName.Value = SteamClient.Name;
                if (loadoutManager)
                {
                    loadoutManager.onLoadoutUpdated += UpdateLoadout;
                    UpdateLoadout();
                }
            }
        }
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            playerManagers.Remove(this);
            if (IsOwner)
            {
                if (loadoutManager)
                {
                    loadoutManager.onLoadoutUpdated -= UpdateLoadout;
                }
            }
        }
        public void UpdateLoadout()
        {
            print("Updating loadout");
            primaryIndex.Value = loadoutManager.primaryIndex;
            secondaryIndex.Value = loadoutManager.secondaryIndex;
            gadgetOneIndex.Value = loadoutManager.gadget1Index;
            gadgetTwoIndex.Value = loadoutManager.gadget2Index;
            specialIndex.Value = loadoutManager.specialIndex;
        }
        private void FixedUpdate()
        {
            if (IsOwner && playerMotor != null && playerMotor.transform.position.y < -50 && !pendingRespawn)
            {
                pendingRespawn = true;
            }
        }
        public void BestowWeapons()
        {
            if(primaryIndex.Value > -1)
            {
                NetworkObject nob = NetworkManager.SpawnManager.InstantiateAndSpawn(loadoutManager.ValidLoadoutItemContainer.primary[primaryIndex.Value].prefab, OwnerClientId);
                weaponManager.primaryWeaponRef.Value = nob.GetComponent<BaseWeapon>();
            }
            if(secondaryIndex.Value > -1)
            {
                NetworkObject nob = NetworkManager.SpawnManager.InstantiateAndSpawn(loadoutManager.ValidLoadoutItemContainer.secondary[secondaryIndex.Value].prefab, OwnerClientId);
                weaponManager.secondaryWeaponRef.Value = nob.GetComponent<BaseWeapon>();
            }
            print("Updating weapons on clients");
            weaponManager.UpdateWeapons_RPC();
        }
        public void SpawnPlayer(Vector3 position, Quaternion rotation, ulong clientID)
        {
            if(playerMotor == null)
            {
                if (!IsServer)
                {
                    //Cannot spawn, do nothing here. We shouldn't even get here anyway.
                    print("How did you get here?");
                }
                else
                {
                    //Spawn the player at the position and rotation provided by the spawn manager
                    NetworkManager.SpawnManager.InstantiateAndSpawn(playerPrefab, clientID, position: position, rotation: rotation);
                }
            }
            else
            {
                SendPlayerSpawn_RPC(position, rotation);
            }
        }
        [Rpc(SendTo.Server)]
        public void RequestRespawn_RPC()
        {
            MatchController.Instance.SpawnPlayer(OwnerClientId);
        }
        [Rpc(SendTo.Owner)]
        public void SendPlayerSpawn_RPC(Vector3 position, Quaternion rotation)
        {
            pendingRespawn = false;
            playerMotor.transform.SetPositionAndRotation(position, rotation);
        }
    }
}
