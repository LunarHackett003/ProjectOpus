using System.Collections.Generic;
using UnityEngine;

namespace Opus
{
    public class LoadoutManager : MonoBehaviour
    {
        public ValidLoadoutItemContainer ValidLoadoutItemContainer;
        public int primaryIndex = -1, secondaryIndex = -1, gadget1Index = -1, gadget2Index = -1, specialIndex = -1;
        public delegate void OnLoadoutUpdated();
        public OnLoadoutUpdated onLoadoutUpdated;
        public static LoadoutManager Instance { get; private set; }
        private void Awake()
        {
            Instance = this;
        }
        public void UpdateLoadoutNumbers()
        {
            onLoadoutUpdated?.Invoke();
        }
    }
}
