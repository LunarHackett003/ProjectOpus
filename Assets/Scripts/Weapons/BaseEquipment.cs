using Netcode.Extensions;
using opus.Gameplay;
using opus.utility;
using System;
using Unity.Netcode;
using UnityEngine;
public class BaseEquipment : BaseGear
{
    public NetworkVariable<bool> primaryInput = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> secondaryInput = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    [SerializeField] internal bool isWeapon;
    [SerializeField] internal AnimationSetScriptableObject playerAnimations;
    [SerializeField] internal AnimationSetScriptableObject weaponAnimations;
    [SerializeField] internal Animator animator;
    [SerializeField] internal ClientNetworkAnimator networkAnimator;
    /// <summary>
    /// Used for hiding equipment when the player dies
    /// </summary>
    [SerializeField] Renderer[] renderers;
    [SerializeField] AnimatorOverrideController overrideController;
    AnimationClipOverrides clipOverrides;
    public NetworkVariable<bool> currentGear = new(writePerm: NetworkVariableWritePermission.Owner);
    [SerializeField] internal Vector3 blockedPosition, blockedRotation, blockingCheckSize, blockingCheckOffset;
    protected override void Start()
    {
        base.Start();
        if (animator)
        {
            UpdateAnimations();
        }
    }
    protected virtual void UpdateAnimations()
    {
        if (overrideController == null)
        {
            overrideController = new(animator.runtimeAnimatorController);
            animator.runtimeAnimatorController = overrideController;

            clipOverrides = new(overrideController.overridesCount);
            overrideController.GetOverrides(clipOverrides);
        }
        for (int i = 0; i < weaponAnimations.overrides.Length; i++)
        {
            OverridePair pair = weaponAnimations.overrides[i];
            clipOverrides[pair.name] = pair.clip;
        }
        overrideController.ApplyOverrides(clipOverrides);

        
    }
    protected virtual void CheckStillUsable()
    {
        if(currentStoredUses.Value <= 0 && currentGear.Value)
        {
            PlayerManager.Instance.pc.wm.SwitchWeaponDirectly(0);
        }
    }
    [Rpc(SendTo.Everyone)]
    public void DisplayWeapon_RPC(bool show)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = show;
        }
    }

    public void SetPrimaryInput(bool input)
    {
        if(input != primaryInput.Value)
            primaryInput.Value = input;
    }
    public void SetSecondaryInput(bool input)
    {
        if(input != secondaryInput.Value)
            secondaryInput.Value = input;
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(blockingCheckOffset, blockingCheckSize);
        Gizmos.DrawWireSphere(blockedPosition, 0.1f);
        Gizmos.DrawRay(blockedPosition, Quaternion.Euler(blockedRotation) * Vector3.forward);
    }
}
