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

    #region Scene Management
    public SceneReference menuScene, gameScene;

    #endregion
    #region Steamworks
    string hostHex;
    #endregion

    #region Fishnet
    public FishySteamworks.FishySteamworks fishySteamworks;

    public void Disconnect()
    {
        if(FishNet.InstanceFinder.NetworkManager.IsClientStarted)
            fishySteamworks.StopConnection(false);
        if(FishNet.InstanceFinder.NetworkManager.IsServerStarted)
            fishySteamworks.StopConnection(true);
    }

    public void StartHost()
    {
        if (fishySteamworks)
        {
            var user = UserData.Get();
            hostHex = user.ToString();

            print(hostHex);

            //Starts a connection as both a server AND client
            fishySteamworks.StartConnection(true);
            fishySteamworks.StartConnection(false);
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
    }

    private void Start()
    {
        
    }
    #endregion


}
