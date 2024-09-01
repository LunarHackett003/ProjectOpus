using Eflatun.SceneReference;
using UnityEngine;

namespace Opus
{
    public class DelayedSceneLoad : MonoBehaviour
    {
        public SceneReference scene;
        private void Start()
        {
            Invoke(nameof(Load), 3f);
        }
        void Load()
        {
            SceneLoader.Instance.LoadScene(scene);
        }
    }
}
