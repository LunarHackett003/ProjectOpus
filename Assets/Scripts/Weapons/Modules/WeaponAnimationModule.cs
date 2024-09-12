using System.Collections.Generic;
using UnityEngine;

namespace Opus
{
    [CreateAssetMenu(fileName = "WeaponAnimationModule", menuName = "Scriptable Objects/WeaponAnimationModule")]
    public class WeaponAnimationModule : ScriptableObject
    {
        public AttackAnimationTiming attackTiming;
        public AnimationOverrideSet[] animationOverrides;

        public int attackAnimationCount;
    }

    public enum AttackAnimationTiming
    {
        none = 0,
        beforeAttack = 1,
        afterAttack = 2,
    }
    [System.Serializable]
    public struct AnimationOverrideSet
    {
        public string name;
        public AnimationClip clip;
    }
    public class AnimationClipOverrides : List<KeyValuePair<AnimationClip, AnimationClip>>
    {
        public AnimationClipOverrides(int capacity) : base(capacity) { }

        public AnimationClip this[string name]
        {
            get { return Find(x => x.Key.name.Equals(name)).Value; }
            set
            {
                int index = FindIndex(x => x.Key.name.Equals(name));
                if (index != -1)
                    this[index] = new KeyValuePair<AnimationClip, AnimationClip>(this[index].Key, value);
            }
        }
    }
}
