using Eflatun.SceneReference;
using UnityEngine;

namespace Opus
{
    public class DelayedSceneLoad : MonoBehaviour
    {
        public SceneReference sceneToLoad;
        public bool onStart;
        private void Start()
        {
            if (onStart)
            {
                TriggerSceneLoad();
            }
        }

        public void TriggerSceneLoad()
        {
            if (sceneToLoad != null && LoadScreenManager.Instance != null)
            {
                LoadScreenManager.Instance.LoadWithScreen(sceneToLoad);
            }
        }
    }
}
