using UnityEngine;

namespace Opus
{
    public class Interactable : MonoBehaviour
    {
        public bool canInteract;
        public virtual void Interact(PlayerMotor pm)
        {
            print($"{pm.name} interacted with {gameObject.name}");
        }
    }
}
