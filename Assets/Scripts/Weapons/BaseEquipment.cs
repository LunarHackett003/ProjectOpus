using Unity.Netcode;
using UnityEngine;

namespace Opus
{
    public class BaseEquipment : NetworkBehaviour
    {
        public bool fireInput;
        public bool secondaryInput;

        public WeaponController myController;
    }
}
