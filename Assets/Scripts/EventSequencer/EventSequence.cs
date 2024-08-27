using opus.utility;
using UnityEngine;

[System.Serializable]
public class EventSequence
{
    public bool IsCanceled { get; private set; } = false;
    public bool IsCompleted { get; private set; } = true;
    
    public SequenceEvent[] events;
    SequenceEventData[] runtimeEvents;
    public delegate void OnLastEventTriggered();
    public OnLastEventTriggered onLastEventTriggered;
    [SerializeField] float currentTime;
    public void Initialise(GameObject gameObject)
    {
        runtimeEvents = new SequenceEventData[events.Length];
        for (int i = 0; i < events.Length; i++)
        {
            if (events[i] == null)
                continue;
            runtimeEvents[i] = events[i].eventData.Clone();
            runtimeEvents[i].gameObject = gameObject;
        }
        Reset();
        EventSequenceRunner.sequences.Add(this);
    }

    public void Reset()
    {
        currentTime = 0;
        IsCompleted = false;
        IsCanceled = false;
    }
    public void Advance(float deltaTime)
    {
        for (int i = 0; i < events.Length; i++)
        {
            if (events[i] == null)
                continue;
            var e = events[i];
            if (e.time < currentTime)
                continue;

            if(e.time >= currentTime &&  e.time <= currentTime + deltaTime)
            {
                runtimeEvents[i].Trigger();
                if(i == events.Length - 1)
                {
                    onLastEventTriggered();
                    IsCompleted = true;
                    CleanUp();
                }
            }

            
        }
        currentTime += deltaTime;
    }
    public void Cancel()
    {
        IsCanceled = true;
        CleanUp();
    }
    void CleanUp()
    {
        for (int i = 0; i < runtimeEvents.Length; i++)
        {
            if (runtimeEvents[i] != null)
            {
                Object.Destroy(runtimeEvents[i]);
            }
        }
    }
}
