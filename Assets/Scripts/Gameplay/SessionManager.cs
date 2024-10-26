using Eflatun.SceneReference;
using Unity.Netcode;
using UnityEngine;

namespace Opus
{
    public class SessionManager : MonoBehaviour
    {
        public static SessionManager Instance {  get; private set; }

        public SceneReference[] scenes;
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
            NetworkManager.Singleton.StartClient();
        }
        public void StartServer()
        {
            NetworkManager.Singleton.StartServer();
            ServerPostConnection();
        }
        public void StartHost()
        {
            NetworkManager.Singleton.StartHost();
            ServerPostConnection();
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
            {
                NetworkManager.Singleton.Shutdown();
            }
        }
    }
}
