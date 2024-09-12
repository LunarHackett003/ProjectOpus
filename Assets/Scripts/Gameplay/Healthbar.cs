using UnityEngine;

namespace Opus
{
    public class Healthbar : MonoBehaviour
    {
        public Entity targetedEntity;
        public UnityEngine.UI.Image healthbar;

        public bool pointToCamera;
        public Transform pointTransform;
        private void Start()
        {
            if(targetedEntity != null)
            {
                targetedEntity.currentHealth.OnValueChanged += EntityHealthUpdated;
                EntityHealthUpdated(0, targetedEntity.maxHealth);
            }
        }
        public void SetEntity(Entity entity)
        {
            if(targetedEntity != null)
            {
                targetedEntity.currentHealth.OnValueChanged -= EntityHealthUpdated;
            }
            targetedEntity = entity;
            if(targetedEntity != null)
            {
                targetedEntity.currentHealth.OnValueChanged += EntityHealthUpdated;

            }
        }
        public void EntityHealthUpdated(float previous, float current)
        {
            healthbar.fillAmount = Mathf.InverseLerp(0, targetedEntity.maxHealth, current);
        }
        private void Update()
        {
            if (pointToCamera)
            {
                pointTransform.LookAt(Camera.main.transform, Vector3.up);
            }
        }
    }
}
