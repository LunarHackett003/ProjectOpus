using TMPro;
using UnityEngine;

public class ConnectionMenu : MonoBehaviour
{

    public bool isMenu;
    public TMP_InputField inputField;
    public TMP_Text hostCodeDisplay;

    public void TryJoinGame()
    {
        if(inputField && !string.IsNullOrWhiteSpace(inputField.text) && GameManager.Instance)
        {
            GameManager.Instance.StartClient(inputField.text);
        }
        else
        {
            Debug.LogWarning("Input field is empty or not assigned!");
        }
    }
    public void TryHostGame()
    {
        if (GameManager.Instance)
        {
            GameManager.Instance.StartHost();
        }
    }

    public void Disconnect()
    {
        if (GameManager.Instance)
        {
            GameManager.Instance.Disconnect();
        }
    }
}
