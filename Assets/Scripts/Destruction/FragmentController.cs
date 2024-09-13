using System;
using Unity.Netcode;
using UnityEngine;

namespace Opus
{
    public class FragmentController : NetworkBehaviour
    {
        public NetworkVariable<bool> hasBeenHit = new();
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
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (hasBeenHit.Value)
            {
                undamagedSurface.SetActive(false);
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
                if (fragments[i].health.Value > 0)
                    fragments[i].renderer.enabled = true;
            }
        }
    }
}
