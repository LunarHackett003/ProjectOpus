using UnityEngine;

namespace Opus
{
    public class Zipline : Interactable
    {
        public Transform start, end;
        public Transform ziplineRenderer;
        public BoxCollider interactBox;
        public float width = 0.1f;
        public Vector3 forwardDirection;
        public float distance;

        public override bool RequireLineOfSight => false;

        [ContextMenu("Set Up Zipline")]
        public void SetUpZipline()
        {
            if(interactBox == null)
            {
                var g = new GameObject("InteractBox_Zipline");
                g.transform.SetParent(transform, false);
                interactBox = g.AddComponent<BoxCollider>();
            }

            forwardDirection = end.position - start.position;
            distance = forwardDirection.magnitude;
            forwardDirection.Normalize();
            interactBox.size = new(width, width, distance);

            interactBox.transform.forward = forwardDirection;
            interactBox.transform.position = Vector3.Lerp(start.position, end.position, 0.5f);

            ziplineRenderer.up = forwardDirection;
            ziplineRenderer.position = interactBox.transform.position;
            ziplineRenderer.localScale = new(width, distance / 2, width);
        }
        private void Start()
        {
            forwardDirection.Normalize();
        }
        public override void Interact(PlayerMotor pm)
        {
            base.Interact(pm);
            if (pm.zipMotor && pm.zipMotor.currentZipline != this)
            {
                pm.zipMotor.AttachToZipline(this);
            }
        }
    }
}
