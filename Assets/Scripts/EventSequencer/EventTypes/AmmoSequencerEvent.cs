using UnityEngine;
[CreateAssetMenu(menuName = "Event Sequencer/Ammo Event")]
public class AmmoSequencerEvent : SequenceEventData
{
    BaseWeapon w;
    public int ammo;
    public bool setAmmo;
    
    private void Awake()
    {
        if (gameObject)
        {
            w = gameObject.GetComponent<BaseWeapon>();
        }
    }
    public override void Trigger()
    {
        if (gameObject.TryGetComponent(out w))
        {
            if (w.NetworkManager.IsServer || w.NetworkManager.IsHost)
            {
                Debug.Log("RELOADED VIA SEQUENCER", gameObject);
                if (setAmmo)
                {
                    w.currentAmmo.Value = ammo;
                }
                else
                {
                    w.currentAmmo.Value += ammo;
                }
            }
            else
            {
                Debug.Log("CANNOT RELOAD VIA SEQUENCER\nREASON: NOT SERVER", gameObject);
            }
        }
        else
        {
            Debug.Log("CANNOT RELOAD VIA SEQUENCER\nWEAPON NOT FOUND", gameObject);
        }
    }
}
