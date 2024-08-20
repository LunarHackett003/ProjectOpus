using opus.Weapons;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class Carriable : NetworkBehaviour
{
    public NetworkVariable<NetworkBehaviourReference> carrier = new NetworkVariable<NetworkBehaviourReference>(writePerm:NetworkVariableWritePermission.Server);
    protected WeaponManager wmCarrier;
    [SerializeField] protected Collider[] colliders = new Collider[0];
    [SerializeField] protected float hardCollisionThreshold = 50;
    [SerializeField] protected float hardCollisionSelfDamage, hardCollisionOtherDamage;
    [SerializeField] Renderer pickupHologram;
    [SerializeField] UnityEvent ThrowEvent;
    [SerializeField] UnityEvent PickupEvent;

    [SerializeField] Rigidbody rb;
    [SerializeField] Quaternion pickupRotation; 
    [SerializeField] Vector3 pickupPosition;
    [SerializeField] float positionDampTime = 0.5f;
    [SerializeField] float rotationSlerpSpeed = 10;
    Vector3 moveDampVelocity;
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        pickupHologram.enabled = false;
        if(rb == null)
            rb = GetComponent<Rigidbody>();
        carrier.OnValueChanged += CarrierChanged;
    }
    public override void OnNetworkDespawn()
    {
        carrier.OnValueChanged -= CarrierChanged;
        if (wmCarrier)
        {
            wmCarrier.currentCarriableRef.Value = null;
        }
        base.OnNetworkDespawn();
    }

    public void OnPickup(NetworkBehaviourReference carrier)
    {
        PickupEvent?.Invoke();
        this.carrier.Value = carrier;
        OnPickup_RPC();
    }
    public void OnThrow(bool thrown)
    {
        if(thrown)
            ThrowEvent?.Invoke();

        rb.AddForce(wmCarrier.fireDirectionReference.forward * wmCarrier.carriableThrowForce, ForceMode.Impulse);
        carrier.Value = null;
        OnThrow_RPC(thrown);
    }
    [Rpc(SendTo.Everyone)]
    void OnThrow_RPC(bool thrown)
    {
        SetToTrigger(false);
        pickupHologram.enabled = false;
    }
    [Rpc(SendTo.Everyone)]
    void OnPickup_RPC()
    {
        SetToTrigger(true);
        pickupHologram.enabled = true;
    }
    void SetToTrigger(bool trigger)
    {
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].isTrigger = trigger;
        }
    }
    public void CarrierChanged(NetworkBehaviourReference previous, NetworkBehaviourReference current)
    {
        rb.useGravity = !current.TryGet(out wmCarrier);
    }
    private void FixedUpdate()
    {
        if (wmCarrier)
        {
            rb.Move(Vector3.SmoothDamp(transform.position, wmCarrier.carryPoint.position + pickupPosition, ref moveDampVelocity, wmCarrier.carriablePositionTime),
                Quaternion.Slerp(transform.rotation, wmCarrier.carryPoint.rotation * pickupRotation, wmCarrier.carriableRotationSpeed * Time.fixedDeltaTime)); 
        }
    }
    protected virtual void OnCollisionEnter(Collision collision)
    {
        print($"COLLISION IMPULSE ON CARRIABLE\n{collision.impulse.sqrMagnitude}");
    }
    protected virtual void OnTriggerEnter(Collider other)
    {
        if (wmCarrier)
        {
            pickupHologram.material.SetFloat(Shader.PropertyToID("_Validity"), 0);
        }
    }
    protected virtual void OnTriggerExit(Collider other)
    {
        if (wmCarrier)
        {
            pickupHologram.material.SetFloat(Shader.PropertyToID("_Validity"), 1);
        }
    }
}
