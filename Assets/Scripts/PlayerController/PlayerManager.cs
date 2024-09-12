
using Steamworks;
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


        public NetworkVariable<int> primaryIndex = new(writePerm: NetworkVariableWritePermission.Owner), secondaryIndex = new(writePerm:NetworkVariableWritePermission.Owner),
            gadgetOneIndex = new(writePerm:NetworkVariableWritePermission.Owner), gadgetTwoIndex = new(writePerm:NetworkVariableWritePermission.Owner), specialIndex = new(writePerm:NetworkVariableWritePermission.Owner);

        private void Start()
        {
            playerManagers.Add(this);
        }
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            InputCollector = GetComponent<InputCollector>();
            loadoutManager = FindAnyObjectByType<LoadoutManager>();
            if (IsOwner)
            {
                playerName.Value = SteamClient.Name;
                if (loadoutManager)
                {
                    loadoutManager.onLoadoutUpdated += UpdateLoadout;
                }
            }
        }
        public void UpdateLoadout()
        {
            primaryIndex.Value = loadoutManager.primaryIndex;
            secondaryIndex.Value = loadoutManager.secondaryIndex;
            gadgetOneIndex.Value = loadoutManager.gadget1Index;
            gadgetTwoIndex.Value = loadoutManager.gadget2Index;
            specialIndex.Value = loadoutManager.specialIndex;
        }
        public void BestowPlayer()
        {
            NetworkManager.SceneManager.OnLoadComplete += SceneLoadComplete;
        }

        private void SceneLoadComplete(ulong clientId, string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode)
        {
            //Wait until the player has loaded into the scene
            NetworkManager.SpawnManager.InstantiateAndSpawn(playerPrefab, OwnerClientId, false, false, false, transform.position, Quaternion.identity);
        }
    }
}
