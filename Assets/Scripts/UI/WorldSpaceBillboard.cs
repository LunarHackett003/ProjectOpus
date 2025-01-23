using UnityEngine;

#if UNITY_EDITOR

using UnityEditor;

#endif
namespace Opus
{
    public class WorldSpaceBillboard : MonoBehaviour
    {
        public bool invertDirection;

        public bool debug;

        private void Update()
        {
            transform.forward = (transform.position - Camera.main.transform.position) * (invertDirection ? -1 : 1);
        }

#if UNITY_EDITOR

        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying)
            {
                if (debug)
                    transform.forward = (transform.position - Camera.current.transform.position) * (invertDirection ? -1 : 1);
                else
                    transform.forward = Vector3.forward;
            }
        }

#endif
    }
}
