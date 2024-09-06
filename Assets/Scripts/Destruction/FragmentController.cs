using System;
using UnityEngine;

namespace Opus
{
    public class FragmentController : MonoBehaviour
    {
        public Fragment[] fragments;
        public GameObject undamagedSurface;
        [ContextMenu("Initialise Fragmentation")]
        public void InitialiseFragmentation()
        {
            fragments = GetComponentsInChildren<Fragment>();
            for (int i = 0; i < fragments.Length; i++)
            {
                fragments[i].controller = this;
            }
        }
        public void FragmentDamaged()
        {
            if (undamagedSurface.activeInHierarchy)
            {
                undamagedSurface.SetActive(false);
            }
            for (int i = 0; i < fragments.Length; i++)
            {
                if (fragments[i].health > 0)
                    fragments[i].renderer.enabled = true;
            }
        }
    }
}
