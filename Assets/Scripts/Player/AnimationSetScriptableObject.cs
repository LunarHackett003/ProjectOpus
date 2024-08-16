using UnityEngine;

[CreateAssetMenu(fileName = "AnimationSetScriptableObject", menuName = "Scriptable Objects/AnimationSetScriptableObject")]
public class AnimationSetScriptableObject : ScriptableObject
{
    public OverridePair[] overrides;
}
[System.Serializable]
public struct OverridePair
{
    public string name;
    public AnimationClip clip;
}