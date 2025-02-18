using Opus;
using Unity.Netcode;
using UnityEngine;

namespace Opus
{
    public class BaseHoverable : ONetBehaviour
    {
        public MeshRenderer grabbedOutline;
        public virtual void HoverOver(bool hovered)
        {
            if(grabbedOutline != null)
                grabbedOutline.enabled = hovered;
        }

    }
}
