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
        public float grappleDrag = 3;
        WaitForFixedUpdate wff = new();
        bool grapplingInteral;

        public override bool FireBlocked => base.FireBlocked || Grappling.Value;

        public LineRenderer grappleRope;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            grappleTransform.localScale = Vector3.zero;
        }

        public override void TrySelect()
        {
            base.TrySelect();
            print("Tried to throw grapple");
            if (!FireBlocked)
            {
                ThrowGrapple_RPC(myController.pm.Character.headTransform.position, myController.pm.Character.headTransform.forward);
            }
            else
            {
                if (Grappling.Value && grappleHit)
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
            grappleTransform.localScale = Vector3.one;
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
                            + (myController.Controller.headTransform.forward * grappleReelAimDirForce), ForceMode.Force);
                        grappleTransform.position = grappleTargetPos;
                        myController.Controller.rb.linearDamping = grappleDrag;
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
            grappleHit = false;
            grappleTransform.localScale = Vector3.zero;
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

        public override void OLateUpdate()
        {
            base.OLateUpdate();

            if(grappleRope != null)
            {
                if(grappleRope.enabled != Grappling.Value)
                {
                    grappleRope.enabled = Grappling.Value;
                }
                if (grappleRope.enabled)
                {
                    grappleRope.SetPositions(new Vector3[]{transform.position, grappleTransform.position});
                }
            }
            if (myController != null)
                myController.Controller.specialMovement = grappleHit;
        }
    }
}
