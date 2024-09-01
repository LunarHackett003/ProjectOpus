using Eflatun.SceneReference;
using FishNet;
using FishNet.Managing;
using Steamworks;
using Steamworks.Data;
using UnityEngine;

namespace Opus
{
    public class SteamTester : MonoBehaviour
    {
        public static SteamTester Instance { get; private set; }

        public FishyFacepunch.FishyFacepunch ffp;
        public Lobby? currentLobby;
        public ulong hostID;

        public GameObject hostButton, quitButton;

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
            if (InstanceFinder.IsHostStarted)
            {
                return;
            }
            StartClient(currentLobby.Value.Owner.Id);
        }
        public void DisconnectFromServer()
        {
            currentLobby?.Leave();
            if (InstanceFinder.IsHostStarted)
            {
                InstanceFinder.ServerManager.OnServerConnectionState -= ServerManager_OnServerConnectionState;
                InstanceFinder.ServerManager.StopConnection(false);
                InstanceFinder.ClientManager.StopConnection();

            }
            else
            {
                InstanceFinder.ClientManager.OnClientConnectionState -= ClientManager_OnClientConnectionState;
                InstanceFinder.ClientManager.StopConnection();
            }
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
            InstanceFinder.ServerManager.OnServerConnectionState += ServerManager_OnServerConnectionState;
            InstanceFinder.ServerManager.StartConnection();
            InstanceFinder.ClientManager.StartConnection();
            currentLobby = await SteamMatchmaking.CreateLobbyAsync(maxMembers);
            InstanceFinder.SceneManager.LoadGlobalScenes(new(SceneLoader.Instance.gameScene.Name) { ReplaceScenes = FishNet.Managing.Scened.ReplaceOption.All});
        }
        public void StartClient(SteamId _steamID)
        {
            InstanceFinder.ClientManager.OnClientConnectionState += ClientManager_OnClientConnectionState;
            ffp.SetClientAddress(_steamID.ToString());
            if (InstanceFinder.ClientManager.StartConnection())
            {
                print("Client started successfully!");

            }
        }

        private void ClientManager_OnClientConnectionState(FishNet.Transporting.ClientConnectionStateArgs obj)
        {
            if(obj.ConnectionState == FishNet.Transporting.LocalConnectionState.Started)
            {
                print("Started connection!");
                hostButton.SetActive(false);
                quitButton.SetActive(true);
            }
            else if (obj.ConnectionState == FishNet.Transporting.LocalConnectionState.Stopped)
            {
                print("Stopped connection!");
                hostButton.SetActive(true);
                quitButton.SetActive(false);
            }
        }

        private void ServerManager_OnServerConnectionState(FishNet.Transporting.ServerConnectionStateArgs obj)
        {
            if(obj.ConnectionState == FishNet.Transporting.LocalConnectionState.Started)
            {
                print("Server started!");
            }
        }
    }
}
