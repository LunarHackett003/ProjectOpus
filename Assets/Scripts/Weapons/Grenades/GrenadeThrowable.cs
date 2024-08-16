using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Allows the player to throw a grenade.
/// </summary>
public class GrenadeThrowable : BaseEquipment
{
    public GameObject grenadePrefab;
    //override OnSelect just in case we need it.
    public override bool CanSelect()
    {
        return base.CanSelect() && currentStoredUses.Value > 0;
    }
    public override void OnSelected()
    {
        base.OnSelected();
    }
    bool primaryPressed;
    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        if (primaryInput.Value)
        {
            //We need to prime the grenade, essentially. Grenades are thrown by releasing the button.

        }
        else
        {
            if(IsServer && primaryPressed)
            {
                //We have just released the primary input, so we need to throw the grenade
                ThrowGrenade();
            }
            if (IsOwner)
            {
                CheckStillUsable();
            }
        }
        primaryPressed = primaryInput.Value;
    }
    protected void ThrowGrenade()
    {
        var n = NetworkManager.SpawnManager.InstantiateAndSpawn(grenadePrefab.GetComponent<NetworkObject>(), OwnerClientId, position: transform.position, rotation: transform.rotation);
        if (n.TryGetComponent(out Grenade g))
        {
            g.rb.AddForce(g.transform.forward * g.launchForce, ForceMode.Impulse);
            g.SetFuse();
        }
        currentStoredUses.Value--;
        localCooldown = cooldownDuration;
    }
}
