using Eflatun.SceneReference;
using Netcode.Transports.Facepunch;
using Steamworks;
using Steamworks.Data;
using Unity.Netcode;
using UnityEngine;

namespace Opus
{
    public class SteamTester : MonoBehaviour
    {
        public static SteamTester Instance { get; private set; }

        public FacepunchTransport transport;
        public Lobby? currentLobby;
        public ulong hostID;

        public GameObject hostButton, quitButton;
        public NetworkObject matchControllerPrefab;
        private void Awake()
        {
            if(Instance != null)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(Instance);
        }
        private void Start()
        {
            SteamMatchmaking.OnLobbyCreated += SteamMatchmaking_OnLobbyCreated;
            SteamMatchmaking.OnLobbyEntered += SteamMatchmaking_OnLobbyEntered;
            SteamMatchmaking.OnLobbyMemberJoined += SteamMatchmaking_OnLobbyMemberJoined;
            SteamMatchmaking.OnLobbyMemberLeave += SteamMatchmaking_OnLobbyMemberLeave;
            SteamMatchmaking.OnLobbyInvite += SteamMatchmaking_OnLobbyInvite;
            SteamMatchmaking.OnLobbyGameCreated += SteamMatchmaking_OnLobbyGameCreated;
            SteamFriends.OnGameLobbyJoinRequested += SteamFriends_OnGameLobbyJoinRequested;

            NetworkManager.Singleton.OnServerStarted += Singleton_OnServerStarted;
            NetworkManager.Singleton.OnClientStarted += ClientStarted;


        }
        private void OnDestroy()
        {
            SteamMatchmaking.OnLobbyCreated -= SteamMatchmaking_OnLobbyCreated;
            SteamMatchmaking.OnLobbyEntered -= SteamMatchmaking_OnLobbyEntered;
            SteamMatchmaking.OnLobbyMemberJoined -= SteamMatchmaking_OnLobbyMemberJoined;
            SteamMatchmaking.OnLobbyMemberLeave -= SteamMatchmaking_OnLobbyMemberLeave;
            SteamMatchmaking.OnLobbyInvite -= SteamMatchmaking_OnLobbyInvite;
            SteamMatchmaking.OnLobbyGameCreated -= SteamMatchmaking_OnLobbyGameCreated;
            SteamFriends.OnGameLobbyJoinRequested -= SteamFriends_OnGameLobbyJoinRequested;

            NetworkManager.Singleton.OnServerStarted -= Singleton_OnServerStarted;
            NetworkManager.Singleton.OnClientStarted -= ClientStarted;
        }

        private async void SteamFriends_OnGameLobbyJoinRequested(Lobby _lobby, SteamId _steamID)
        {
            RoomEnter joinedLobby = await _lobby.Join();
            if(joinedLobby != RoomEnter.Success)
            {
                Debug.LogWarning("Failed to create room");
            }
            else
            {
                currentLobby = _lobby;
                print("joined a lobby");
            }
        }

        private void SteamMatchmaking_OnLobbyGameCreated(Lobby __lobby, uint _ip, ushort _port, SteamId _steamID)
        {
            print("lobby created");
        }

        private void SteamMatchmaking_OnLobbyInvite(Friend _friend, Lobby _lobby)
        {
            print($"invite from {_friend.Name}");
        }

        private void SteamMatchmaking_OnLobbyMemberLeave(Lobby _lobby, Friend _friend)
        {
            print("member left...");
        }

        private void SteamMatchmaking_OnLobbyMemberJoined(Lobby _lobby, Friend _friend)
        {
            print("member joined");
        }

        private void SteamMatchmaking_OnLobbyEntered(Lobby _lobby)
        {
            if (NetworkManager.Singleton.IsHost)
            {
                return;
            }
            StartClient(currentLobby.Value.Owner.Id);
        }
        public void DisconnectFromServer()
        {
            currentLobby?.Leave();
            currentLobby = null;
            NetworkManager.Singleton.Shutdown();
            SceneLoader.Instance.LoadMenuScene();
        }
        private void SteamMatchmaking_OnLobbyCreated(Result _result, Lobby _lobby)
        {
            if(_result != Result.OK)
            {
                print("Lobby not created");
                return;
            }
            _lobby.SetPublic();
            _lobby.SetJoinable(true);
            _lobby.SetGameServer(_lobby.Owner.Id);
        }
        public async void StartHost(int maxMembers = 8)
        {
            currentLobby = await SteamMatchmaking.CreateLobbyAsync(maxMembers);
            NetworkManager.Singleton.StartHost();
            NetworkManager.Singleton.SpawnManager.InstantiateAndSpawn(matchControllerPrefab);
            NetworkManager.Singleton.SceneManager.LoadScene(SceneLoader.Instance.gameScene.Name, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }

        private void Singleton_OnServerStarted()
        {
            print("server started");
        }

        public void StartClient(SteamId _steamID)
        {
            transport.targetSteamId = _steamID;
            if (NetworkManager.Singleton.StartClient())
            {
                print("Client started successfully!");

            }
        }

        private void ClientStarted()
        {
            print("Client started");
        }
    }
}
