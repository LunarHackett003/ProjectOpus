using System.Collections.Generic;
using UnityEngine;

namespace Opus
{
    [CreateAssetMenu(fileName = "ValidLoadoutItemContainer", menuName = "Scriptable Objects/ValidLoadoutItemContainer")]
    public class ValidLoadoutItemContainer : ScriptableObject
    {
        public List<LoadoutItem> primary, secondary, gadget, special;
    }
}
