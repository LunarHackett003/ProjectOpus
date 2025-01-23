using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Opus
{
    public class DamageTestDummy : HealthyEntity
    {

        public ParticleSystem deathParticle, restoreParticle;
        public MeshRenderer[] dummyMeshes;
        public float restoreTime = 3;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            currentHealth.OnValueChanged += HealthUpdated;
        }

        protected override void HealthUpdated(float prev, float curr)
        {
            base.HealthUpdated(prev, curr);
            if(curr >= 0 && prev < 0)
            {
                if(restoreParticle != null)
                    restoreParticle.Play();
                for (int i = 0; i < dummyMeshes.Length; i++)
                {
                    dummyMeshes[i].gameObject.SetActive(true);
                }
            }
            else if(curr <= 0 && prev > 0)
            {
                if(deathParticle != null)
                    deathParticle.Play();
                for (int i = 0; i < dummyMeshes.Length; i++)
                {
                    dummyMeshes[i].gameObject.SetActive(false);
                }
                if (IsServer)
                {
                    StartCoroutine(RestoreDummy());
                }
            }
        }
        
        IEnumerator RestoreDummy()
        {
            yield return new WaitForSeconds(restoreTime);
            currentHealth.Value = MaxHealth;
            yield break;
        }

        public override void ReceiveDamage(float damageIn, ulong sourceClientID, float incomingCritMultiply)
        {
            base.ReceiveDamage(damageIn, sourceClientID, incomingCritMultiply);
        }

        public override void RestoreHealth(float healthIn, ulong sourceClientID)
        {
            base.RestoreHealth(healthIn, sourceClientID);
        }

    }
}
