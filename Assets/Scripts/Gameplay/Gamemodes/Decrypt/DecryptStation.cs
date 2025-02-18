using Unity.Collections;
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

        public ObjectiveUI objectiveUI;


        public override bool CanInteract(ulong clientID)
        {
            return (!isDecrypting.Value) || (decryptingTeam.Value != MatchManager.Instance.clientsOnTeams.Value[clientID]);
        }


        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if(objectiveUI != null)
            {
                if (IsServer && GameMode.CurrentGameMode is DecryptGameMode dgm)
                {
                    objectiveUI.dynamicText.Value = objectiveUI.useLetter ? ObjectiveUI.alphabet[(dgm.objectiveIndex - 1) % 26].ToString() : dgm.objectiveIndex.ToString();
                }
            }
            isDecrypting.OnValueChanged += ObjectiveStateChanged;
            isStealing.OnValueChanged += ObjectiveStateChanged;

            ObjectiveStateChanged(false, isStealing.Value);

        }

        protected override void InteractStart(ulong clientID = 0)
        {
            base.InteractStart(clientID);
            uint teamindex = PlayerManager.playersByID[clientID].teamIndex.Value;
            if (!isDecrypting.Value || decryptingTeam.Value != teamindex)
            {
                TrySteal_RPC(teamindex, true);
            }
        }
        protected override void InteractEnd(ulong clientID = 0)
        {
            base.InteractEnd(clientID);
            TrySteal_RPC(PlayerManager.playersByID[clientID].teamIndex.Value, false);
        }

        void ObjectiveStateChanged(bool previous, bool current)
        {
            if (objectiveUI != null)
            {
                objectiveUI.progressImage.enabled = isDecrypting.Value;
                if (!isStealing.Value)
                {
                    objectiveUI.progressImage.color = PlayerSettings.Instance.teamColours[decryptingTeam.Value];
                }

                if (isDecrypting.Value)
                {
                    interactText = "E - Steal!";
                }
                else
                {
                    interactText = "E - Start Decrpytion";
                }
                holdInteract = isDecrypting.Value;
            }
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

        [SerializeField] bool decryptFlipFlop;
        public override void OFixedUpdate()
        {

            if (!isDecrypting.Value || GameMode.CurrentGameMode is not DecryptGameMode dgm)
            {
                return;
            }

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
                    dgm.DecryptionCompleted(decryptingTeam.Value, this);
                }
            }

            if (objectiveUI != null)
            {
                if (isStealing.Value)
                {
                    objectiveUI.progressImage.color = Color.Lerp(PlayerSettings.Instance.teamColours[decryptingTeam.Value], PlayerSettings.Instance.teamColours[stealingTeam], Mathf.Sin(currentStealTime * 5));
                }
                if (isDecrypting.Value)
                {
                    objectiveUI.progressImage.fillAmount = Mathf.InverseLerp(0, dgm.timeToDecrypt, decryptTime.Value);
                }
            }
        }
    }
}
