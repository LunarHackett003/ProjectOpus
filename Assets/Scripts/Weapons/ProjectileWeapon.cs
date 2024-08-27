using Unity.Netcode;
using UnityEngine;

public class ProjectileWeapon : BaseWeapon
{

    [SerializeField] internal bool projectileUseGravity;
    [SerializeField] internal float launchVelocity;
    [SerializeField] internal float maxProjectileLifetime;


    [SerializeField] Transform projectileOrigin;
    [SerializeField] NetworkObject projectilePrefab;

    [SerializeField] internal Renderer projectileRenderer;
    protected override void FireWeaponOnServer(NetworkObject ownerObject)
    {
        
        Vector3 start;
        Vector3 spread = transform.forward;
        base.FireWeaponOnServer(ownerObject);
        if (wm)
        {
            start = projectileOrigin.position;
            spread = wm.fireDirectionReference.TransformDirection(SpreadVector(minBaseSpread, maxBaseSpread, launchVelocity) + ((1 - wm.aimAmount) * wm.accumulatedSpread * SpreadVector(minHipSpread, maxHipSpread, 0)));
        }
        else
        {
            start = transform.position;
        }

        if(NetworkManager.SpawnManager.InstantiateAndSpawn(projectilePrefab, OwnerClientId, position: start).TryGetComponent(out Projectile p))
        {
            p.InitialiseProjectile(spread * launchVelocity, start);
        }
        FireWeapon_RPC(spread + start);
    }
    public override void FireWeapon(Vector3 end)
    {
        base.FireWeapon(end);
        if (projectileRenderer != null)
            projectileRenderer.enabled = false;
    }
    
}
