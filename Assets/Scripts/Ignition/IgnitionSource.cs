using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class IgnitionSource : NetworkBehaviour
{
    /// <summary>
    /// How the Ignition Source should check for Flammables
    /// </summary>
    public enum CheckType
    {
        None = 0,
        Box = 1,
        Sphere = 2,
    }
    public CheckType checkType = CheckType.None;
    public float sphereCheckRadius;
    public Vector3 checkOffset, boxCheckSize;
    public float burnPerTick;
    public float burnDuration;
    public bool activated;
    public List<Collider> ignoreColliders;
    protected virtual void FixedUpdate()
    {
        if (IsServer && GameplayManager.Instance && activated)
        {
            Collider[] cols = new Collider[20];
            switch (checkType)
            {
                case CheckType.None:
                    return;
                case CheckType.Box:
                    Physics.OverlapBoxNonAlloc(transform.TransformPoint(checkOffset), boxCheckSize / 2, cols, Quaternion.identity, GameplayManager.Instance.fireMask);
                    break;
                case CheckType.Sphere:
                    Physics.OverlapSphereNonAlloc(transform.TransformPoint(checkOffset), sphereCheckRadius, cols, GameplayManager.Instance.fireMask);
                    break;
                default:
                    return;
            }
            if (cols.Length == 0)
                return;
            for (int i = 0; i < cols.Length; i++)
            {
                if (cols[i] == null)
                    continue;
                Collider col = cols[i];
                if (ignoreColliders.Count > 0 && ignoreColliders.Contains(col))
                {
                    print($"Ignoring collider {col.name}");
                    continue;
                }
                if (col.TryGetComponent(out IFlammable f))
                {
                    print($"trying to ignite {col.gameObject.name}");
                    f.TryIgnite(burnDuration, burnPerTick);
                }
            }
        }
    }
    

    private void OnDrawGizmosSelected()
    {
        switch (checkType)
        {
            case CheckType.None:
                break;
            case CheckType.Box:
                Gizmos.DrawWireCube(transform.TransformPoint(checkOffset), boxCheckSize);
                break;
            case CheckType.Sphere:
                Gizmos.DrawWireSphere(transform.TransformPoint(checkOffset), sphereCheckRadius);
                break;
            default:
                break;
        }
    }
}
