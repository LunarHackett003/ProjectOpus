using Netcode.Extensions;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Opus
{
    public class BaseEquipment : NetworkBehaviour
    {
        public bool fireInput;
        public bool secondaryInput;

        public SwayContainerSO swayContainer;

        public WeaponController myController;

        public CharacterRenderable cr;

        public ClientNetworkAnimator netAnimator;

        public AnimatorCustomParamProxy acpp;
    }
}
