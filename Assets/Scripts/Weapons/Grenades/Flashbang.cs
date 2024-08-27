using opus.Gameplay;
using Unity.Netcode;
using UnityEngine;

public class Flashbang : Grenade
{
    [SerializeField] protected float flashbangMaxRange;
    [SerializeField] protected float maxFlashbangTime;
    [SerializeField] protected float flashViewDotThreshold;
    [SerializeField] protected LayerMask layermask;
    [SerializeField] protected float viewDirectionModifier;

    public void FlashExplode()
    {

        print("Flashbang exploded!");
        //var planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
        //var point = transform.position;
        //foreach (var item in planes)
        //{
        //if (item.GetDistanceToPoint(point) > 0)
        //{
        //Ray r = new(Camera.main.transform.position, transform.position - Camera.main.transform.position);

        //if (Physics.Raycast(r, out RaycastHit hit) && hit.rigidbody.CompareTag("Player"))
        //{
        ////Flash Time is to fake how long the player has already been flashed for.
        ////This gives 
        //float flashTime = 


        //FindAnyObjectByType<PlayerManager>().pc.ReceiveFlashbangEffect();
        //print("Blinded??? WTF???");
        //}
        //}
        //}
        Vector3 dir = (Camera.main.transform.position - transform.position).normalized;
        float viewDot = Vector3.Dot(dir, Camera.main.transform.forward);
        Debug.DrawRay(transform.position, dir, Color.red, 5);
        if(viewDot <= flashViewDotThreshold)
        {
            if (Physics.Raycast(transform.position, dir, out RaycastHit hit, flashbangMaxRange, layermask ))
            {
                if (hit.rigidbody && hit.rigidbody.CompareTag("Player"))
                {
                    float flashTime = Mathf.Lerp(0, maxFlashbangTime, Mathf.InverseLerp(0, flashbangMaxRange, hit.distance) + (((viewDot * 0.5f) + 0.5f)) * viewDirectionModifier);
                    //PlayerManager.Instance.pc.ReceiveFlashbangEffect(flashTime, maxFlashbangTime);
                }
            }
        }

    }
}
