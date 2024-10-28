using Eflatun.SceneReference;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Opus
{
    public class LoadScreenManager : MonoBehaviour
    {
        public CanvasGroup loadScreenCanvasGroup;
        public static LoadScreenManager Instance { get; private set; }
        public float localLoadDelayTime = 2;
        private void Awake()
        {
            if(Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            SetLoadScreenActive(false);
        }
        private void Start()
        {
            NetworkManager.Singleton.OnClientStarted += Singleton_OnClientStarted;
            NetworkManager.Singleton.OnClientStopped += Singleton_OnClientStopped;

            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        }

        private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            SetLoadScreenActive(false);
        }

        private void OnApplicationQuit()
        {
            if(NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientStarted -= Singleton_OnClientStarted;
                NetworkManager.Singleton.OnClientStopped -= Singleton_OnClientStopped;
            }
        }
        private void Singleton_OnClientStopped(bool obj)
        {
            SetLoadScreenActive(true);
            LoadWithScreen(SessionManager.Instance.menuScene);
        }

        private void Singleton_OnClientStarted()
        {
            SetLoadScreenActive(true);
            NetworkManager.Singleton.SceneManager.OnLoadComplete += SceneManager_OnLoadComplete;
        }

        private void SceneManager_OnLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
        {
            if(NetworkManager.Singleton.LocalClientId == clientId)
            {
                //Only handle the load screen if we are this client
                SetLoadScreenActive(false);
            }
        }

        public void SetLoadScreenActive(bool active)
        {
            if (loadScreenCanvasGroup != null)
            {
                loadScreenCanvasGroup.alpha = active ? 1 : 0;
                loadScreenCanvasGroup.blocksRaycasts = active;
                loadScreenCanvasGroup.interactable = active;
                loadScreenCanvasGroup.gameObject.SetActive(active);
            }
        }
        public void LoadWithScreen(SceneReference sceneRef)
        {
            StartCoroutine(AsyncLoadWithScreen(sceneRef));
        }
        IEnumerator AsyncLoadWithScreen(SceneReference sceneRef)
        {
            SetLoadScreenActive(true);
            yield return new WaitForSeconds(localLoadDelayTime);
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneRef.BuildIndex, LoadSceneMode.Single);
            while (op.progress <= 1)
            {
                yield return null;
            }
            yield return new WaitForSeconds(localLoadDelayTime);
            SetLoadScreenActive(false);
        }
    }
}
