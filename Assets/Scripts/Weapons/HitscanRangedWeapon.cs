using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Pool;

namespace Opus
{
    public class HitscanRangedWeapon : RangedWeapon
    {

        public struct Tracer
        {
            public Vector3 start, end;
            public TrailRenderer t;
            public float time, speed;
        }

        #region TracerJob
        public struct TracerJob : IJobParallelForTransform
        {
            public NativeArray<Vector3> start, end;
            public NativeArray<float> time;

            public void Execute(int index, TransformAccess transform)
            {
                var pos = Vector3.Lerp(start[index], end[index], time[index]);
                transform.position = pos;
            }
        }


        #endregion
        #region Pooling
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
        #endregion Pooling
        List<Tracer> tracers = new();
        RaycastHit[] workingRaycastHits;

        public float tracerSpeed;
        public float tracerRemovalTime;
        public GameObject tracerPrefab;


        TransformAccessArray accessArray;
        NativeArray<Vector3> startArr, endArr;
        NativeArray<float> timeArr;
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            TracerPool?.Clear();
        }
        protected override void FixedUpdate()
        {
            UpdateTracers();

            base.FixedUpdate();
        }



        void UpdateTracers()
        {
            if (tracers.Count > 0)
            {
                for (int i = 0; i < tracers.Count; i++)
                {
                    Tracer x = tracers[i];
                    x.time += x.speed * Time.fixedDeltaTime;
                    x.t.emitting = x.time < tracerRemovalTime;
                    if (x.time >= tracerRemovalTime)
                    {
                        TracerPool.Release(x.t);
                        x.t.emitting = true;
                    }
                    tracers[i] = x;
                }
                tracers.RemoveAll(x => x.time >= tracerRemovalTime);
                accessArray = new(tracers.Select(x => x.t.transform).ToArray());
                startArr = new(tracers.Select(x => x.start).ToArray(), Allocator.TempJob);
                endArr = new(tracers.Select(x => x.end).ToArray(), Allocator.TempJob);
                timeArr = new(tracers.Select(x => x.time).ToArray(), Allocator.TempJob);

                var traceJob = new TracerJob()
                {
                    start = startArr,
                    end = endArr,
                    time = timeArr
                };

                JobHandle traceJobHandle = traceJob.Schedule(accessArray);

                traceJobHandle.Complete();


                accessArray.Dispose();
                startArr.Dispose();
                endArr.Dispose();
                timeArr.Dispose();
            }   
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if(accessArray.isCreated)
                accessArray.Dispose();
            if(startArr.IsCreated)
                startArr.Dispose();
            if (endArr.IsCreated)
                endArr.Dispose();
            if (timeArr.IsCreated)
                timeArr.Dispose();
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
        protected override void ServerFireLogic(Vector3 direction, Vector3 origin)
        {
            base.ServerFireLogic(direction, origin);

            Vector3[] hitEndPoints = new Vector3[fireIterations];
            for (int f = 0; f < fireIterations; f++)
            {
                workingRaycastHits = Physics.RaycastAll(origin,
                    direction, maxRange, damageLayermask, QueryTriggerInteraction.Ignore);
                if (workingRaycastHits.Length > 0)
                {
                    float closestHitDistance = maxRange + 1;
                    int closestHitIndex = -1;
                    for (int i = 0; i < workingRaycastHits.Length; i++)
                    {
                        RaycastHit hit = workingRaycastHits[i];
                        if (hit.rigidbody != null)
                        {
                            if (hit.rigidbody.transform == myController.transform)
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

                    if (closestHitIndex != -1)
                    {
                        RaycastHit hit = workingRaycastHits[closestHitIndex];
                        if (hit.collider.TryGetComponent(out Hitbox box))
                        {
                            float damage = Mathf.Lerp(maxRangeDamage, minRangeDamage, damageFalloff.Evaluate(Mathf.Clamp01(Mathf.InverseLerp(minRange, maxRange, hit.distance))));
                            box.ReceiveDamage(damage, OwnerClientId, critMultiplier, DamageType.Regular);
                        }
                        hitEndPoints[f] = hit.point;
                    }
                    else
                    {
                        hitEndPoints[f] = origin +
                            (direction * maxRange);
                    }
                }
                else
                {
                    hitEndPoints[f] = origin +
                        (direction * maxRange);
                }
            }
            SendTracer_RPC(hitEndPoints);
        }

    }
}
