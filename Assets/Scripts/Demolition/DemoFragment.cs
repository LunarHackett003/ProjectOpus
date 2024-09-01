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
        protected override void IntegrityChanged(float prev, float next, bool asServer)
        {
            base.IntegrityChanged(prev, next, asServer);
            if(next <= 0)
                render.SetActive(false);
        }
    }
}
