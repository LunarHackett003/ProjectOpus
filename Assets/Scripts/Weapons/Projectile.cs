using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using Unity.Netcode;
using UnityEngine;

namespace Opus
{
    public class Projectile : ONetBehaviour
    {
        public RangedWeapon weaponRef;


        public Rigidbody rb;
        public float launchSpeed;
        public NetworkObject impactPrefab;
        public bool destroyHitPrefabAfterDelay;
        public float hitPrefabDestroyTime;
        public bool useBounces, useFuse;
        public float fuseLength, currentFuseTime;
        public int numberOfBounces;
        public bool hideRenderer;

        public float projectileRadius;
        public LayerMask projectileMask;
        public LayerMask damageLayermask;
        float distanceTravelled;

        [Tooltip("The point at which the projectile will deal minimum damage")]
        public float minDamageRadius;
        [Tooltip("THe point at which the projectile will deal maximum damage")]
        public float maxDamageRadius;
        public float minDamage, maxDamage;

        public NetworkVariable<bool> projectileAlive = new(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public Renderer[] projectileRenderers;
        public Collider[] colliders;

        public DamageType damageType;
        public float critMultiplier;

        public bool useDamageRadius => maxDamageRadius > 0 || minDamageRadius > 0;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer)
            {
                rb.isKinematic = false;
                rb.linearVelocity = transform.forward * launchSpeed;
            }
            
        }
        float rbmag;
        public override void OFixedUpdate()
        {
            base.OFixedUpdate();

            if(IsServer)
                rb.isKinematic = !projectileAlive.Value;

            if (!projectileAlive.Value)
                return;
            transform.forward = rb.linearVelocity;
            if (useFuse)
            {
                currentFuseTime += Time.fixedDeltaTime;
                if(currentFuseTime >= fuseLength)
                {
                    if (IsServer)
                    {
                        DoHitEffect(null);
                    }
                }
            }
            rbmag = rb.linearVelocity.magnitude * Time.fixedDeltaTime;
            if(weaponRef != null && weaponRef.useDamageRanging)
            {
                distanceTravelled += rbmag;
            }

            if (IsServer)
            {
                if (Physics.SphereCast(transform.position, projectileRadius, rb.linearVelocity * Time.fixedDeltaTime, out RaycastHit hit, rbmag, projectileMask))
                {
                    Debug.DrawRay(transform.position, rb.linearVelocity * Time.fixedDeltaTime);
                    if (hit.collider)
                    {
                        transform.position = hit.point;
                    }
                }
            }
        }
        private void OnCollisionEnter(Collision collision)
        {
            bool hit = false;
            numberOfBounces--;
            if(useBounces && numberOfBounces <= 0)
            {
                transform.up = collision.GetContact(0).normal;
                hit = true;
            }
            if (hit)
            {
                DoHitEffect(collision);
            }
        }
        void DoHitEffect(Collision collision)
        {
            NetworkObject hitEffect = NetworkManager.SpawnManager.InstantiateAndSpawn(impactPrefab, OwnerClientId, false, false, false, transform.position, transform.rotation);
            if (destroyHitPrefabAfterDelay)
            {
                StartCoroutine(DestroyHitPrefab(hitEffect));
            }
            DoHitEffect_RPC();
            if (useDamageRadius)
            {
                Collider[] array = Physics.OverlapSphere(transform.position, minDamageRadius, damageLayermask, QueryTriggerInteraction.Ignore);
                HashSet<Rigidbody> bodies = new();
                for (int i = 0; i < array.Length; i++)
                {
                    Collider item = array[i];
                    if (item.attachedRigidbody && !bodies.Contains(item.attachedRigidbody))
                    {
                        bodies.Add(item.attachedRigidbody);
                        if (item.attachedRigidbody.TryGetComponent(out Entity entity))
                        {
                            entity.ReceiveDamage(Mathf.Lerp(maxDamage, minDamage,
                                Mathf.InverseLerp(maxDamageRadius, minDamageRadius, Vector3.Distance(item.ClosestPoint(transform.position), transform.position))), OwnerClientId, critMultiplier, damageType);
                        }
                    }
                }
            }
            else
            {
                if (collision != null && collision.rigidbody)
                {
                    if (collision.rigidbody.TryGetComponent(out Entity entity))
                    {
                        entity.ReceiveDamage(Mathf.Lerp(maxDamage, minDamage,
                            Mathf.InverseLerp(maxDamageRadius, minDamageRadius, Vector3.Distance(collision.collider.ClosestPoint(transform.position), transform.position))), OwnerClientId, critMultiplier, damageType);
                    }
                }
            }
            projectileAlive.Value = false;
            if(NetworkObject.IsSpawned)
                NetworkObject.Despawn(true);
        }
        [Rpc(SendTo.ClientsAndHost)]
        void DoHitEffect_RPC()
        {
            if (hideRenderer)
            {
                for (int i = 0; i < projectileRenderers.Length; i++)
                {
                    projectileRenderers[i].enabled = false;
                }
            }
                for (int i = 0; i < colliders.Length; i++)
                {
                    colliders[i].enabled = false;
                }
        }
        IEnumerator DestroyHitPrefab(NetworkObject hitObject)
        {
            yield return new WaitForSeconds(hitPrefabDestroyTime);
            hitObject.Despawn();
            yield break;
        }
    }
}
