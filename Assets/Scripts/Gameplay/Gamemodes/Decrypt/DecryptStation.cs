using Unity.Netcode;
using UnityEngine;

namespace Opus
{
    public class DecryptStation : BaseInteractable
    {
        public NetworkVariable<uint> decryptingTeam = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<bool> isDecrypting = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<bool> isStealing = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public uint stealingTeam = 0;
        protected float currentStealTime;

        public NetworkVariable<float> decryptTime = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);


        public override void InteractStart_RPC(uint clientID = 0)
        {
            base.InteractStart_RPC(clientID);

            if (!isDecrypting.Value || decryptingTeam.Value != PlayerManager.playersByID[clientID].teamIndex.Value)
            {
                TrySteal_RPC(clientID, true);
            }
        }
        public override void InteractEnd_RPC(uint clientID = 0)
        {
            base.InteractEnd_RPC(clientID);

            TrySteal_RPC(clientID, false);
        }


        [Rpc(SendTo.Everyone)]
        public void TrySteal_RPC (uint team = 0, bool state = true)
        {
            if (!IsServer)
            {
            }
            else
            {
                if (!isDecrypting.Value)
                {
                    isDecrypting.Value = true;
                    decryptingTeam.Value = team;
                    return;
                }
                isStealing.Value = state;
            }
            stealingTeam = team;
        }

        bool decryptFlipFlop;
        private void FixedUpdate()
        {


            if (!isDecrypting.Value || GameMode.CurrentGameMode is not DecryptGameMode dgm)
                return;

            if (isStealing.Value)
            {
                currentStealTime += Time.fixedDeltaTime;
            }
            else
            {
                currentStealTime = 0;
            }
            if (IsServer)
            {
                if(currentStealTime > dgm.timeToSteal)
                {
                    isStealing.Value = false;
                    decryptingTeam.Value = stealingTeam;
                }
                decryptFlipFlop = !decryptFlipFlop;
                if(decryptFlipFlop)
                    decryptTime.Value += Time.fixedDeltaTime * 2;

                if(decryptTime.Value > dgm.timeToDecrypt)
                {
                    isDecrypting.Value = false;
                    dgm.DecryptionCompleted(decryptingTeam.Value, NetworkObject);
                }

            }
        }
    }
}
