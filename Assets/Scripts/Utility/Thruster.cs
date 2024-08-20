using UnityEngine;

public class Thruster : MonoBehaviour
{
    public bool activated;
    public void Activate()
    {
        activated = true;
    }
    public float force;
    public Vector3 forceDirection;
    public bool useGravity;
    public Rigidbody rb;
    
    private void FixedUpdate()
    {
        if (activated)
        {
            rb.AddForce(force * transform.TransformDirection(forceDirection));
            rb.useGravity = useGravity;
        }
    }
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawRay(transform.position, transform.TransformDirection(force * Time.fixedDeltaTime * forceDirection));
    }

#endif
}
