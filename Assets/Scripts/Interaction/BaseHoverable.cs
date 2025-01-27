using Opus;
using Unity.Netcode;
using UnityEngine;

namespace Opus
{
    public class BaseHoverable : NetworkBehaviour
    {
        public MeshRenderer grabbedOutline;
        public virtual void HoverOver(bool hovered)
        {
            grabbedOutline.enabled = hovered;
        }

    }
}
