using opus.Weapons;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Scope : NetworkBehaviour
{
    public Camera scopeCam;
    public BaseWeapon weapon;
    public Renderer scopeRenderer;
    public GameObject scopeGlint;
    [Range(1, 120)]
    public int scopeFPS;
    public CustomRenderTexture crt;
    [Range(8, 11), Tooltip("Creates a scope texture of 2 ^ this value")]
    public int ScopeResolutionLevel;
    float timeBetweenRenders;
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        int size = (int)Mathf.Pow(2, ScopeResolutionLevel);
        print(size);
        crt = new CustomRenderTexture(size, size)
        {
            format = RenderTextureFormat.DefaultHDR
        };
        crt.Create();
        scopeCam.targetTexture = crt;
        scopeCam.enabled = false;
        scopeRenderer.material.SetTexture("_MainTex", crt);
        if (IsOwner)
        {
            StartCoroutine(UpdateScope());
            if(weapon.ownerWeaponManager.Value.TryGet(out WeaponManager wm))
            {
                weapon.wm = wm;
            }
        }
    }
    IEnumerator UpdateScope()
    {
        while (true)
        {
            timeBetweenRenders = 1 / scopeFPS;
            yield return new WaitForSeconds(timeBetweenRenders);
            if(weapon.wm && weapon.wm.aimAmount > 0.2f)
            {
                scopeCam.Render();
                crt.Update();
            }
        }
    }
    public NetworkVariable<bool> scopeActive = new NetworkVariable<bool>(writePerm: NetworkVariableWritePermission.Owner);
    private void Update()
    {
        if (IsOwner && weapon.wm)
        {
            scopeActive.Value = weapon.wm.aimAmount > 0.2f;
        }
        scopeGlint.SetActive(scopeActive.Value);
        scopeRenderer.enabled = scopeActive.Value;
    }

}
