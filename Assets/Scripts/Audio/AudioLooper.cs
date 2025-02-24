using UnityEngine;

namespace Opus
{
    public class AudioLooper : MonoBehaviour
    {
        public AK.Wwise.Event audio;

        void Start()
        {
            audio.Post(gameObject);
        }

        private void OnDestroy()
        {
            audio.Stop(gameObject);
        }
    }
}
