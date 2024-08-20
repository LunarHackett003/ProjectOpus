using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class Barrel : Carriable, IDamageable
{
    public NetworkObject NetObject => NetworkObject;

    public Transform ThisTransform => transform;

    public NetworkVariable<float> health = new(writePerm:NetworkVariableWritePermission.Server);
    [SerializeField]
    protected float lowHealthThreshold, maxHealth;
    public UnityEvent lowHealthEvent;
    public UnityEvent destructionEvent;
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            health.Value = maxHealth;
        }
        health.OnValueChanged += HealthChanged;
    }
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        health.OnValueChanged -= HealthChanged;
    }
    void HealthChanged(float previous, float current)
    {
        if(current < previous && IsServer)
        {
            //The barrel has taken damage
            if(current > 0 && current < lowHealthThreshold)
            {
                //Its still alive, but is now below the LowHealthThreshold
                lowHealthEvent?.Invoke();
            }
            else if(current <= 0)
            {
                destructionEvent?.Invoke();
            }
        }
    }
    public void TakeDamage(float damageAmount)
    {
        if(health.Value > 0)
        {
            health.Value -= damageAmount;
        }
    }
    protected override void OnCollisionEnter(Collision collision)
    {
        base.OnCollisionEnter(collision);
        if (IsServer)
        {
            if(collision.impulse.sqrMagnitude > hardCollisionThreshold)
            {
                if (collision.collider.TryGetComponent(out IDamageable d))
                {
                    d.TakeDamage(hardCollisionOtherDamage);
                    TakeDamage(hardCollisionSelfDamage);
                }
            }
        }
    }

}
