using Unity.Netcode;
using UnityEngine;

public class BaseGear : NetworkBehaviour
{
    public NetworkVariable<bool> isReadyToUse = new NetworkVariable<bool>(true);
    public bool hasCooldown;
    public int storedUses;
    public NetworkVariable<int> currentStoredUses = new();
    public float cooldownDuration = 15;
    public NetworkVariable<float> syncedCooldown = new();
    public float localCooldown;
    public bool canCooldown;
    int framesSinceSync;

    protected virtual void Start()
    {
        
    }

    public virtual void OnSelected()
    {
        print("selected gear");
    }

    /// <summary>
    /// Called when this gear is selected
    /// </summary>
    public virtual bool CanSelect()
    {
        if(isReadyToUse.Value)
        {
            OnSelected();
            return true;
        }
        return false;
    }
    protected virtual void FixedUpdate()
    {
        if (currentStoredUses.Value < storedUses)
        {
            if (localCooldown > 0 && canCooldown)
            {
                localCooldown -= Time.fixedDeltaTime;
            }
            if (IsServer)
            {
                framesSinceSync++;
                framesSinceSync %= 10;
                if (localCooldown <= 0)
                {
                    localCooldown = cooldownDuration;
                    currentStoredUses.Value++;
                }
                if (framesSinceSync == 0 || localCooldown == cooldownDuration)
                {
                    syncedCooldown.Value = localCooldown;
                }
            }
        }
        else
        {
            localCooldown = 0;
        }
    }
    public void SetUsableByServer(bool usable)
    {
        if(IsServer)
        {
            isReadyToUse.Value = usable;
        }
    }
}
