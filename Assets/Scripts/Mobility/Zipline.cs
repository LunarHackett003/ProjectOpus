using UnityEngine;

namespace Opus
{
    public class Zipline : Interactable
    {
        public Transform start, end;
        public LineRenderer ziplineRenderer;
        public BoxCollider interactBox;
        public float width = 0.1f;
        public Vector3 forwardDirection;
        public float distance;
        [ContextMenu("Set Up Zipline")]
        public void SetUpZipline()
        {
            if(ziplineRenderer == null)
            {
                ziplineRenderer = gameObject.AddComponent<LineRenderer>();
            }
            if(interactBox == null)
            {
                var g = new GameObject("InteractBox_Zipline");
                g.transform.SetParent(transform, false);
                interactBox = g.AddComponent<BoxCollider>();
            }

            interactBox.transform.forward = forwardDirection;
            interactBox.transform.position = Vector3.Lerp(start.position, end.position, 0.5f);
            forwardDirection = end.position - start.position;
            distance = forwardDirection.magnitude;
            interactBox.size = new(width, width, distance);
            ziplineRenderer.useWorldSpace = true;
            ziplineRenderer.widthMultiplier = width;
            ziplineRenderer.SetPosition(0, start.position);
            ziplineRenderer.SetPosition(1, end.position);
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
