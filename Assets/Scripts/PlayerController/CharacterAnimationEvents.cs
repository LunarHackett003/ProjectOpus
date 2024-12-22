using UnityEngine;

namespace Opus
{
    public class CharacterAnimationEvents : MonoBehaviour
    {
        public WeaponController wc;
        private void Start()
        {
            wc = GetComponentInParent<WeaponController>();
        }
        public void EndGesture()
        {
            if(wc != null)
            {
                wc.LerpGestureWeight(false);
            }
        }   
    }
}
