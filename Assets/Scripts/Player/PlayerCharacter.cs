using Netcode.Extensions;
using opus.Gameplay;
using Steamworks;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using opus.SteamIntegration;

public class PlayerCharacter : NetworkBehaviour
{

    [SerializeField] Rigidbody rb;
    [SerializeField] Transform lookTransform;
    [SerializeField] Transform clientRotationRoot;
    [SerializeField] Transform weaponSwayTransform;

    [SerializeField] Vector3 groundNormal;
    [SerializeField] float groundCheckRadius;
    [SerializeField] Vector3 groundCheckOffset;
    [SerializeField] float groundCheckDistance;
    [SerializeField] LayerMask groundMask;
    public NetworkVariable<Vector2> MoveInput = new(Vector2.zero, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Owner);

    [SerializeField] NetworkTransform nt;
    [SerializeField] bool grounded;

    [SerializeField] ClientNetworkAnimator netAnimator;
    [SerializeField] Animator animator;

    [SerializeField] float aimPitch = 0;

    public float aimPitchOffset;

    public NetworkVariable<float> currentHealth = new(), currentShields = new();

    float healthRegenTime, shieldRegenTime;

    public List<Behaviour> remoteDisableComponents;
    public List<Behaviour> localDisableComponents;
    public float groundDrag;
    public float airDrag;
    private void OnDisable()
    {
        if (IsServer || IsOwner)
        {
            currentHealth.OnValueChanged -= OnHealthChanged;
            currentShields.OnValueChanged -= OnShieldChanged;
            SceneManager.sceneLoaded -= NewSceneLoaded;
        }
    }
    private void NewSceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        print("Scene loaded, player is in game.");

