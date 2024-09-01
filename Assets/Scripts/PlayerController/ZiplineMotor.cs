using System.Collections;
using TMPro.EditorUtilities;
using UnityEngine;

namespace Opus
{
    public class ZiplineMotor : MonoBehaviour
    {
        public float ziplineSpeed;
        public Zipline currentZipline;
        public PlayerMotor pm;

        public bool inForwardDirection;
        Vector3 currentZiplinePosition;
        public float currentZiplineLerp;
        float zipPerStep;
        public float ziplineLerpTowardTime;
        bool zipping;
        float ziptime = 0;
        public float detachForce = 3;
        public Vector3 ziplineOffset;
        public void AttachToZipline(Zipline zipline)
        {
            currentZipline = zipline;
            StartCoroutine(ZiplineLerp());
        }
        IEnumerator ZiplineLerp()
        {
            float t = 0;
            Vector3 startPosition = pm.rb.position;
            ziptime = 0;
            currentZiplineLerp = MathFunctions.GetNearestPoint(currentZipline.start.position, currentZipline.end.position, transform.position);
            currentZiplinePosition = Vector3.Lerp(currentZipline.start.position, currentZipline.end.position, currentZiplineLerp) + ziplineOffset;
            while (t < ziplineLerpTowardTime)
            {
                pm.transform.position = Vector3.Lerp(startPosition, currentZiplinePosition, Mathf.InverseLerp(0, ziplineLerpTowardTime, t));
                t += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
            inForwardDirection = Vector3.Dot(pm.head.forward, currentZipline.forwardDirection) >= 0f;
            zipping = true;
            zipPerStep = ziplineSpeed / currentZipline.distance * Time.fixedDeltaTime;
            yield break;
        }
        private void FixedUpdate()
        {
            if (currentZipline == null || !zipping)
                return;

            currentZiplineLerp = Mathf.Clamp01(currentZiplineLerp + zipPerStep * (inForwardDirection ? 1 : -1));
            currentZiplinePosition = Vector3.Lerp(currentZipline.start.position, currentZipline.end.position, currentZiplineLerp);
            pm.transform.position = currentZiplinePosition + ziplineOffset;
            if(Physics.CapsuleCast(pm.transform.position + Vector3.up * .8f, pm.transform.position - Vector3.up * .8f, 0.4f, currentZipline.forwardDirection, ziplineSpeed * Time.fixedDeltaTime))
            {
                Detach();
            }
            if (ziptime != 0 && (currentZiplineLerp == 0 || currentZiplineLerp == 1))
            {
                Detach();
            }
            ziptime += Time.fixedDeltaTime;
        }
        public void Detach()
        {
            zipping = false;
            Vector3 velocity = inForwardDirection ? currentZipline.start.forward : currentZipline.end.forward;
            currentZipline = null;
            pm.moveState = PlayerMotor.MovementState.none;
            pm.rb.isKinematic = false;
            pm.rb.AddForce(velocity * detachForce, ForceMode.Impulse);
        }
    }
}
