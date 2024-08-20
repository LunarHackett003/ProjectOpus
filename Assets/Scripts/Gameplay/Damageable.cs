using Unity.Netcode;
using UnityEngine;
public interface IDamageable
{
    public NetworkObject NetObject { get; }
    public Transform ThisTransform { get; }
    public void TakeDamage(float damageAmount);
}
