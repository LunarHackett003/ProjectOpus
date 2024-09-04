using UnityEngine;

namespace Opus.Demolition
{
    public class DemoFragment : DemoPiece
    {
        [SerializeField] GameObject render;
        public override void DealDamage(float damage)
        {
            base.DealDamage(damage);
        }
        protected override void IntegrityChanged(float prev, float next)
        {
            base.IntegrityChanged(prev, next);
            if(next <= 0)
                render.SetActive(false);
        }
    }
}
