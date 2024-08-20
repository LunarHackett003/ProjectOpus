using UnityEngine;
using Unity.Netcode;
using opus.utility;
using opus.Gameplay;


#if UNITY_EDITOR
using UnityEditor;
#endif
public class TurretController : NetworkBehaviour, IDamageable
{
    public NetworkVariable<ulong> ownerSteamID = new(writePerm: NetworkVariableWritePermission.Owner);

    [SerializeField] BaseWeapon turretWeapon;

    [SerializeField] NetworkVariable<float> currentHealth = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] float maxHealth;

    [SerializeField] Transform turretTransform;
    [SerializeField] float pitchAngle, yawAngle;
    [SerializeField] float viewFOV, viewRange;

    [SerializeField] float rotateSpeed;

    [SerializeField] LayerMask detectionTryMask, detectionBlockMask;
    [SerializeField] LineRenderer visLineRenderer;
    [SerializeField] Transform pointer;
    [SerializeField] bool ignoreTeams;

    [SerializeField] PlayerCharacter currentTarget;

    public NetworkObject NetObject => NetworkObject;

    public Transform ThisTransform => transform;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            currentHealth.Value = maxHealth;
        }
        if (IsOwner)
        {
            ownerSteamID.Value = PlayerManager.Instance.pc.mySteamID.Value;
        }
    }
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
    }
    private void FixedUpdate()
    {
        if (IsServer)
        {
            if(ownerSteamID.Value == 0)
                return;

            TurretCheck();
            TurretFire();
        }

        Physics.Raycast(visLineRenderer.transform.position, turretWeapon.transform.forward, out RaycastHit hit, viewRange, detectionBlockMask);
        visLineRenderer.SetPosition(1, Vector3.forward * (hit.collider ? hit.distance : viewRange));
    }
    bool TargetValidate(PlayerCharacter item)
    {
        float dist = UtilityMethods.SquaredDistance(turretWeapon.transform.position, item.transform.position);
        //Will proceed if the target is in range, visible, the closest target AND if we're either ignoring teams or the target is on the other team.
        Debug.DrawLine(turretWeapon.transform.position, item.rb.worldCenterOfMass, Color.red, 0.1f);
        return dist < viewRange * viewRange && Vector3.Angle(turretTransform.forward, item.transform.position - turretTransform.position) < viewFOV &&
            !Physics.Linecast(turretWeapon.transform.position, item.rb.worldCenterOfMass, detectionBlockMask)
            && (ignoreTeams || GameplayManager.Instance.IsOppositeTeam(ownerSteamID.Value, item.mySteamID.Value)) && !item.Dead.Value;
    }
    void TurretCheck()
    {
        if (currentTarget)
        {
            if (!TargetValidate(currentTarget))
            {
                currentTarget = null;
            }
        }
        else
        {
            print("Checking for new target");
            PlayerCharacter closestPlayer = null;
            float closestDistance = 99999999;
            foreach (var item in PlayerCharacter.players)
            {
                float dist = UtilityMethods.SquaredDistance(turretWeapon.transform.position, item.transform.position);
                //Will proceed if the target is in range, visible, the closest target AND if we're either ignoring teams or the target is on the other team.
                Debug.DrawLine(turretWeapon.transform.position, item.rb.worldCenterOfMass, Color.red, 0.1f);
                if (TargetValidate(item) && dist < closestDistance)
                {
                    closestDistance = dist;
                    closestPlayer = item;
                }
            }
            if(closestPlayer != null)
                currentTarget = closestPlayer;
        }
    }
    void TurretFire()
    {
        if (currentTarget)
        {
            print("getting turret direction");
            Vector3 direction = currentTarget.rb.worldCenterOfMass - turretWeapon.transform.position;
            pointer.forward = direction;
            print("setting turret forward");
            turretTransform.forward = Vector3.RotateTowards(turretTransform.forward, direction, rotateSpeed * Time.fixedDeltaTime, 5);
            print("setting turret fire");
            turretWeapon.primaryInput.Value = Vector3.Dot(turretWeapon.transform.forward, direction.normalized) > 0.9f;
        }
        else
        {
            turretWeapon.primaryInput.Value = false;
        }
    }
    public void TakeDamage(float damageAmount)
    {
        currentHealth.Value -= damageAmount;
        if(currentHealth.Value < 0)
        {
            NetworkObject.Despawn();
        }
    }




#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (turretWeapon)
        {
            Handles.DrawLine(turretWeapon.transform.position, turretWeapon.transform.TransformPoint(Vector3.forward * viewRange));
            Handles.DrawLine(turretWeapon.transform.position, turretWeapon.transform.TransformPoint(Quaternion.Euler(viewFOV, 0, 0) * Vector3.forward * viewRange));
            Handles.DrawLine(turretWeapon.transform.position, turretWeapon.transform.TransformPoint(Quaternion.Euler(-viewFOV, 0, 0) * Vector3.forward * viewRange));
            Handles.DrawLine(turretWeapon.transform.position, turretWeapon.transform.TransformPoint(Quaternion.Euler(0, -viewFOV, 0) * Vector3.forward * viewRange));
            Handles.DrawLine(turretWeapon.transform.position, turretWeapon.transform.TransformPoint(Quaternion.Euler(0, viewFOV, 0) * Vector3.forward * viewRange));
        }
    }
#endif
}
