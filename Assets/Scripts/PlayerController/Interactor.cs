using UnityEngine;

namespace Opus
{
    public class Interactor : MonoBehaviour
    {
        public Transform interactOrigin;
        public float interactDistance, interactRadiusLineOfSight;
        public LayerMask interactMask;

        public InputCollector collector;
        public PlayerMotor pm;

        public GameObject interactPrompt;
        bool canInteract = false;
        bool lastInteract = false;
        private void FixedUpdate()
        {
            if (Physics.SphereCast(interactOrigin.position, interactRadiusLineOfSight, interactOrigin.forward, out RaycastHit hit, interactDistance, interactMask, QueryTriggerInteraction.Collide))
            {
                Interactable i = hit.collider.GetComponentInParent<Interactable>();
                if (i != null && i.canInteract)
                {
                    canInteract = true;
                    if(collector.interactInput)
                    i.Interact(pm);
                }
                else
                {
                    canInteract = false;
                }
            }
            else { canInteract = false; }

            if(interactPrompt != null && lastInteract != canInteract)
            {
                lastInteract = canInteract;
                interactPrompt.SetActive(canInteract);
            }
        }
    }
}
