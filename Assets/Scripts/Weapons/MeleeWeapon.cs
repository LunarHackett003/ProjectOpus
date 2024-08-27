using Unity.Netcode;
using UnityEngine;

public class MeleeWeapon : BaseWeapon
{
    [SerializeField] float meleeRadius;
    [SerializeField] Vector3 meleeOffset;
    public void MeleeAttack()
    {
        if (IsServer)
        {
            if (CheckWeaponManager())
            {
                Debug.Log("Melee Attack!");
                FireWeaponOnServer(wm.NetworkObject);
            }
            else
            {
                Debug.Log("No weapon manager!");
            }
        }
    }
    protected override void FixedUpdate()
    {
        //Do nothing in fixed update.
    }
    protected override void FireWeaponOnServer(NetworkObject ownerObject)
    {
        base.FireWeaponOnServer(ownerObject);

        var colliders = new Collider[3];
        if (Physics.OverlapSphereNonAlloc(wm.fireDirectionReference.TransformPoint(meleeOffset), meleeRadius, colliders ,GameplayManager.Instance.bulletLayermask) > 0)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] == null)
                    continue;
                if (colliders[i].transform.TryGetComponent(out IDamageable d))
                {
                    //Hit something hurtable
                    if(d.NetObject != ownerObject)
                    {
                        d.TakeDamage(maxDamage);
                        print("melee hit");
                        wm.HitFeedback_RPC(false);
                    }
                }
                else
                {
                    //Did not hit something hurtable
                }
            }
        }
    }
}
