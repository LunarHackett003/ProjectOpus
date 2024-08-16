using Unity.Netcode;
using UnityEngine;

public abstract class Damageable : NetworkBehaviour
{
    public abstract void TakeDamage(float damageAmount);
}
