using opus.Gameplay;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.VFX;

namespace opus.Weapons
{
    public class HitscanWeapon : BaseWeapon
    {
        [SerializeField, Tooltip("How much damage this weapon does at the dropoff start")] protected float maxDamage = 20;
        [SerializeField, Tooltip("How much damage this weapon does at the dropoff end")] protected float minDamage = 1;
        [SerializeField, Tooltip("How much the damage is multiplied by on a headshot")] protected float headshotDamageMultiplier;
        [SerializeField, Tooltip("The distance in metres before which the weapon deals max damage")] protected float damageDropoffStart = 10;
        [SerializeField, Tooltip("The distance in metres after which the weapon deals min damage")] protected float damageDropoffEnd = 100;
        [SerializeField, Tooltip("The maximum distance this weapon will fire across")] protected float maxRange = 300;
        [SerializeField] protected GameObject tracerPrefab;
        [SerializeField] protected float tracerSpeed;
        [SerializeField] protected Transform tracerOrigin;
        public List<Tracer> tracers;
        [System.Serializable]
        public class Tracer
        {
            public Transform tracer;
            public float t;
            public float distance;
            public Vector3 start, end;
            public float increment;
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
                        x.tracer.position = Vector3.Lerp(x.start, x.end, x.t);
                    }
                    x.t += x.increment * Time.fixedDeltaTime;
                });
                tracers.RemoveAll(x => x.tracer == null);
            }
        }
        protected override void FireWeaponOnServer()
        {
            Vector3 start;
            Vector3 end;
            base.FireWeaponOnServer();
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
            if (Physics.Linecast(start, end, out RaycastHit hit, GameplayManager.Instance.bulletLayermask))
            {
                if (hit.collider.TryGetComponent<Hitbox>(out var d))
                {
                    float damage = Mathf.Lerp(maxDamage, minDamage, Mathf.InverseLerp(damageDropoffStart, damageDropoffEnd, hit.distance));
                    d.TakeDamage(damage * (d.isHead ? headshotDamageMultiplier : 1));
                }
                else
                {
                    if(hit.collider.TryGetComponent<Damageable>(out var d1))
                    {
                        float damage = Mathf.Lerp(minDamage, maxDamage, Mathf.InverseLerp(damageDropoffStart, damageDropoffEnd, hit.distance));
                        d.TakeDamage(damage);
                    }
                }
                end = hit.point;
            }
            else
            {
                print("did not hit anything");
            }
            FireWeapon_RPC(end);
            print("fired weapon on server");
        }
        public override void FireWeapon(Vector3 end)
        {
            base.FireWeapon(end);
            Vector3 start = tracerOrigin.position;
            float distance = Vector3.Distance(start, end);
            float tracerIncrement = tracerSpeed / distance;
            Tracer t = new()
            {
                tracer = Instantiate(tracerPrefab, tracerOrigin.position, Quaternion.identity).transform,
                start = start,
                end = end,
                distance = distance,
                increment = tracerIncrement,
                t = 0
            };
            Debug.DrawLine(start, end, Color.red, 3);
            tracers.Add(t);
        }
    }
}