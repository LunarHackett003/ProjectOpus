using FMODUnity;
using UnityEngine;
[CreateAssetMenu(menuName = "Event Sequencer/Audio Event")]
public class AudioSequenceEvent : SequenceEventData
{

    public EventReference audioRef;
    public override void Trigger()
    {
        RuntimeManager.PlayOneShotAttached(audioRef, gameObject);
    }
}
