using UnityEngine;
[CreateAssetMenu(menuName = "Event Sequencer/Projectile Weapon Show Projectile")]
public class ProjectileShowEvent : SequenceEventData
{
    public bool show = true;
    public override void Trigger()
    {
        if (gameObject.TryGetComponent(out ProjectileWeapon p))
            p.projectileRenderer.enabled = show;
    }
}
