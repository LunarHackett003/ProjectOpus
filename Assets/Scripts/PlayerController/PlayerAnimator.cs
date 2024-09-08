using Netcode.Extensions;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Opus
{
    public class PlayerAnimator : NetworkBehaviour
    {
        public Rigidbody rb;
        public InputCollector ic;
        public PlayerMotor pm;
        public Animator animator;
        public NetworkAnimator networkAnimator;


        int horizontalMoveID, verticalMoveID, jumpTriggerID, landTriggerID = -1;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsOwner)
            {
                rb = rb != null ? rb : GetComponentInParent<Rigidbody>();
                ic = ic != null ? ic : GetComponentInParent<InputCollector>();
                pm = pm != null ? pm : GetComponentInParent<PlayerMotor>();

                horizontalMoveID = Animator.StringToHash("Horizontal");
                verticalMoveID = Animator.StringToHash("Vertical");
                jumpTriggerID = Animator.StringToHash("Jump");
                landTriggerID = Animator.StringToHash("Land");
            }
        }

        private void FixedUpdate()
        {
            //Rigidbody or Input Collector is not assigned, and we therefore probably shouldn't be doing stuff with this character.
            //If we don't own this player character, we'll also ignore this too.
            if (!IsOwner || rb == null || ic == null || pm == null) return;
            animator.SetFloat(horizontalMoveID, pm.movementVector.x);
            animator.SetFloat(verticalMoveID, pm.movementVector.y);
        }
        public void JumpTrigger()
        {
            animator.SetTrigger(jumpTriggerID);
        }
        public void LandTrigger()
        {
            animator.SetTrigger(landTriggerID);
        }
    }
}
