using Eflatun.SceneReference;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Opus
{
    public class SessionManager : MonoBehaviour
    {
        public static SessionManager Instance {  get; private set; }

        public SceneReference[] scenes;
        public SceneReference menuScene;

        public Canvas sessionUI;
        private void Awake()
        {
            if(Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(Instance);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }
        private void Start()
        {
            
        }
        public void StartClient()
        {
            if (NetworkManager.Singleton.StartClient())
            {
                NetworkManager.Singleton.OnConnectionEvent += Singleton_OnConnectionEvent;
                sessionUI.gameObject.SetActive(true);
            }
        }

        private void Singleton_OnConnectionEvent(NetworkManager arg1, ConnectionEventData arg2)
        {
            switch (arg2.EventType)
            {
                case ConnectionEvent.ClientConnected:
                    break;
                case ConnectionEvent.PeerConnected:
                    break;
                case ConnectionEvent.ClientDisconnected:
                    if(arg2.ClientId == NetworkManager.Singleton.LocalClientId)
                    {
                        sessionUI.gameObject.SetActive(false);
                    }
                    break;
                case ConnectionEvent.PeerDisconnected:
                    break;
                default:
                    break;
            }
        }

        public void StartServer()
        {
            if(NetworkManager.Singleton.StartServer())
                ServerPostConnection();
        }
        public void StartHost()
        {
            if (NetworkManager.Singleton.StartHost())
            {
                sessionUI.gameObject.SetActive(true);
                ServerPostConnection();
            }
        }
        public void ServerPostConnection()
        {
            if(scenes.Length > 0)
            {
                int random = Random.Range(0, scenes.Length);
                NetworkManager.Singleton.SceneManager.LoadScene(scenes[random].Name, UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
        }
        public void CloseConnection()
        {
            if(NetworkManager.Singleton != null)
                NetworkManager.Singleton.Shutdown();

            if(LoadScreenManager.Instance != null)
                LoadScreenManager.Instance.LoadWithScreen(menuScene);
        }

        public void QuitGame()
        {
            StartCoroutine(QuitGameRoutine());
            sessionUI.gameObject.SetActive(false);
        }
        IEnumerator QuitGameRoutine()
        {
            yield return null;
            CloseConnection();
            yield break;
        }
    }
}
