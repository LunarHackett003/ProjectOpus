using FishNet.Object;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using HeathenEngineering;
using HeathenEngineering.SteamworksIntegration;

public class MessageProxy : NetworkBehaviour
{
    public ParticleSystem joinParticle;
    protected static MessageProxy instance;
    public static MessageProxy Instance
    {
        get
        {
            if(instance == null)
                return instance = Instantiate(GameManager.Instance.MessageProxyPrefab).GetComponent<MessageProxy>();
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
        }

        GameManager.MessageProxy = this;
        if (!IsSpawned && IsServerInitialized)
        {
            Spawn(gameObject);
            print("Spawn object");
        }

        instance = this;
    }
    public override void OnStartClient()
    {
        base.OnStartClient();
        if (LocalConnection.IsLocalClient)
        {
            SendJoinMessage(UserData.Get().HexId);
        }
    }
    public void SendMessageProxy(string hex, string message)
    {
        SendChatMessage(message, hex);
    }
    [ObserversRpc()]
    public void SendJoinMessage(string hex)
    {
        ChatManager.Instance.ReceiveChatMessage(hex, "has joined the game!", true);
        joinParticle.Play();
    }
    [ObserversRpc()]
    public void SendChatMessage(string message, string senderHex)
    {
        ChatManager.Instance.ReceiveChatMessage(senderHex, message, false);
    }
    
}
