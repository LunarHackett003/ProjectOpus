using Eflatun.SceneReference;
using UnityEngine;

namespace Opus
{
    public class DelayedSceneLoad : MonoBehaviour
    {
        public float time = 0.1f;
        public SceneReference scene;
        private void Start()
        {
            Invoke(nameof(Load), time);
        }
        void Load()
        {
            SceneLoader.Instance.LoadScene(scene);
        }
    }
}
