using UnityEngine;

namespace Opus
{
    public class TargetDummy : Entity
    {

        protected override void AliveStateChanged(bool previous, bool current)
        {
            base.AliveStateChanged(previous, current);
        }
        protected override void HealthChanged(float previous, float current)
        {
            base.HealthChanged(previous, current);
        }
    }
}