        if (IsOwner)
        {
            Respawn(true);
            rb.isKinematic = false;
        }
        ResetHealth();

    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsOwner)
        {
            remoteDisableComponents.ForEach(x =>
            {
                x.enabled = false;
            });
        }
        if (IsOwner)
        {
            SceneManager.sceneLoaded += NewSceneLoaded;

            localDisableComponents.ForEach(x =>
            {
                x.enabled = false;
            });
            PlayerManager.Instance.pc = this;
        }

        if (IsServer || IsOwner)
        {
            currentHealth.OnValueChanged += OnHealthChanged;
            currentShields.OnValueChanged += OnShieldChanged;
            rb.isKinematic = true;
        }
        rb = GetComponent<Rigidbody>();
    }
    private void FixedUpdate()
    {
        //Everything done beyond this point must ONLY be done by the owner or server
        if (!IsOwner || !IsServer || !GameplayManager.Instance)
        {
            return;
        }
        //Checks if the player hit the ground.
        CheckGround(out RaycastHit hit);
        float dot = Vector3.Dot(hit.normal, Vector3.up);
        grounded = hit.collider && (dot >= 0.5f);

        if (hit.collider)
        {
            groundNormal = hit.normal;
        }
        else
        {
            groundNormal = Vector3.up;
        }

        rb.linearDamping = grounded ? groundDrag : airDrag;
        //Checks if the network transform is client or server auth. If server auth, it uses the server's synchronised move input.
        //This prevents some cheating methods, but also increases latency. If player's network transform is client auth, then it'll use the client's own move input.
        //This is a little more responsive, but can enable cheating or visual disparity between players.
        //Rotation of the player will ALWAYS be done by the client, as networked rotation COUGH COUGH HALO can be quite jarring, and honestly very difficult to play with.
        //When the player attacks, it will be performed by the server, and will use whatever information the server has, to again help mitigate cheating.
        bool moved = false;
        if (IsServer)
        {
            if (nt.IsServerAuthoritative())
            {
                if (MoveInput.Value != Vector2.zero)
                    MovePlayer(MoveInput.Value);
                moved = true;
            }
            float health = currentHealth.Value;
            float shield = currentShields.Value;

            float maxHealth = GameplayManager.BaseHealth * GameplayManager.Instance.healthMultiplier.Value;
            float maxShield = GameplayManager.BaseShields * GameplayManager.Instance.shieldsMultiplier.Value;
            if (GameplayManager.Instance.regenHealth.Value)
            {
                healthRegenTime += Time.fixedDeltaTime;
                //Start regenerating health if we've waited long enough.
                if(healthRegenTime >= GameplayManager.Instance.healthRegenDelay.Value && 
                    health < maxHealth)
                {
                    health += Time.fixedDeltaTime * GameplayManager.Instance.healthRegenPerSec.Value;
                }
            }
            if (GameplayManager.Instance.regenShield.Value)
            {
                shieldRegenTime += Time.fixedDeltaTime;

                if(shieldRegenTime >= GameplayManager.Instance.shieldRegenDelay.Value &&
                    shield < maxShield)
                {
                    shield += Time.fixedDeltaTime * GameplayManager.Instance.shieldRegenPerSec.Value;
                }
            }

            shieldRegenTime += Time.fixedDeltaTime;
            //checks if current health and shields are equal to the potential new values.
            //values are clamped to the max health to prevent HP hacking
            //The clamped values are then applied to the network variable.
            //This is all done by the server. The player cannot write their own health, except for the host.
            if(currentHealth.Value != Mathf.Min(health, maxHealth))
            {
                currentHealth.Value = Mathf.Min(health, maxHealth);
            }
            if(currentShields.Value !=  Mathf.Min(shield, maxShield))
            {
                currentShields.Value = Mathf.Min(shield, maxShield);
            }
        }
        
        if (!moved && IsOwner && !nt.IsServerAuthoritative())
        {
            if (PlayerManager.Instance.moveInput != Vector2.zero)
                MovePlayer(PlayerManager.Instance.moveInput);
        }
        if (animator)
        {
            animator.SetBool("Sprint", PlayerManager.Instance.sprintInput);
            animator.SetBool("Crouch", PlayerManager.Instance.crouchInput);
            animator.SetFloat("Horizontal", PlayerManager.Instance.moveInput.x, 0.1f, Time.deltaTime);
            animator.SetFloat("Vertical", PlayerManager.Instance.moveInput.y, 0.1f, Time.deltaTime);
        }
    }
    bool CheckGround(out RaycastHit hit)
    {
        return Physics.SphereCast(transform.position + groundCheckOffset, groundCheckRadius, Vector3.down, out hit, groundCheckDistance, groundMask);
    }
    void MovePlayer(Vector2 moveInput)
    {
        //Ensures the move input is no bigger than 1 unit in length - the maximum value a joystick or simulated joystick (including WASD) can provide.
        moveInput = Vector2.ClampMagnitude(moveInput, 1);
        if (!grounded)
        {
            moveInput *= GameplayManager.BaseAirControl * GameplayManager.Instance.airControlMultiplier.Value;
        }
        else
        {
            moveInput *= GameplayManager.BaseMoveSpeed * GameplayManager.Instance.moveSpeedMultiplier.Value;
        }
        
        Vector3 forward = Vector3.Cross(groundNormal, -clientRotationRoot.right);
        Vector3 right = Vector3.Cross(groundNormal, clientRotationRoot.forward);
        Debug.DrawRay(transform.position, forward, Color.blue);
        Debug.DrawRay(transform.position, right, Color.red);
        Debug.DrawRay(transform.position, groundNormal, Color.green);
        Vector3 moveVec = (forward * moveInput.y) + (right * moveInput.x);
        rb.AddForce(moveVec, mode: ForceMode.Acceleration);
    }
    private void Update()
    {
        if (!IsOwner)
            return;
        if (nt.IsServerAuthoritative())
        {
            MoveInput.Value = PlayerManager.Instance.moveInput;
        }
        if (PlayerManager.Instance.InGame)
        {
            aimPitch = Mathf.Clamp(aimPitch - (PlayerManager.Instance.lookSpeed.y * PlayerManager.Instance.lookInput.y * (PlayerManager.Instance.invertLookY ? -1 : 1) * Time.deltaTime), -89, 89);
            lookTransform.localRotation = Quaternion.Euler(aimPitch + aimPitchOffset, 0, 0);
            clientRotationRoot.localRotation *= Quaternion.Euler(0, PlayerManager.Instance.lookInput.x * Time.deltaTime * PlayerManager.Instance.lookSpeed.x, 0);
        }
    }
    void OnHealthChanged(float previous, float current)
    {
        if(current <= 0)
        {
            //Kill the player
        }
    }
    void OnShieldChanged(float previous, float current)
    {
        if(current <= 0)
        {
            //Disable the player's shield
        }
    }
    void Respawn(bool firstSpawn)
    {
        if(!GameplayManager.Instance || !SteamLobbyManager.Instance.InLobby)
        {
            return;
        }
        ResetHealth_ServerRPC();
        bool bravoTeam = GameplayManager.Instance.QueryTeam(SteamClient.SteamId);
        MapSceneData msd = FindAnyObjectByType<MapSceneData>();
        if (msd != null)
        {
            Transform spawnpoint = FindAnyObjectByType<MapSceneData>().GetSpawnPoint(bravoTeam, firstSpawn);
            transform.SetPositionAndRotation(spawnpoint.position, spawnpoint.rotation);
        }
    }
    void ResetHealth()
    {
        if (IsServer && GameplayManager.Instance)
        {
            currentHealth.Value = GameplayManager.BaseHealth * GameplayManager.Instance.healthMultiplier.Value;
            currentShields.Value = GameplayManager.BaseShields * GameplayManager.Instance.shieldsMultiplier.Value;
        }
    }
    [ServerRpc]
    void ResetHealth_ServerRPC()
    {
        currentHealth.Value = GameplayManager.BaseHealth * GameplayManager.Instance.healthMultiplier.Value;
        currentShields.Value = GameplayManager.BaseShields * GameplayManager.Instance.shieldsMultiplier.Value;
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireSphere(groundCheckOffset, groundCheckRadius);
        Gizmos.DrawWireSphere(groundCheckOffset + (Vector3.down * groundCheckDistance), groundCheckRadius);
    }
    public void TryJump()
    {
        if (!grounded)
        {
            return;
        }
        if (nt.IsServerAuthoritative())
        {
            Jump_ServerRPC();
        }
        else
        {
            ApplyJumpForce(PlayerManager.Instance.moveInput);
        }
    }
    void ApplyJumpForce(Vector2 moveInput)
    {
        rb.AddForce((transform.rotation * new Vector3(moveInput.x, GameplayManager.BaseJumpHeight, moveInput.y)), ForceMode.Impulse);
    }
    [ServerRpc]
    void Jump_ServerRPC()
    {
        if (IsServer && GameplayManager.Instance)
        {
            ApplyJumpForce(MoveInput.Value);
        }
    }
}
