using opus.Gameplay;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class Grenade : NetworkBehaviour
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

    public NetworkObject NetObject => NetworkObject;

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
    public void Explode()
    {
        ExplodeServerSide();
        NetworkManager.SpawnManager.InstantiateAndSpawn(explosionPrefab.GetComponent<NetworkObject>(), position: transform.position, rotation: Quaternion.identity);

    }
    [ServerRpc(DeferLocal = true)]
    protected void SetFuse_ServerRPC()
    {
        print("fuse set!");
        StartCoroutine(DelayExplosion());
    }
    IEnumerator DelayExplosion()
    {
        yield return new WaitForSeconds(fuseTime);
        Explode();
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

    public void TakeDamage(float damageAmount)
    {
        if (explodeWhenShot && !hit)
        {
            hit = true;
            ExplodeServerSide();
        }
    }
}
