using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Opus
{
    public class GrapplingHook : BaseEquipment
    {
        public NetworkVariable<bool> Grappling = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);


        public float grappleThrowTime, grappleThrowSpeed;
        public float grappleReelDirectForce, grappleReelAimDirForce;
        public float grappleReturnSpeed;
        public Transform grappleTransform;
        public LayerMask grappleLayermask;
        bool grappleHit;
        float grappleTime;
        public Vector3 grappleStartPos, grappleTargetPos;
        public float grappleReleaseDistance, grappleCastRadius;
        WaitForFixedUpdate wff = new();
        bool grapplingInteral;
        public override void TrySelect()
        {
            base.TrySelect();
            print("Tried to throw grapple");
            if (!Grappling.Value)
            {
                ThrowGrapple_RPC(myController.pm.Character.headTransform.position, myController.pm.Character.headTransform.forward);
            }
            else
            {
                if (grappleHit)
                {
                    SetGrappling(false);
                }
            }
        }
        void SetGrappling(bool value)
        {
            if (IsServer)
            {
                Grappling.Value = value;
                grapplingInteral = value;
            }
        }
        [Rpc(SendTo.Everyone)]
        public void ThrowGrapple_RPC(Vector3 origin, Vector3 direction)
        {
            StartCoroutine(TryGrapple(origin, direction));
        }
        IEnumerator TryGrapple(Vector3 origin, Vector3 startdirection)
        {
            if (myController)
            {
                print("Controller found, grappling!");
                SetGrappling(true);
                grappleHit = false;
                grappleTime = 0;
            }
            else
            {
                print("No controller, cannot grapple!");
                yield break;
            }
            grappleTargetPos = (grappleThrowSpeed * grappleThrowTime * startdirection) + origin;
            yield return null;
            //myController.PlayGesture("Grapple");
            //Do everything between here...
            while (grappleTime < grappleThrowTime && !grappleHit)
            {
                grappleStartPos = transform.position;
                grappleTime += Time.fixedDeltaTime;
                Vector3 direction = grappleTargetPos - grappleStartPos;
                if (Physics.SphereCast(grappleTransform.position, grappleCastRadius, direction, out RaycastHit hit, grappleThrowSpeed * Time.fixedDeltaTime, grappleLayermask, QueryTriggerInteraction.Ignore))
                {
                    print("Grapple hit!");
                    grappleTargetPos = hit.point;
                    grappleTime = grappleThrowTime;
                    //myController.networkAnimator.SetTrigger("PullGrapple");
                    grappleHit = true;
                }
                grappleTransform.position = Vector3.Lerp(grappleStartPos, grappleTargetPos, Mathf.InverseLerp(0, grappleThrowTime, grappleTime));
                yield return wff;
            }
            if (grappleHit)
            {
                while (Vector3.Distance(grappleTargetPos, myController.transform.position) > grappleReleaseDistance && !myController.pm.jumpInput && Grappling.Value)
                {
                    if (IsOwner)
                    {
                        myController.Controller.rb.AddForce(((grappleTargetPos - myController.transform.position).normalized * grappleReelDirectForce)
                            + (myController.Controller.headTransform.forward * grappleReelAimDirForce), ForceMode.Acceleration);
                        grappleTransform.position = grappleTargetPos;
                    }
                    yield return wff;
                }
            }
            while (grappleTime > 0)
            {
                grappleTime -= Time.fixedDeltaTime * grappleReturnSpeed;
                grappleTransform.position = Vector3.Lerp(transform.position, grappleTargetPos, Mathf.InverseLerp(0, grappleThrowTime, grappleTime));
            }
            grappleTransform.localPosition = Vector3.zero;
            //...And here
            if (IsServer)
                Grappling.Value = false;
            yield break;
        }

        private void OnDrawGizmos()
        {
            if(grappleStartPos != Vector3.zero)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(grappleStartPos, .5f);
            }
            if(grappleTargetPos != Vector3.zero)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(grappleTargetPos, .5f);
            }
        }
    }
}
