using Unity.Netcode;
using UnityEngine;

namespace Opus
{
    public class WeaponManager : NetworkBehaviour
    {
        [SerializeField] protected bool primaryInput, secondaryInput;
        public Transform attackOrigin;
    }
}
