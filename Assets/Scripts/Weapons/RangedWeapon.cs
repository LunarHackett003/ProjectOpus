using Unity.Netcode;
using UnityEngine;
using System.Collections;
using UnityEngine.Pool;
using System.Collections.Generic;
namespace Opus
{
    public struct Tracer
    {
        public Vector3 start, end;
        public TrailRenderer t;
        public float time, speed;
    }

    public class RangedWeapon : BaseWeapon
    {

        IObjectPool<TrailRenderer> _tracerPool;
        public IObjectPool<TrailRenderer> TracerPool
        {
            get
            {
                _tracerPool ??= new ObjectPool<TrailRenderer>(CreatePooledTracer, OnTakeFromPool, OnReturnedToPool, OnDestroyPoolObject, true, Mathf.FloorToInt(roundsPerMinute / 20), Mathf.FloorToInt(roundsPerMinute / 2));
                return _tracerPool;
            }

        }

        private void OnDestroyPoolObject(TrailRenderer renderer)
        {
            Destroy(renderer.gameObject);
        }

        private void OnReturnedToPool(TrailRenderer renderer)
        {
            renderer.gameObject.SetActive(false);
            renderer.emitting = false;
        }

        private void OnTakeFromPool(TrailRenderer renderer)
        {
            renderer.gameObject.SetActive(true);
            renderer.emitting = true;
            renderer.Clear();
        }

        private TrailRenderer CreatePooledTracer()
        {
            var go = Instantiate(tracerPrefab);
            var tr = go.GetComponent<TrailRenderer>();
            tr.emitting = false;
            return tr;
        }





        public float roundsPerMinute;
        float timeBetweenRounds;
        [Tooltip("How long, in seconds, the weapon waits after firing a burst before allowing the player to fire again")]
        public float timeBetweenBursts;
        [Tooltip("How quickly the weapon charges up when firing.")]
        public float chargeSpeed;
        [Tooltip("How quickly the weapon \"Cools down\"")]
        public float chargeDecay;
        [Tooltip("How much charge the weapon currently has")]
        public float CurrentCharge { get; private set; }
        [Tooltip("How many shots are fired in a burst")]
        public int burstFireRounds;
        bool burstFiring;
        public Vector3 recoilPos, recoilEuler;
        public readonly NetworkVariable<bool> fireInputSynced = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        bool fireInputPressed;
        bool fired;
        public WeaponFireType weaponFireType;
        public WeaponFireBehaviour fireBehaviour;

