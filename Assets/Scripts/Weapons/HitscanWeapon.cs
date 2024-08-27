using opus.Gameplay;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Pool;

namespace opus.Weapons
{
    public class HitscanWeapon : BaseWeapon
    {
        IObjectPool<TrailRenderer> _tracerPool;
        public IObjectPool<TrailRenderer> TracerPool
        {
            get
            {
                _tracerPool ??= new ObjectPool<TrailRenderer>(CreatePooledTracer, OnTakeFromPool, OnReturnedToPool, OnDestroyPoolObject, true, (int)roundsPerSecond * 3, (int)roundsPerMinute);
                return _tracerPool;
            }
        }

        private void OnDestroyPoolObject(TrailRenderer renderer)
        {
            Destroy(renderer.gameObject);
        }

        private void OnReturnedToPool(TrailRenderer renderer)
        {
            renderer.Clear();
            renderer.gameObject.SetActive(false);
            renderer.emitting = false;
        }

        private void OnTakeFromPool(TrailRenderer renderer)
        {
            renderer.gameObject.SetActive(true);
        }

        private TrailRenderer CreatePooledTracer()
        {
            var go = Instantiate(tracerPrefab);
            var tr = go.GetComponent<TrailRenderer>();
            tr.emitting = false;
            return tr;
        }

        [SerializeField, Tooltip("The maximum distance this weapon will fire across")] protected float maxRange = 300;
        [SerializeField] protected GameObject tracerPrefab;
        [SerializeField] protected float tracerSpeed;
        [SerializeField] protected Transform tracerOrigin;
        public List<Tracer> tracers;
        [System.Serializable]
        public class Tracer
        {
            public Transform tracer;
            public TrailRenderer renderer;
            public float t;
            public float distance;
            public Vector3 start, end;
            public float increment;
        }
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            if (TracerPool != null)
            {
                TracerPool.Clear();
            }
        }
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                currentAmmo.Value = maxAmmo;
            }
            if (IsOwner)
            {
                primaryInput.Value = false;
            }
            tracers = new();
        }


        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            if (tracers.Count > 0)
            {
                tracers.ForEach(x =>
                {
                    if (x.tracer != null)
                    {
                        x.renderer.emitting = true;
                        x.tracer.position = Vector3.Lerp(x.start, x.end, x.t);
                    }
                    x.t += x.increment * Time.fixedDeltaTime;
                    if(x.t >= 25)
                    {
                        TracerPool.Release(x.renderer);
                        x.renderer = null;
                    }
                });
                tracers.RemoveAll(x => x.renderer == null);
            }
        }
        protected override void FireWeaponOnServer(NetworkObject ownerObject)
        {
            Vector3 start;
            Vector3 end;
            base.FireWeaponOnServer(ownerObject);
            if (wm)
            {
                start = wm.fireDirectionReference.position;
                Vector3 spread = SpreadVector(minBaseSpread, maxBaseSpread, maxRange) + ((1 - wm.aimAmount) * wm.accumulatedSpread * SpreadVector(minHipSpread, maxHipSpread, 0));
                end = wm.fireDirectionReference.TransformPoint(spread);
            }
            else
            {
                start = transform.position;
                end = transform.TransformPoint(Vector3.forward * maxRange);
            }
            RaycastHit[] hits = Physics.RaycastAll(start, end - start, maxRange, GameplayManager.Instance.bulletLayermask, QueryTriggerInteraction.Ignore);
            RaycastHit? closest = null;
            Debug.DrawRay(start, end - start, Color.red, 1f);
            IDamageable closestDamageable = null;
            float distance = maxRange * 2;
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].distance < distance)
                {
                    if (hits[i].collider.TryGetComponent(out closestDamageable) && closestDamageable.NetObject == ownerObject)
                    {
                        //We need to ignore this damageable as it belongs to us.
                        closestDamageable = null;
                        continue;
                        
                    }
                    distance = hits[i].distance;
                    closest = hits[i];
                }
            }
            if (closest != null)
            {
                end = closest.Value.point;
                if (closestDamageable != null)
                {
                    bool didHeadshotDamage = false;
                    float damage = Mathf.Lerp(maxDamage, minDamage, Mathf.InverseLerp(damageDropoffStart, damageDropoffEnd, closest.Value.distance));
                    if (closestDamageable is Hitbox h)
                    {
                        h.TakeDamage(damage * (h.isHead ? headshotDamageMultiplier : 1));
                        didHeadshotDamage = h.isHead;
                    }
                    else
                    {
                        closestDamageable.TakeDamage(damage);
                    }
                    if (wm)
                    {
                        wm.HitFeedback_RPC(didHeadshotDamage);
                    }
                    else
                    {
                        PlayerCharacter.players.First(x => x.OwnerClientId == OwnerClientId).wm.HitFeedback_RPC(didHeadshotDamage);
                    }
                    print($"dealing {damage}/{maxDamage}(headshot:{didHeadshotDamage}) to entity.");
                }
                Debug.DrawLine(start, end, Color.green, 1f);
            }
            FireWeapon_RPC(end);
            print("fired weapon on server");
        }
        public override void FireWeapon(Vector3 end)
        {
            base.FireWeapon(end);
            print("instantiating tracer");
            Vector3 start = tracerOrigin.position;
            float distance = Vector3.Distance(start, end);
            float tracerIncrement = tracerSpeed / distance;
            TrailRenderer trace = TracerPool.Get();
            Tracer t = new()
            {
                tracer = trace.transform,
                renderer = trace,
                start = start,
                end = end,
                distance = distance,
                increment = tracerIncrement,
                t = 0
            };
            Debug.DrawLine(start, end, Color.red, 3);
            tracers.Add(t);
            trace.transform.position = t.start;
        }
    }
}