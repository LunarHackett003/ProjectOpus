using UnityEngine;

public class Hitbox : Damageable
{

    [SerializeField] Damageable owner;
    [SerializeField] internal bool isHead;
    public override void TakeDamage(float damageAmount)
    {
        owner.TakeDamage(damageAmount);
    }

    private void OnValidate()
    {
        if (!owner)
        {
            owner = GetComponentInParent<Damageable>();
        }
    }
}
