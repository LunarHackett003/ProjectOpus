using Eflatun.SceneReference;
using FishNet;
using System.Collections;
using TMPro;
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
            InstanceFinder.SceneManager.OnLoadStart += SceneManager_OnLoadStart;
            InstanceFinder.SceneManager.OnLoadEnd += SceneManager_OnLoadEnd;
        }

        private void SceneManager_OnLoadEnd(FishNet.Managing.Scened.SceneLoadEndEventArgs obj)
        {
            loadScreenCanvas.alpha = 0;
            loadScreenCanvas.gameObject.SetActive(false);

        }

        private void OnDestroy()
        {
            InstanceFinder.SceneManager.OnLoadStart -= SceneManager_OnLoadStart;
            InstanceFinder.SceneManager.OnLoadEnd -= SceneManager_OnLoadEnd;
        }
        private void SceneManager_OnLoadStart(FishNet.Managing.Scened.SceneLoadStartEventArgs obj)
        {
            loadScreenCanvas.alpha = 1;
            loadScreenCanvas.gameObject.SetActive(true);

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
