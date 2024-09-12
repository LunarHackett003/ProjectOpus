using TMPro;
using UnityEngine;

namespace Opus
{
    public class TeamEntry : MonoBehaviour
    {
        public Transform parent;
        public GameObject teamEntryRoot;
        public UnityEngine.UI.Image teamIcon;
        public UnityEngine.UI.Image teamBackground;
        public TMP_Text teamNameDisplay;

        public ScoreboardEntry[] scoreboardEntries;
    }
}
