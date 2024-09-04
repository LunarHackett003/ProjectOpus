using Eflatun.SceneReference;
using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace Opus
{
    public class SceneLoader : MonoBehaviour
    {
        protected static SceneLoader _instance;
        public static SceneLoader Instance => _instance;

        public CanvasGroup loadScreenCanvas;
        public UnityEngine.UI.Image loadScreenImage;
        public TMP_Text nextSceneNameDisplay;


        public SceneReference gameScene, menuScene;

        private void Awake()
        {
            if(_instance == null)
            {
                _instance = this;
            }
            else
            {
                Destroy(this);
                return;
            }

        }
        //fix compilation
        private void Start()
        {
            //NetworkManager.Singleton.SceneManager.OnLoad += SceneManager_OnLoad;
            //NetworkManager.Singleton.SceneManager.OnLoadComplete += SceneManager_OnLoadComplete;
        }
        private void SceneManager_OnLoadComplete(ulong clientId, string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode)
        {
            loadScreenCanvas.alpha = 0;
            loadScreenCanvas.gameObject.SetActive(false);
        }

        private void SceneManager_OnLoad(ulong clientId, string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode, AsyncOperation asyncOperation)
        {
            loadScreenCanvas.alpha = 1;
            loadScreenCanvas.gameObject.SetActive(true);
        }

        private void OnDestroy()
        {
            //NetworkManager.Singleton.SceneManager.OnLoad -= SceneManager_OnLoad;
            //NetworkManager.Singleton.SceneManager.OnLoadComplete -= SceneManager_OnLoadComplete;
        }


        public void LoadGameScene()
        {
            LoadScene(gameScene);
        }
        public void LoadMenuScene()
        {
            LoadScene(menuScene);
        }
        public void LoadScene(SceneReference scene)
        {
            StartCoroutine(LoadSceneRoutine(scene.BuildIndex));
        }
        IEnumerator LoadSceneRoutine(int buildIndex)
        {
            loadScreenCanvas.alpha = 1;
            loadScreenCanvas.gameObject.SetActive(true);
            AsyncOperation load = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(buildIndex);
            while (!load.isDone)
            {
                yield return null;
            }
            load.allowSceneActivation = true;
            loadScreenCanvas.alpha = 0;
            loadScreenCanvas.gameObject.SetActive(false);
        }
    }
}
