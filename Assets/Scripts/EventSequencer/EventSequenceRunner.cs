using System.Collections.Generic;
using UnityEngine;

public class EventSequenceRunner : MonoBehaviour
{
    public static EventSequenceRunner Instance { get; private set;}
    public static List<EventSequence> sequences = new();
    public int SequencesRunning;
    private void Awake()
    {
        if(Instance != null)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        SequencesRunning = sequences.Count;
        for (int i = sequences.Count - 1; i >= 0; i--)
        {
            if (sequences[i] == null)
                continue;
            var s = sequences[i];
            if (s.IsCanceled)
            {
                sequences.RemoveAt(i);
                continue;
            }
            s.Advance(Time.deltaTime);
            if (s.IsCompleted)
            {
                sequences.RemoveAt(i);
            }
        }
    }
}
