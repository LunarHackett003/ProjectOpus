using Opus;
using System.Collections;
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
    public class GameMode : ONetBehaviour
    {
        public string gameModeDisplayName;
        public string gameModeDescription;

        public static GameMode CurrentGameMode {  get; private set; }
        public float endGameSlowSpeed = 2;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();


            if(CurrentGameMode != null)
            {
                if (IsServer)
                {
                    NetworkObject.Despawn();
                    return;
                }
            }
            CurrentGameMode = this;
        }
        public virtual bool GetWinningTeam(out uint team)
        {
            team = 0;
            return false;
        }

        public virtual IEnumerator EndGameSequence()
        {
            if (IsServer)
            {
                EndGameActions_RPC();
                MatchManager.Instance.GameInProgress.Value = false;
                yield return new WaitForSecondsRealtime(15);
                yield return new WaitForFixedUpdate();
                SessionManager.Instance.CloseConnection();
            }
            yield break;
        }
        [Rpc(SendTo.Everyone)]
        public void EndGameActions_RPC()
        {
            EndGameActions();
        }
        public virtual void EndGameActions()
        {

        }

    }
}
