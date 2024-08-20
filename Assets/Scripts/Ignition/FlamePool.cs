using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;

/// <summary>
/// Bears no similarity to an Object Pool - this refers to an object that is both a Flammable (to make use of Extinguishing) and an IgnitionSource.
/// </summary>
public class FlamePool : IgnitionSource, IFlammable
{
    [SerializeField] float flamePoolDuration;

    [SerializeField] VisualEffect flameEffect;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            Ignite(flamePoolDuration);
        }
        flameEffect.Play();
    }
    public IEnumerator Burn(float duration)
    {
        yield return new WaitForSeconds(duration);
        Extinguish();
    }
    protected override void FixedUpdate()
    {
        print("Flame Pool Fixed Update");
        base.FixedUpdate();
    }
    public void EndBurn()
    {
        NetworkObject.Despawn();
    }

    public void Extinguish()
    {
        Invoke(nameof(EndBurn), 5);
    }

    public void Ignite(float duration)
    {
        StartCoroutine(Burn(duration));
        activated = true;
    }

    public void TryIgnite(float duration, float igniteContribute)
    {

    }
}
