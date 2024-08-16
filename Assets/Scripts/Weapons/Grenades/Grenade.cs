using opus.Gameplay;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class Grenade : Damageable
{
    [SerializeField] protected float fuseTime;
    [SerializeField] protected bool fuseOnStart;
    [SerializeField] internal float launchForce;
    [SerializeField] protected bool launchOnStart;
    internal Rigidbody rb;
    [SerializeField] protected UnityEvent explosionEvents;
    [SerializeField] protected GameObject explosionPrefab;
    [SerializeField] protected float explosionDestroyTime = 10;
    [SerializeField] protected bool explodeWhenShot;
    bool hit;
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            rb = GetComponent<Rigidbody>();
            if (launchOnStart)
                rb.AddForce(transform.forward * launchForce);
            if (fuseOnStart)
                SetFuse();
        }
    }
    [ClientRpc(DeferLocal = true)]
    protected void Explode_ClientRPC()
    {
        print("Exploded on client");
        GameObject g = Instantiate(explosionPrefab, transform.position, transform.rotation);
        Destroy(g, explosionDestroyTime);
        explosionEvents?.Invoke();
    }
    public void SetFuse()
    {
        if (IsOwner)
        {
            SetFuse_ServerRPC();
            print("Setting fuse!");
        }
    }
    [ServerRpc]
    protected void SetFuse_ServerRPC()
    {
        print("fuse set!");
        StartCoroutine(DelayExplosion());
    }
    IEnumerator DelayExplosion()
    {
        yield return new WaitForSeconds(fuseTime);
        ExplodeServerSide();
    }
    protected virtual void ExplodeServerSide()
    {
        if (IsServer)
        {
            print("Server-side Explosion!");
            Explode_ClientRPC();
            NetworkObject.Despawn(true);
        }
    }

    public override void TakeDamage(float damageAmount)
    {
        if (explodeWhenShot && !hit)
        {
            hit = true;
            ExplodeServerSide();
        }
    }
}
