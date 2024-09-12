using System.Collections.Generic;
using UnityEngine;

namespace Opus
{
    [CreateAssetMenu(fileName = "TeamNameSO", menuName = "Scriptable Objects/TeamNameSO")]
    public class TeamNameSO : ScriptableObject
    {
        [System.Serializable]
        public struct TeamData
        {
            public string name;
            public Color color;
            public TeamData(string name = "Default Team", Color color = new())
            {
                this.name = name;
                this.color = color;
            }
        }
        public List<TeamData> teams;
    }
}
