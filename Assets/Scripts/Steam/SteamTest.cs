using Steamworks;
using UnityEngine;

public class SteamTest : MonoBehaviour
{
    public string displayName;
    private CallResult<NumberOfCurrentPlayers_t> numberOfCurrentPlayers;

    private void Start()
    {
        if (SteamManager.Initialized)
        {
            //Self explanatory, i think. gets your name
            displayName = SteamFriends.GetPersonaName();
            print("Found player name: " + displayName);

            /*Check number of players
            *The SDK documentation for Steamworks.NET (https://steamworks.github.io/) mentions call results,
            *and explains them as asynchronous api results, similar to callbacks.
            *If a function returns a SteamAPICall_t then it must use a Call Result.
            *in Steamworks.NET, setting up Call Results seems to be very easy.
           */


            InvokeRepeating(nameof(CheckPlayers), 0.5f, 5);
            print("set up repeating invoke for CheckPlayers");
        }
    }

    void CheckPlayers()
    {
        SteamAPICall_t handle = SteamUserStats.GetNumberOfCurrentPlayers();
        numberOfCurrentPlayers.Set(handle);
    }

    private void OnNumberOfCurrentPlayers(NumberOfCurrentPlayers_t callback, bool IOFailure)
    {
        if (callback.m_bSuccess != 1 || IOFailure)
        {
            Debug.Log("There was an error retrieving the NumberOfCurrentPlayers.");
        }
        else
        {
            Debug.Log("The number of players playing your game: " + callback.m_cPlayers);
        }
    }

    private void OnEnable()
    {
        if(SteamManager.Initialized)
        {
            numberOfCurrentPlayers = CallResult<NumberOfCurrentPlayers_t>.Create(OnNumberOfCurrentPlayers);
        }
    }
    private void OnDisable()
    {
        if (SteamManager.Initialized)
        {
            numberOfCurrentPlayers.Dispose();
            print("Disposed of call results");
        }
    }
}
