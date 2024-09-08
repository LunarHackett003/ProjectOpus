using Unity.Netcode.Components;
using UnityEngine;

namespace Opus
{
    /// <summary>
    /// Functionally <i>just</i> a Client Network Animator. That's all this is, but the Netcode Extensions one already has that name
    /// </summary>
    public class OpusNetworkAnimator : NetworkAnimator
    {
        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }
    }
}
