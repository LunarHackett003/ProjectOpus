using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Event Sequencer/Sequence Event")]
public abstract class SequenceEventData : ScriptableObject
{
    public GameObject gameObject;
    public abstract void Trigger();
    public bool triggered;
}
[System.Serializable]
public class SequenceEvent
{
    public float time;
    public SequenceEventData eventData;
    public void Trigger()
    {
        if (!eventData.triggered)
            eventData.Trigger();
    }
}