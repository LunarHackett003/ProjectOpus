using NUnit.Framework;
using opus.Gameplay;
using opus.Weapons;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem.XR.Haptics;
using UnityEngine.UI;

public class Deployable : BaseEquipment
{
    [SerializeField] NetworkObject deployablePrefab;
    [SerializeField] GameObject deployableHologram;

    [SerializeField] WeaponManager wm;

    [SerializeField] LayerMask placementLayermask, obstructionLayermask;
    [SerializeField] Vector3 placementObstructionBoxBounds, placementObstructionBoxOffset;
    [SerializeField] float maxPlacementDistance;
    [SerializeField, Tooltip("If true, this deployable can be placed on surfaces of any orientation")] bool canPlaceOnAllSides = true;
    [SerializeField] Vector3Bool allowedSides;
    [SerializeField, Tooltip("If CanPlaceOnAllSides is false, then this determines the allowed dot product between the surface you're placing it on, and the dot product of the allowed sides." +
        "\nI have no idea what changing this will do.")] float placementDisallowDotThreshold = 0.5f;
    [SerializeField] GameObject hologramInstance;
    [SerializeField] Renderer hologramRenderer;
    bool _current;
    NetworkList<NetworkObjectReference> currentDeployable = new();
    [SerializeField] internal int maxDeployables;
    Vector3 targetedPoint, normal;
    bool obstructed;
    [SerializeField] internal bool canPickupFromTablet;
    [SerializeField] internal float tabletPickupTime;
    float currentTabletPickupTime;
    [SerializeField] internal bool cooldownBlockedByPlacement;
    [SerializeField] Image tabletPickupImage;

    [SerializeField] int deployablecount;
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner)
        {
            wm = PlayerManager.Instance.pc.wm;
        }
        if (IsServer)
        {
            currentDeployable.OnListChanged += CurrentDeployable_OnListChanged;
        }
    }

    private void CurrentDeployable_OnListChanged(NetworkListEvent<NetworkObjectReference> changeEvent)
    {
        if(currentDeployable.Count > maxDeployables)
        {
            //Removes the OLDEST

            if(currentDeployable[0].TryGet(out var nob)){
                nob.Despawn();
            }
            currentDeployable.RemoveAt(0);
        }
        deployablecount = currentDeployable.Count;
    }
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (IsServer)
        {
            currentDeployable.OnListChanged -= CurrentDeployable_OnListChanged;
        }
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        if (IsServer) {
            if (currentDeployable.Count > 0) {
                for (int i = 0; i < currentDeployable.Count; i++)
                {
                    if (!currentDeployable[i].TryGet(out NetworkObject n))
                    {
                        currentDeployable.RemoveAt(i);
                    }
                }
            }
        }
        if (currentDeployable.Count > 0)
        {
            canCooldown = !(cooldownBlockedByPlacement && currentDeployable.Count >= maxDeployables);
            if (secondaryInput.Value && canPickupFromTablet)
            {
                currentTabletPickupTime += Time.fixedDeltaTime;
                if(IsServer && currentTabletPickupTime >= tabletPickupTime)
                {
                    currentTabletPickupTime = 0;
                    if (currentDeployable[^1].TryGet(out NetworkObject nob))
                    {
                        nob.Despawn();
                        currentDeployable.RemoveAt(currentDeployable.Count -1);
                    }
                }
            }
            else
            {
                currentTabletPickupTime = 0;
            }
        }
        else
        {
            currentTabletPickupTime = 0;
            canCooldown = true;
        }
        tabletPickupImage.fillAmount = Mathf.InverseLerp(0, tabletPickupTime, currentTabletPickupTime);
        if (currentGear.Value && currentStoredUses.Value > 0)
        {
            if ((cooldownBlockedByPlacement && currentDeployable.Count < maxDeployables) || !cooldownBlockedByPlacement)
            {
                print("Able to place turret");
                if (CheckPlacement() && !obstructed && primaryInput.Value)
                {
                    print("placed turret!");
                    PlaceDeployable();
                    CheckStillUsable();
                }
            }
            else
            {
                print("deployment is blocked due to existing deployable");
                RemoveHologram();
            }
        }
        else
        {
            print("deployable not selected, removing hologram and ignoring");
            RemoveHologram();
        }
        
    }
    private void Update()
    {
        
    }
    void PlaceDeployable()
    {
        if (IsServer)
        {
            var net = NetworkManager.SpawnManager.InstantiateAndSpawn(deployablePrefab, OwnerClientId, position: targetedPoint, rotation: deploymentOrientation);
            currentDeployable.Add(net);
            currentStoredUses.Value--;
            localCooldown = cooldownDuration;
            RemoveHologram();
        }
    }

    [SerializeField] internal Quaternion deploymentOrientation = Quaternion.identity;
    bool CheckPlacement()
    {
        if (IsOwner)
        {
            if (Physics.Raycast(wm.fireDirectionReference.position, wm.fireDirectionReference.forward, out RaycastHit hit, maxPlacementDistance, placementLayermask))
            {
                if (ResolvePlacement(hit))
                {
                        CreateHologram();
                    //We can show the hologram preview here
                    deploymentOrientation = Quaternion.LookRotation(Vector3.ProjectOnPlane(wm.fireDirectionReference.forward, hit.normal).normalized, hit.normal);
                    obstructed = Physics.CheckBox(hit.point + deploymentOrientation * placementObstructionBoxOffset, placementObstructionBoxBounds / 2, deploymentOrientation, obstructionLayermask);
                    if (hologramInstance)
                    {
                        hologramInstance.transform.SetPositionAndRotation(hit.point, deploymentOrientation);
                    }
                    if (hologramRenderer)
                    {
                        hologramRenderer.material.SetFloat("_Validity", obstructed ? 0 : 1);
                    }
                    targetedPoint = hit.point;
                    normal = hit.normal;
                    if (obstructed)
                    {
                        //Hologram is obstructed
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    print("removing hologram");
                    RemoveHologram();
                }
            }
            else
            {
                print("removing hologram");
                RemoveHologram();
            }
        }
        return false;
    }
    void CreateHologram()
    {
        if (hologramInstance)
            return;
        hologramInstance = Instantiate(deployableHologram);
        hologramRenderer = hologramInstance.GetComponent<Renderer>();
    }
    void RemoveHologram()
    {
        if (hologramInstance != null)
        {
            Destroy(hologramInstance);
            hologramRenderer = null;
            hologramInstance = null;
        }
    }
    bool ResolvePlacement(RaycastHit hit)
    {
        if (canPlaceOnAllSides)
        {
            return true;
        }
        else
        {
            bool[] blocked = new bool[3];
            if (!allowedSides.x)
            {
                if (Mathf.Abs(Vector3.Dot(hit.normal, Vector3.right)) < placementDisallowDotThreshold)
                    blocked[0] = true;
            }
            if (!allowedSides.y)
            {
                if (Mathf.Abs(Vector3.Dot(hit.normal, Vector3.up)) < placementDisallowDotThreshold)
                    blocked[1] = true;
            }
            if (!allowedSides.z)
            {
                if (Mathf.Abs(Vector3.Dot(hit.normal, Vector3.up)) < placementDisallowDotThreshold)
                    blocked[2] = true;
            }


            return blocked[0] || blocked[1] || blocked[2];
        }
    }

}
