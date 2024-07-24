using Netcode.Transports.Facepunch;
using Steamworks;
using Steamworks.Data;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace opus.SteamIntegration {
    public class SteamLobbyManager : MonoBehaviour
    {
        protected static SteamLobbyManager _instance;
        public static SteamLobbyManager Instance 
        {
            get
            {
                if (!_instance)
                {
                    return _instance = new GameObject("TestSteamLobby").AddComponent<SteamLobbyManager>();
                }
                return _instance;
            } 
        }

        FacepunchTransport transport;
        Lobby? currentLobby;
        public ulong hostID;
        public uint maxMembers;
        public GameObject disconnectButton;
        public GameObject hostGameButton;

        public GameObject gameJoinedDisplay;
        private void Awake()
        {
            if (_instance == null)
                _instance = this;
            else
            {
                print("destroying duplicate instance");
                Destroy(gameObject);
                return;
            }


            DontDestroyOnLoad(gameObject);
            transport = FindAnyObjectByType<FacepunchTransport>();
            if (!transport)
            {
                Debug.LogWarning("No transport found! Disabling!", this);
                enabled = false;
                return;
            }
        }
        private void Start()
        {
            //Create callbacks for steam stuff
            SteamMatchmaking.OnLobbyCreated += LobbyCreated;
            SteamMatchmaking.OnLobbyEntered += LobbyEntered;
            SteamMatchmaking.OnLobbyMemberJoined += LobbyMemberJoined;
            SteamMatchmaking.OnLobbyMemberLeave += LobbyMemberLeave;
            SteamMatchmaking.OnLobbyInvite += LobbyInvite;
            SteamMatchmaking.OnLobbyGameCreated += LobbyGameCreated;
            SteamFriends.OnGameLobbyJoinRequested += GameLobbyJoinRequested;

            disconnectButton.SetActive(false);
            gameJoinedDisplay.SetActive(false);
            hostGameButton.SetActive(true);
        }
        private void OnDestroy()
        {
            SteamMatchmaking.OnLobbyCreated -= LobbyCreated;
            SteamMatchmaking.OnLobbyEntered -= LobbyEntered;
            SteamMatchmaking.OnLobbyMemberJoined -= LobbyMemberJoined;
            SteamMatchmaking.OnLobbyMemberLeave -= LobbyMemberLeave;
            SteamMatchmaking.OnLobbyInvite -= LobbyInvite;
            SteamMatchmaking.OnLobbyGameCreated -= LobbyGameCreated;
            SteamFriends.OnGameLobbyJoinRequested -= GameLobbyJoinRequested;


            if(NetworkManager.Singleton == null)
                return;
            NetworkManager.Singleton.OnServerStarted -= NetworkManager_ServerStarted;
            NetworkManager.Singleton.OnClientConnectedCallback -= NetworkManager_ClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= NetworkManager_ClientDisconnected;

        }
        private void OnApplicationQuit()
        {
            Disconnected();
        }
        private async void GameLobbyJoinRequested(Lobby arg1, SteamId arg2)
        {
            RoomEnter joinedLobby = await arg1.Join();
            if(joinedLobby != RoomEnter.Success)
            {
                print("Game Lobby Join Requested - failed");
            }
            else
            {
                print("Game Lobby Join Requested - success!");
                currentLobby = arg1;
            }
        }

        private void LobbyGameCreated(Lobby arg1, uint arg2, ushort arg3, SteamId arg4)
        {
            print("lobby was created!");
        }

        private void LobbyInvite(Friend arg1, Lobby arg2)
        {
            print($"Invite from {arg1.Name}");
        }

        private void LobbyMemberLeave(Lobby arg1, Friend arg2)
        {
            print($"{arg2.Name} has left the lobby");
        }

        private void LobbyMemberJoined(Lobby arg1, Friend arg2)
        {
            print($"{arg2.Name} joined!");
        }

        private void LobbyEntered(Lobby obj)
        {
            if (NetworkManager.Singleton.IsHost)
            {
                return;
            }
            StartClient(currentLobby.Value.Owner.Id);
        }

        private void LobbyCreated(Result arg1, Lobby _lobby)
        {
            if(arg1 != Result.OK)
            {
                print("lobby was NOT created!");
                return;
            }

            _lobby.SetPublic();
            _lobby.SetJoinable(true);
            _lobby.SetGameServer(_lobby.Owner.Id);
        }

        public async void StartHost()
        {
            NetworkManager.Singleton.OnServerStarted += NetworkManager_ServerStarted;
            try
            {
                NetworkManager.Singleton.StartHost();
                currentLobby = await SteamMatchmaking.CreateLobbyAsync((int)maxMembers);
            }
            catch (System.Exception)
            {
                if (NetworkManager.Singleton.IsHost)
                    NetworkManager.Singleton.Shutdown();
                throw;
            }
            PostConnection();

        }
        public void StartClient(SteamId sID)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_ClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_ClientDisconnected;
            transport.targetSteamId = sID;

            if (NetworkManager.Singleton.StartClient())
            {
                print("Client has started!");
                PostConnection();
            }
        }
        private void PostConnection()
        {
            disconnectButton.SetActive(true);
            hostGameButton.SetActive(false);
            gameJoinedDisplay.SetActive(true);
        }
        private void NetworkManager_ClientDisconnected(ulong obj)
        {

        }

        private void NetworkManager_ClientConnected(ulong obj)
        {

        }

        public void Disconnected()
        {
            currentLobby?.Leave();
            if(NetworkManager.Singleton == null)
            {
                return;
            }
            if (NetworkManager.Singleton.IsHost)
            {
                NetworkManager.Singleton.OnServerStarted -= NetworkManager_ServerStarted;
            }
            else
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= NetworkManager_ClientConnected;
            }
            NetworkManager.Singleton.Shutdown(true);
            disconnectButton.SetActive(false);
            hostGameButton.SetActive(true);
            gameJoinedDisplay.SetActive(false);

            print("Disconnected from game");
        }

        private void NetworkManager_ServerStarted()
        {
            print("host started!");
        }
    }
}