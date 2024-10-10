using FMODUnity;
using System.Collections.Generic;
using UnityEngine;

namespace Opus
{
    public class AudioEventManager : MonoBehaviour
    {
        public static AudioEventManager Instance { get; private set; }
        [System.Serializable]
        public struct AudioEvent
        {
            public string key;
            public EventReference eventReference;
        }
        public AudioEvent[] events;
        public Dictionary<string, EventReference> eventDictionary = new();

        private void Awake()
        {
            if(Instance == null)
            {
                for (int i = 0; i < events.Length; i++)
                {
                    //Convert an array of AudioEvent struct to a dictionary, so we have faster access to the values.
                    eventDictionary.Add(events[i].key, events[i].eventReference);
                }
            }
            else
            {
                Destroy(this);
                return;
            }
        }

        public void PlayAtPosition(Vector3 position, string key)
        {
            if(eventDictionary.ContainsKey(key))
            {
                RuntimeManager.PlayOneShot(eventDictionary[key], position);
            }
            else
            {
                Debug.LogWarning("No event reference, cannot play!");
            }
        }
    }
}
