using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class Projectile : NetworkBehaviour
{

    public Vector3 Velocity { get; private set; }
    public float Speed { get; private set; }

    public virtual void InitialiseProjectile(Vector3 velocity, Vector3 position)
    {
        transform.position = lastPosition = position;
        nextPosition = lastPosition + (velocity * Time.fixedDeltaTime);
        transform.forward = velocity;
        Velocity = velocity;
        Speed = velocity.magnitude;
    }
    [SerializeField, Tooltip("If the direction the projectile is travelling in is GREATER than this threshold, we will bounce.\nIf it is less than this, the projectile is heading too directly into the surface.")] 
    protected float bounceDotThreshold;
    [SerializeField] protected int maxBounces;
    protected int remainingBounces;
    [SerializeField] protected float bounciness;
    [SerializeField] protected bool ignoreBouncesOnDamage;
    [SerializeField] protected bool useGravity;
    [SerializeField] protected Vector3 lastPosition, nextPosition;
    [SerializeField] protected float pathCastRadius;

    [SerializeField] protected float distanceTravelled;
    [SerializeField] protected float selfDamageDistance;
    [SerializeField] protected float minDamage, maxDamage, damageDropoffStart, damageDropoffEnd;
    [SerializeField] protected float headshotMultiplier = 1;
    [SerializeField] UnityEvent hitEvent;
    private void FixedUpdate()
    {
        if (IsServer)
        {
            RaycastHit hit;
            Debug.DrawRay(transform.position, Velocity * Time.fixedDeltaTime, Color.yellow, 0.1f);
            if(pathCastRadius > 0)
            {
                Physics.SphereCast(transform.position, pathCastRadius, Velocity, out hit, Speed * Time.fixedDeltaTime * 1.1f, GameplayManager.Instance.bulletLayermask);
            }
            else
            {
                Physics.Raycast(transform.position, Velocity, out hit, Speed * Time.fixedDeltaTime * 1.1f, GameplayManager.Instance.bulletLayermask);
            }
            if (hit.collider)
            {
                PlayerCharacter p = PlayerCharacter.players.First(x => x.OwnerClientId == OwnerClientId);
                NetworkObject n = GetComponentInParent<NetworkObject>();
                if (n == null || n != p.NetworkObject)
                {
                    if(hit.collider.TryGetComponent(out IDamageable d) && d.NetObject != p.NetObject)
                    {
                        float damage = Mathf.Lerp(minDamage, maxDamage, Mathf.InverseLerp(damageDropoffStart, damageDropoffEnd, distanceTravelled));
                        if (d is Hitbox h)
                        {
                        damage *= h.isHead ? headshotMultiplier : 1;
                        }
                        d.TakeDamage(damage);
                        nextPosition = hit.point;

                    }
                    bool canBounce = CanBounce(hit, d != null);
                    if (canBounce)
                    {
                        Velocity = Vector3.Reflect(Velocity, hit.normal) * bounciness;
                    }
                    else
                    {
                        hitEvent?.Invoke();
                        if(IsSpawned)
                            NetworkObject.Despawn();
                    }
                }
            }
            else
            {
                Velocity += (useGravity ? Physics.gravity : Vector3.zero) * Time.fixedDeltaTime;
                nextPosition += Velocity * Time.fixedDeltaTime;
                transform.position = nextPosition;
                transform.forward = Velocity;
                distanceTravelled += Speed * Time.fixedDeltaTime;
                lastPosition = nextPosition;
            }
            }
    }
    bool CanBounce(RaycastHit hit, bool hitDamageable)
    {
        if (maxBounces <= 0 || remainingBounces <= 0 || (hitDamageable && ignoreBouncesOnDamage))
            return false;
        else
        {
            return remainingBounces > 0 && Vector3.Dot(transform.forward, hit.normal) > bounceDotThreshold;
        }
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, pathCastRadius);
    }
}
