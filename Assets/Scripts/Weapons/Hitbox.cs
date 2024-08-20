using Unity.Netcode;
using UnityEngine;

public class Hitbox : NetworkBehaviour, IDamageable
{

    [SerializeField] IDamageable owner;
    [SerializeField] internal bool isHead;
    public NetworkObject NetObject => NetworkObject;

    public Transform ThisTransform => transform;

    public void TakeDamage(float damageAmount)
    {
        owner.TakeDamage(damageAmount);
    }
    private void Awake()
    {
        owner ??= transform.parent.GetComponentInParent<IDamageable>();
    }

}
