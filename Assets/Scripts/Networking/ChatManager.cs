using HeathenEngineering.SteamworksIntegration;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChatManager : MonoBehaviour
{
    public int maxMessagesStored = 12;
    public float messageWindowFadeStartTime, messageWindowFadeSpeed;
    public CanvasGroup messageWindow;
    protected static ChatManager instance;
    public static ChatManager Instance 
    {
        get
        {
            if (instance == null)
                return instance = Instantiate(GameManager.Instance.ChatManagerPrefab).GetComponent<ChatManager>();
            return instance;
        } 
    }
    bool fading;
    public TMP_Text messageDisplay;
    public TMP_InputField messageField;
    private void Awake()
    {
        if(instance != null)
        {
            Destroy(gameObject);
            return;
        }
        chatMessages = new List<ChatMessage>();
        instance = this;
        GameManager.ChatManager = this;

        DontDestroyOnLoad(gameObject);

        if (!GameManager.Instance.ConnectedToServer)
            gameObject.SetActive(false);

        print(UserData.Get().HexId);
    }

    public List<ChatMessage> chatMessages;
    [System.Serializable]
    public struct ChatMessage
    {
        public string senderID, senderName, message;
        public bool isJoinMessage;
    }
    public void ReceiveChatMessage(string accountID, string message, bool isJoinMessage)
    {

        chatMessages.Insert(0, new ChatMessage
        {
            senderID = accountID,
            senderName = UserData.Get().Name,
            message = message,
            isJoinMessage = isJoinMessage
        });
        UpdateMessages();
    }
    public void SendChatMessage(string message)
    {
        if (!string.IsNullOrWhiteSpace(message) && message.Length > 0 && MessageProxy.Instance)
        {
            MessageProxy.Instance.SendMessageProxy(UserData.Get().HexId, message);
        }
    }
    public void UpdateMessages()
    {
        messageWindow.gameObject.SetActive(true);
        messageWindow.alpha = 1;
        fading = false;
        StopCoroutine(FadeChatWindow());
        while (chatMessages.Count > maxMessagesStored - 1)
        {
            chatMessages.RemoveAt(chatMessages.Count - 1);
        }

        if (chatMessages.Count > 0)
        {
            string messageOutput = "";
            for (int i = 0; i < chatMessages.Count; i++)
            {
                ChatMessage message = chatMessages[i];
                messageOutput += $"[{message.senderName}]: " + message.message + "\n";
            }
            messageDisplay.text = messageOutput;
        }
        StartCoroutine(FadeChatWindow());


    }

    IEnumerator FadeChatWindow()
    {
        fading = true;
        float t = 0;
        while (t < messageWindowFadeStartTime && fading)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (fading)
        {
            t = 1;
            while (t > 0)
            {
                t -= Time.unscaledDeltaTime * messageWindowFadeSpeed;
                messageWindow.alpha = t;
            }
            messageWindow.alpha = 0;
            messageWindow.gameObject.SetActive(false);
        }
        else
        {
            yield return null;
        }
    }
}