        public int maxAmmo;
        public int CurrentAmmo { get; private set; }
        public NetworkVariable<int> syncedAmmo = new(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public bool reloading;

        public GameObject tracerPrefab;
        public Transform muzzle;
        public float tracerSpeed;
        public float tracerRemovalTime;

        public LayerMask damageLayermask;
        public float maxRange, minRange;
        public float maxRangeDamage, minRangeDamage;
        public AnimationCurve damageFalloff;
        public float critMultiplier = 1;
        public int fireIterations = 1;

        List<Tracer> tracers = new();
        void UpdateAmmo()
        {
            SendAmmo_RPC(CurrentAmmo);
            syncedAmmo.Value = CurrentAmmo;
        }

        public void AddAmmo(int ammoToAdd)
        {
            if (IsServer)
            {
                CurrentAmmo += ammoToAdd;
                UpdateAmmo();
            }
        }
        public void RefillAmmo()
        {
            if (IsServer)
            {
                CurrentAmmo = maxAmmo;
                SendAmmo_RPC(CurrentAmmo);
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            fireInput = fireInputSynced.Value;
            if (syncedAmmo.Value != -1)
            {
                CurrentAmmo = syncedAmmo.Value;
            }
            else
            {
                CurrentAmmo = maxAmmo;
            }
            OnValidate();
        }
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            
            TracerPool?.Clear();
        }

        protected override void FixedUpdate()
        {
            //if (fireInput)
            //{
            //    TryFire();
            //}
            //else
            //{
            //    fireInputPressed = false;
            //}
            reloading = acpp.customParams[0].boolValue;

            if (IsOwner)
            {
                if (lastFireInput != fireInput)
                {
                    lastFireInput = fireInput;
                    SendFireInput_RPC(fireInput);
                }
            }

            if(tracers.Count > 0)
            {
                if (tracers.Count > 0)
                {
                    for (int i = 0; i < tracers.Count; i++)
                    {
                        Tracer x = tracers[i];
                        if (x.t != null)
                        {
                            x.t.emitting = true;
                            x.t.transform.position = Vector3.Lerp(x.start, x.end, x.time);
                        }
                        x.time += x.speed * Time.fixedDeltaTime;
                        if (x.time >= tracerRemovalTime)
                        {
                            TracerPool.Release(x.t);
                            x.t = null;
                        }
                        tracers[i] = x;
                    }
                    tracers.RemoveAll(x => x.t == null);
                }
            }

            if (fired || reloading || (maxAmmo > 0 && CurrentAmmo <= 0))
                return;
            switch (weaponFireType)
            {
                case WeaponFireType.onPress:
                    if (fireInput)
                    {
                        TryFire();
                        fireInputPressed = true;
                    }
                    else
                    {
                        fireInputPressed = false;
                    }
                    break;
                case WeaponFireType.onReleaseInstant:
                    if (fireInput)
                    {
                        fireInputPressed = true;
                    }
                    if (!fireInput)
                    {

                    }
                    break;
                case WeaponFireType.onReleaseAnimated:
                    break;
                case WeaponFireType.chargeHold:
                    break;
                case WeaponFireType.chargePress:
                    break;
                default:
                    break;
            }
        }
        void TryFire()
        {
            switch (fireBehaviour)
            {
                case WeaponFireBehaviour.semiAutomatic:
                    if (!fireInputPressed)
                    {
                        Fire();
                    }
                    break;
                case WeaponFireBehaviour.fullyAutomatic:
                    Fire();
                    break;
                case WeaponFireBehaviour.burstFireOnce:
                    if (!fireInputPressed && !burstFiring)
                    {
                        StartCoroutine(BurstFire());
                    }
                    break;
                case WeaponFireBehaviour.burstFireLoop:
                    if (!burstFiring)
                    {
                        StartCoroutine(BurstFire());
                    }
                    break;
                case WeaponFireBehaviour.animatedReset:
                    Fire();
                    break;
                default:
                    break;
            }
        }
        [Rpc(SendTo.Everyone)]
        public void SendFireInput_RPC(bool input)
        {
            fireInput = input;

            if (IsServer)
            {
                fireInputSynced.Value = input;
            }
        }
        void Fire()
        {
            if (IsServer)
            {
                FireOnServer();
            }
            if (IsClient)
            {
                FireOnClient();
            }
            fired = true;
            StartCoroutine(ResetFire());
        }
        IEnumerator ResetFire()
        {
            yield return new WaitForSeconds(timeBetweenRounds);
            fired = false;
        }
        public IEnumerator ChargeToFull()
        {
            WaitForFixedUpdate wff = new();
            while (CurrentCharge < 1)
            {
                CurrentCharge += chargeSpeed * Time.fixedDeltaTime;
                yield return wff;
            }

        }
        public IEnumerator BurstFire()
        {
            WaitForSeconds wait = new(timeBetweenRounds);
            int fired = 0;
            while (fired < burstFireRounds)
            {
                Fire();
                fired++;
                if (fired != burstFireRounds - 1)
                    yield return wait;
            }
            yield return new WaitForSeconds(timeBetweenBursts);
            if (fireBehaviour == WeaponFireBehaviour.burstFireLoop)
            {
                fireInputPressed = false;
            }
        }
        RaycastHit[] workingRaycastHits;
        public void FireOnServer()
        {
            //If a client somehow gets here, we need to make sure they don't get any further.
            if (!IsServer)
                return;
            CurrentAmmo--;
            UpdateAmmo();
            Vector3[] hitEndPoints = new Vector3[fireIterations];
            for (int f = 0; f < fireIterations; f++)
            {
                workingRaycastHits = Physics.RaycastAll(myController.Controller.worldCineCam.transform.position, 
                    myController.Controller.headTransform.forward, maxRange, damageLayermask, QueryTriggerInteraction.Ignore);
                if(workingRaycastHits.Length > 0)
                {
                    float closestHitDistance = maxRange + 1;
                    int closestHitIndex = -1;
                    for (int i = 0; i < workingRaycastHits.Length; i++)
                    {
                        RaycastHit hit = workingRaycastHits[i];
                        if(hit.rigidbody != null)
                        {
                            if(hit.rigidbody.transform == myController.transform)
                            {
                                //Ignore this one, we just hit ourselves.
                                continue;
                            }
                            else
                            {
                                //We did not hit ourself! hooray!
                            }
                        }
                        else
                        {
                            //We must've hit something static.
                        }
                        if (hit.distance < closestHitDistance)
                        {
                            closestHitIndex = i;
                            closestHitDistance = hit.distance;
                        }
                    }

                    if(closestHitIndex != -1)
                    {
                        RaycastHit hit = workingRaycastHits[closestHitIndex];
                        if (hit.collider.TryGetComponent(out Hitbox box))
                        {
                            float damage = damageFalloff.Evaluate(Mathf.Clamp01(Mathf.InverseLerp(minRange, maxRange, hit.distance)));
                            box.ReceiveDamage(damage, OwnerClientId, critMultiplier);
                        }
                        hitEndPoints[f] = hit.point;
                    }
                    else
                    {
                        hitEndPoints[f] = myController.Controller.headTransform.position +
                            (myController.Controller.headTransform.forward * maxRange);
                    }
                }
                else
                {
                    hitEndPoints[f] = myController.Controller.headTransform.position +
                        (myController.Controller.headTransform.forward * maxRange);
                }
            }
            SendTracer_RPC(hitEndPoints);

            //SendTracer_RPC(Vector3.zero, Vector3.zero);
        }
        Tracer[] tracerWorkingArray;
        [Rpc(SendTo.Everyone)]
        public void SendTracer_RPC(Vector3[] end)
        {
            tracerWorkingArray = new Tracer[end.Length];
            for (int i = 0; i < end.Length; i++)
            {
                Tracer t = new()
                {
                    t = TracerPool.Get(),
                    time = 0,
                    start = muzzle.position,
                    end = end[i],
                    speed = tracerSpeed / Vector3.Distance(muzzle.position, end[i])
                };
                tracerWorkingArray[i] = t;
                t.t.transform.position = muzzle.position;
                t.t.Clear();
            }
            tracers.AddRange(tracerWorkingArray);
            tracerWorkingArray = new Tracer[0];
        }
        [Rpc(SendTo.ClientsAndHost)]
        void SendAmmo_RPC(int ammoAmount)
        {
            CurrentAmmo = ammoAmount;
        }
        public void FireOnClient()
        {
            if (fireParticleSystem)
            {
                fireParticleSystem.Play();
            }
            if (fireVFX)
            {
                fireVFX.Play();
            }
            if (myController)
            {
                myController.ReceiveShot();
            }
        }
        private void OnValidate()
        {
            timeBetweenRounds = 1 / (roundsPerMinute / 60);
        }
    }
}
