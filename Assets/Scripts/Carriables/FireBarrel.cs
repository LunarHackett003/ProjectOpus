using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;

public class FireBarrel : Barrel, IFlammable
{
    float accumulatedBurn;
    public NetworkVariable<bool> burning = new(writePerm:NetworkVariableWritePermission.Server);
    public IgnitionSource ignitionSource;
    public VisualEffect burnEffect;
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        burning.OnValueChanged += BurningChanged;
    }
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        burning.OnValueChanged -= BurningChanged;
    }
    public IEnumerator Burn(float duration)
    {
        float t = 0;
        while (burning.Value && t < duration)
        {
            yield return new WaitForSeconds(GameplayManager.Instance.fireDamagePerTick);
            TakeDamage(GameplayManager.Instance.fireDamagePerTick);
        }
    }
    void BurningChanged(bool previous, bool current)
    {
        if (current)
            burnEffect.Play();
        else
            burnEffect.Stop();

        if (IsServer)
        {
            ignitionSource.activated = current;
        }
    }
    public void EndBurn()
    {

    }

    public void Extinguish()
    {
        //Fire Barrels cannot be extinguished.
    }

    public void Ignite(float duration)
    {
        burning.Value = true;
        StartCoroutine(Burn(duration));
    }
    
    public void TryIgnite(float duration, float igniteContribute)
    {
        accumulatedBurn += igniteContribute;
        if(accumulatedBurn > 0 && !burning.Value)
        {
            Ignite(duration);
        }
    }
}
