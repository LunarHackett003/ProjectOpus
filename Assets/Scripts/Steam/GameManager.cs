using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Steamworks;
using FishySteamworks;
using TMPro;
using HeathenEngineering.SteamworksIntegration;
using Eflatun.SceneReference;
using FishNet.Managing;
public class GameManager : MonoBehaviour
{
    protected static GameManager instance;
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                return new GameObject("Game Manager").AddComponent<GameManager>();
            }
            else
                return instance;
        }
    }

    #region Manager Prefabs
    public GameObject GameManagerPrefab;
    public GameObject ChatManagerPrefab;
    public GameObject MessageProxyPrefab;
    /// <summary>
    /// Reference to the singleton instance of the Chat Manager. This is also accessible via ChatManager.Instance, but can also be found here.
    /// </summary>
    public static ChatManager ChatManager;
    /// <summary>
    /// Reference to the singleton instance of the Message Proxy. This is also accessible via MessageProxy.Instance, but can also be found here.<br></br>
    /// The Message Proxy will ONLY ever exist while in a game. Text chat will be unavailable if not connected to a server.
    /// </summary>
    public static MessageProxy MessageProxy;
    #endregion
    #region Scene Management
    public SceneReference menuScene, gameScene;

    #endregion
    #region Steamworks
    string hostHex;
    #endregion

    #region Fishnet
    public bool ConnectedToServer { get; private set; }

    public FishySteamworks.FishySteamworks fishySteamworks;

    public void Disconnect()
    {
        if(FishNet.InstanceFinder.NetworkManager.IsClientStarted)
            fishySteamworks.StopConnection(false);
        if(FishNet.InstanceFinder.NetworkManager.IsServerStarted)
            fishySteamworks.StopConnection(true);
        ConnectedToServer = false;
        ChatManager.gameObject.SetActive(false);
    }

    public void StartHost()
    {
        if (fishySteamworks)
        {
            var user = UserData.Get();
            hostHex = user.ToString();

            print(hostHex);
            if (user.IsValid)
            {
                //Starts a connection as both a server AND client
                fishySteamworks.StartConnection(true);
                fishySteamworks.StartConnection(false);
                ConnectionComplete();
            }
        }
    }

    public void StartClient(string hostHex)
    {
        if (fishySteamworks)
        {
            var hostUser = UserData.Get(hostHex);
            if (!hostUser.IsValid)
            {
                Debug.LogWarning("User was invalid! Check the host hex.");
                return;
            }
            fishySteamworks.SetClientAddress(hostUser.id.ToString());
            fishySteamworks.StartConnection(false);
            ConnectionComplete();
        }
    }

    void ConnectionComplete()
    {
        ConnectedToServer = true;
        if (ChatManager)
        {
            ChatManager.gameObject.SetActive(true);
        }
    }



    #endregion




    #region Unity Callbacks

    private void Awake()
    {
        if(instance != null)
        {
            Debug.Log("A Game Manager already exists. Destroying this one...");
            Destroy(gameObject);
            return;
        }
        fishySteamworks = FindAnyObjectByType<FishySteamworks.FishySteamworks>();

        if(fishySteamworks == null)
        {
            Debug.LogError("No FishySteamworks was found! Disabling the Game Manager...");
            enabled = false;
            return;
        }

        print("");
        instance = this;
        DontDestroyOnLoad(gameObject);


        fishySteamworks.OnClientConnectionState += ClientConnected;
    }

    private void ClientConnected(FishNet.Transporting.ClientConnectionStateArgs obj)
    {
        if(obj.ConnectionState == FishNet.Transporting.LocalConnectionState.Started)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(gameScene.BuildIndex);
        }
        else if(obj.ConnectionState == FishNet.Transporting.LocalConnectionState.Stopped)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(menuScene.BuildIndex);
        }
    }

    private void Start()
    {
        
    }
    #endregion

    public string HostHex => hostHex;
}
