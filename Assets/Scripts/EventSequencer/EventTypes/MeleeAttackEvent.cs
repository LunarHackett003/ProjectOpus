using GLTFast.Schema;
using opus.Weapons;
using Unity.Netcode;
using UnityEngine;
[CreateAssetMenu(menuName = "Event Sequencer/Melee Attack Event")]
public class MeleeAttackEvent : SequenceEventData
{
    WeaponManager manager;
    public override void Trigger()
    {
        if (gameObject.TryGetComponent(out manager))
        {
            if (NetworkManager.Singleton.IsServer)
            {
                manager.meleeAttack.MeleeAttack();
            }
            else
            {
                Debug.Log("Cannot use Melee Attack - not server", gameObject);
            }
        }
        else
        {
            Debug.Log("Cannot use Melee Attack - No Weapon Manager");
        }
    }
}
