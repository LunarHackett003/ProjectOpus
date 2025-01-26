using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Opus
{
    /// <summary>
    /// The gamemode class has little functionality by itself.<br></br>
    /// Other gamemodes will inherit GameMode and do stuff by themselves.
    /// <br></br>In the future, GameMode will also have rulesets that can limit weapons/gadgets, or completely change how parts of the game play.
    /// <br></br>I would also like to look at the possibility of <i>entire different statistic sets,</i> although this may be more effort than it is worth.
    /// <br></br>ADD MORE LATER ON!
    /// </summary>
    public class GameMode : NetworkBehaviour
    {
        public string gameModeDisplayName;
        public string gameModeDescription;

        public static GameMode CurrentGameMode {  get; private set; }

        public List<GameObject> objectives;

    }
}
