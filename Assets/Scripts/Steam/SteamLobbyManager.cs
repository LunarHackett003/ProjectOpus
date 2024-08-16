using Eflatun.SceneReference;
using Netcode.Transports.Facepunch;
using opus.Gameplay;
using Steamworks;
using Steamworks.Data;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        public Lobby? CurrentLobby { get; private set; }
        public bool InLobby => CurrentLobby != null;
        public ulong hostID;
        public uint maxMembers;
        public GameObject[] buttonsInMenu, buttonsInGame;
        public GameObject gameJoinedDisplay;
        public bool GetNamesOfTeamMembersAutomatically = true;
        public GameObject lobbySettingsButton;
        public GameObject lobbySettingsPanel;
        public SceneReference lobbyScene;
        public SceneReference menuScene;
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


            NetworkManager.Singleton.OnServerStarted += NetworkManager_OnServerStarted;
            NetworkManager.Singleton.OnServerStopped += NetworkManager_OnServerStopped;

            SceneManager.sceneLoaded += SceneManager_sceneLoaded;

            SetButtonsActive();
            lobbySettingsPanel.SetActive(false);
        }

        private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            Camera.main.cullingMask = PlayerManager.Instance.cameraMask;
        }

        void SetButtonsActive()
        {
            for (int i = 0; i < buttonsInGame.Length; i++)
            {
                buttonsInGame[i].SetActive(InLobby);
            }
            for (int i = 0; i < buttonsInMenu.Length; i++)
            {
                buttonsInMenu[i].SetActive(!InLobby);
            }
            gameJoinedDisplay.SetActive(InLobby);
        }
        private void NetworkManager_OnServerStopped(bool obj)
        {
            print("server stopped");
            if(CurrentLobby != null)
                Disconnected();
        }

        private void NetworkManager_OnServerStarted()
        {
            print("server started");
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
                CurrentLobby = arg1;
            }
        }
        public bool IsHost {  get; private set; }
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
            StartClient(CurrentLobby.Value.Owner.Id);
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
                CurrentLobby = await SteamMatchmaking.CreateLobbyAsync((int)maxMembers);

            }
            catch (System.Exception)
            {
                if (NetworkManager.Singleton.IsHost)
                    NetworkManager.Singleton.Shutdown();
                throw;
            }


            NetworkManager.Singleton.SceneManager.LoadScene(lobbyScene.Name, LoadSceneMode.Single);
            
            PostConnection();
            IsHost = true;
            lobbySettingsButton.SetActive(true);
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
                lobbySettingsButton.SetActive(false);
                IsHost = false;
            }
        }
        private void PostConnection()
        {
            SetButtonsActive();
            PlayerManager.Instance.SetPause(false);
        }
        private void NetworkManager_ClientDisconnected(ulong obj)
        {
            
        }

        private void NetworkManager_ClientConnected(ulong obj)
        {

        }

        public void Disconnected()
        {
            CurrentLobby?.Leave();
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
            print("Disconnected from game");
            SceneManager.LoadScene(menuScene.Name, LoadSceneMode.Single);
            CurrentLobby = null;
            SetButtonsActive();
            lobbySettingsPanel.SetActive(false);
        }

        private void NetworkManager_ServerStarted()
        {
            print("host started!");
        }
    }
}