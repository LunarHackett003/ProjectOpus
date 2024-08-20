using System.Collections;
using Unity.Netcode;
using UnityEngine;

public interface IFlammable
{
    public void TryIgnite(float duration, float igniteContribute);
    public void Extinguish();
    public void Ignite(float duration);
    public IEnumerator Burn(float duration);
    public void EndBurn();
}
