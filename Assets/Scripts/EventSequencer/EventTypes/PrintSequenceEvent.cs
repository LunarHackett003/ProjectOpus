using System;
using UnityEngine;
[CreateAssetMenu(menuName = "Event Sequencer/Print Event")]
public class PrintSequenceEvent : SequenceEventData
{
    public override void Trigger()
    {
        Debug.Log($"Executed Sequence Event {name} @ {DateTime.Now.TimeOfDay}.", gameObject);
    }
}
