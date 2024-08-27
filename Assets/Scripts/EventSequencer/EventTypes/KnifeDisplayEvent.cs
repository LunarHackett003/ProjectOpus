using opus.Weapons;
using UnityEngine;
[CreateAssetMenu(menuName = "Event Sequencer/Knife Display Event")]
public class KnifeDisplayEvent : SequenceEventData
{
    public bool show;
    WeaponManager manager;
    public override void Trigger()
    {
        if(gameObject.TryGetComponent(out manager))
        {
            if (manager.IsServer)
            {
                manager.meleeAttack.DisplayWeapon_RPC(show);
            }
        }
    }
}
