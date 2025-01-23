using UnityEngine;

namespace Opus
{
    public class RangedAmmoVisual : MonoBehaviour
    {
        public RangedWeapon rw;
        public Renderer[] ammoRenderers;
        private void Start()
        {
            if(rw.maxAmmo > 0)
            {
                rw.syncedAmmo.OnValueChanged += AmmoChanged;
            }
        }
        void AmmoChanged(int prev, int current)
        {
            for (int i = 0; i < ammoRenderers.Length; i++)
            {
                if (ammoRenderers[i] != null)
                {
                    ammoRenderers[i].enabled = current > i;
                }
            }
        }
    }
}
