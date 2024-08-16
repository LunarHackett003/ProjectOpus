using Netcode.Extensions;
using opus.Gameplay;
using Steamworks;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using opus.SteamIntegration;
using Unity.Cinemachine;
using opus.Weapons;
using UnityEngine.Rendering;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class PlayerCharacter : Damageable
{
    public static readonly HashSet<PlayerCharacter> players = new HashSet<PlayerCharacter>();



    public NetworkVariable<ulong> mySteamID;

    [SerializeField] internal Rigidbody rb;
    [SerializeField] Transform lookTransform;
    [SerializeField] internal Transform clientRotationRoot;
    [SerializeField] Transform weaponSwayTransform;
    [SerializeField] Transform cameraRecoilTransform;
    [SerializeField] Transform weaponRootTransform;
    [SerializeField] Vector3 groundNormal;
    [SerializeField] float groundCheckRadius;
    [SerializeField] Vector3 groundCheckOffset;
    [SerializeField] float groundCheckDistance;
    [SerializeField] LayerMask groundMask;
    public NetworkVariable<Vector2> MoveInput = new(Vector2.zero, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Owner);

    [SerializeField] NetworkTransform nt;
    [SerializeField] bool grounded;

    [SerializeField] internal ClientNetworkAnimator netAnimator;
    [SerializeField] internal Animator animator;

    [SerializeField] float aimPitch = 0;

    public float aimPitchOffset;

    public NetworkVariable<float> currentHealth = new(writePerm: NetworkVariableWritePermission.Server), currentShields = new(writePerm: NetworkVariableWritePermission.Server);

    float healthRegenTime, shieldRegenTime;

    public List<Behaviour> remoteDisableComponents;
    public List<Behaviour> localDisableComponents;
    public float groundDrag;
    public float airDrag;

    public CinemachineCamera cineCam;
    public WeaponManager wm;

    public float minGroundDotProduct;
    public NetworkVariable<bool> crouchInput = new(writePerm: NetworkVariableWritePermission.Owner), sprintInput = new(writePerm: NetworkVariableWritePermission.Owner);

    [SerializeField] Volume flashbangVolume;
    [SerializeField] AnimationCurve flashbangWeightCurve;
    [SerializeField] float flashbangTime;

    NetworkVariable<bool> canRespawn = new(writePerm: NetworkVariableWritePermission.Server);
    public NetworkVariable<float> timeLeftToRespawn = new(writePerm: NetworkVariableWritePermission.Server);

    [SerializeField] Image healthBarImage, shieldBarImage;
    [SerializeField] TMP_Text respawnTimer;
    [SerializeField] GameObject respawnPrompt;
    public NetworkVariable<bool> Dead = new(writePerm: NetworkVariableWritePermission.Server);

    [SerializeField] Renderer[] renderersToDisableOnDeath;
    [SerializeField] UnityEvent deathEvent, respawnEvent;

    [SerializeField] Transform spectatorTransform;

    [SerializeField] Ladder currentLadder;
    [SerializeField] LayerMask ladderGrabMask;
    [SerializeField] float ladderNormalOffset;
    [SerializeField] float ladderGrabDistance, ladderClimbSpeed;
    [SerializeField] Vector3 ladderGrabOffset;
    [SerializeField] Vector3 ladderTargetPosition;
    [SerializeField] Vector3 ladderDismountTopPosition, ladderDismountBottomPosition;

    [SerializeField] float vaultSpeed, vaultForwardCheckDistance;
    [SerializeField] Vector3 vaultForwardCheckOffsetTop, vaultForwardCheckOffsetBottom, vaultDownwardCheckPosition, vaultTargetOffset;
    [SerializeField] AnimationCurve vaultLerp_y, vaultLerp_xz;
    public enum MoveState
    {
        grounded = 0,
        airborne = 1,
        ladder = 2,
        mounted = 3,
        sliding = 4,
        vaulting = 5,
    }
    public NetworkVariable<MoveState> moveState = new(writePerm:NetworkVariableWritePermission.Owner);
    float aimPitchRecoil;
    Vector3 weaponRecoilLinear, weaponRecoilAngular, cameraRecoilLinear, cameraRecoilAngular, 
        vel_weaponRecoilLinear, vel_weaponRecoilAngular, vel_cameraRecoilLinear, vel_cameraRecoilAngular;
    private void OnEnable()
    {
        players.Add(this);
    }

    private void OnDisable()
    {
        if (IsServer || IsOwner)
        {
            currentHealth.OnValueChanged -= OnHealthChanged;
            currentShields.OnValueChanged -= OnShieldChanged;
            SceneManager.sceneLoaded -= NewSceneLoaded;
        }
        players.Remove(this);
    }
    private void NewSceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        print("Scene loaded, player is in game.");

        if (IsOwner)
        {
            Respawn(true);
            rb.isKinematic = false;
        }
        ResetHealth_ServerRPC();
        
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
            mySteamID.Value = SteamClient.SteamId;
        }

        canRespawn.OnValueChanged += OnRespawnChanged;
        timeLeftToRespawn.OnValueChanged += OnRespawnTimeChanged;


        if (IsServer || IsOwner)
        {
            currentHealth.OnValueChanged += OnHealthChanged;
            currentShields.OnValueChanged += OnShieldChanged;
            rb.isKinematic = true;
        }
        rb = GetComponent<Rigidbody>();
    }

    void OnRespawnChanged(bool previous, bool current)
    {
        respawnPrompt.SetActive(current);
    }
    void OnRespawnTimeChanged(float previous, float current)
    {
        if (IsOwner)
        {
            if (!respawnTimer.isActiveAndEnabled)
            {
                respawnTimer.gameObject.SetActive(true);
            }
            if (current != 0)
            {
                respawnTimer.text = $"Respawn in {timeLeftToRespawn.Value}!";
            }
            else
            {
                respawnTimer.text = $"Respawn ready!";
            }
        }
    }

    /// <summary>
    /// Adds a force to counteract the player sliding down a slope. In theory.
    /// </summary>
    void CounteractGravityOnSlope()
    {
        Vector3 vec = -Vector3.ProjectOnPlane(Physics.gravity, groundNormal);
        Debug.DrawRay(transform.position, Physics.gravity, Color.green);
        Debug.DrawRay(transform.position, groundNormal, Color.yellow);
        Debug.DrawRay(transform.position, vec, Color.red);
        rb.AddForce(vec, ForceMode.Acceleration);
    }

    private void FixedUpdate()
    {
        //Everything done beyond this point must ONLY be done by the owner or server
        if (!IsOwner || !IsServer || !GameplayManager.Instance)
        {
            return;
        }
        //Checks if the player hit the ground.

        if (!Dead.Value)
        {
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
        }
        /*Checks if the network transform is client or server auth. If server auth, it uses the server's synchronised move input.
        *This prevents some cheating methods, but also increases latency. If player's network transform is client auth, then it'll use the client's own move input.
        *This is a little more responsive, but can enable cheating or visual disparity between players.
        *Rotation of the player will ALWAYS be done by the client, as networked rotation COUGH COUGH HALO can be quite jarring, and honestly very difficult to play with.
        *When the player attacks, it will be performed by the server, and will use whatever information the server has, to again help mitigate cheating.
        */
        bool moved = false;
        if (IsServer)
        {
            if (Dead.Value)
            {
                if(timeLeftToRespawn.Value > 0)
                {
                    timeLeftToRespawn.Value = Mathf.Max(timeLeftToRespawn.Value - Time.fixedDeltaTime, 0);
                }
                else
                {
                    if(!canRespawn.Value)
                        canRespawn.Value = true;
                }
                return;
            }
            else
            {
                ProcessHealth();
                if (nt.IsServerAuthoritative())
                {
                    CounteractGravityOnSlope();
                    TryMove(MoveInput.Value);
                    moved = true;
                }
            }
        }

        if (!moved && IsOwner && !nt.IsServerAuthoritative())
        {
            if (!Dead.Value)
            {
                CounteractGravityOnSlope();

                TryMove(PlayerManager.Instance.moveInput);
            }
            else
            {
                //Spectator stuff here
            }
        }
        if (Dead.Value)
        {
            MoveSpectator();
        }
        if (animator)
        {
            animator.SetBool("Sprint", PlayerManager.Instance.sprintInput);
            animator.SetBool("Crouch", PlayerManager.Instance.crouchInput);
            animator.SetFloat("Horizontal", PlayerManager.Instance.moveInput.x, 0.1f, Time.deltaTime);
            animator.SetFloat("Vertical", PlayerManager.Instance.moveInput.y, 0.1f, Time.deltaTime);
        
        }
        UpdateRecoil_Fixed();
    }
    
    void UpdateRecoil_Frame()
    {
        if (weaponSwayTransform && wm.GetRecoilProfile != null)
        {
            cameraRecoilTransform.SetLocalPositionAndRotation(Vector3.SmoothDamp(cameraRecoilTransform.localPosition, cameraRecoilLinear, ref vel_cameraRecoilLinear, wm.GetRecoilProfile.camRecoilSmoothness),
                Quaternion.Lerp(cameraRecoilTransform.localRotation, Quaternion.Euler(new Vector3(0, cameraRecoilAngular.y, cameraRecoilAngular.z)), Time.fixedDeltaTime * wm.GetRecoilProfile.camAngularRecoilDecay) );

            weaponSwayTransform.SetLocalPositionAndRotation(Vector3.SmoothDamp(weaponSwayTransform.localPosition, weaponRecoilLinear +
                (wm.weaponBlocked ? wm.equipmentList[wm.equipmentIndex].blockedPosition : Vector3.zero), ref vel_weaponRecoilLinear, wm.GetRecoilProfile.weaponRecoilSmoothness),
                Quaternion.Lerp(weaponSwayTransform.localRotation,  Quaternion.Euler(weaponRecoilAngular + (wm.weaponBlocked ? wm.equipmentList[wm.equipmentIndex].blockedRotation : Vector3.zero)), Time.fixedDeltaTime * wm.GetRecoilProfile.weaponAngularRecoilDecay));
        }
    }
    void UpdateRecoil_Fixed()
    {
        if (weaponSwayTransform && wm.GetRecoilProfile)
        {
            float cameraDecay = wm.GetRecoilProfile.cameraRecoilDecay * Time.fixedDeltaTime;
            float weaponDecay = wm.GetRecoilProfile.weaponRecoilDecay * Time.fixedDeltaTime;

            float oldAimPitch = aimPitchRecoil;
            aimPitchRecoil = Mathf.Lerp(aimPitchRecoil, 0, wm.GetRecoilProfile.aimPitchDecaySpeed * Time.fixedDeltaTime);
            float aimPitchAdditive = oldAimPitch - aimPitchRecoil;
            aimPitch += aimPitchAdditive;

            cameraRecoilLinear = Vector3.Lerp(cameraRecoilLinear, Vector3.zero, cameraDecay);
            cameraRecoilAngular = Vector3.Lerp(cameraRecoilAngular, Vector3.zero, cameraDecay);
            weaponRecoilLinear = Vector3.Lerp(weaponRecoilLinear, Vector3.zero, weaponDecay);
            weaponRecoilAngular = Vector3.Lerp(weaponRecoilAngular , Vector3.zero, weaponDecay);
        }
    }
    Vector3 spectatorPosition;
    Quaternion spectatorRotation;
    void MoveSpectator()
    {
        Vector3 moveInput = new Vector3(PlayerManager.Instance.moveInput.x, 0, PlayerManager.Instance.moveInput.y);
        spectatorRotation = Quaternion.Euler(aimPitch, spectatorTransform.eulerAngles.x + PlayerManager.Instance.lookInput.x * PlayerManager.Instance.lookSpeed.x, 0);
        spectatorPosition += spectatorTransform.forward * moveInput.y + spectatorTransform.right * moveInput.x;
        spectatorTransform.SetPositionAndRotation(spectatorPosition, spectatorRotation);
    }
    void ProcessHealth()
    {
        float health = currentHealth.Value;
        float shield = currentShields.Value;
        if (GameplayManager.Instance.regenHealth.Value)
        {
            //Start regenerating health if we've waited long enough.
            if (healthRegenTime >= GameplayManager.Instance.healthRegenDelay.Value &&
                health < GameplayManager.MaxHealth)
            {
                health += Time.fixedDeltaTime * GameplayManager.Instance.healthRegenPerSec.Value;
            }
            else
            {
                healthRegenTime += Time.fixedDeltaTime;
            }
        }
        if (GameplayManager.Instance.regenShield.Value)
        {

            if (shieldRegenTime >= GameplayManager.Instance.shieldRegenDelay.Value &&
                shield < GameplayManager.MaxShield)
            {
                shield += Time.fixedDeltaTime * GameplayManager.Instance.shieldRegenPerSec.Value;
            }
            else
            {
                shieldRegenTime += Time.fixedDeltaTime;
            }
        }

        //checks if current health and shields are equal to the potential new values.
        //values are clamped to the max health to prevent HP hacking
        //The clamped values are then applied to the network variable.
        //This is all done by the server. The player cannot write their own health, except for the host.
        if (currentHealth.Value != Mathf.Min(health, GameplayManager.MaxHealth))
        {
            currentHealth.Value = Mathf.Min(health, GameplayManager.MaxHealth);
        }
        if (currentShields.Value != Mathf.Min(shield, GameplayManager.MaxShield))
        {
            currentShields.Value = Mathf.Min(shield, GameplayManager.MaxShield);
        }
    }
    bool CheckGround(out RaycastHit hit)
    {
        return Physics.SphereCast(transform.position + groundCheckOffset, groundCheckRadius, Vector3.down, out hit, groundCheckDistance, groundMask);
    }
    void TryMove(Vector2 moveInput)
    {
        moveInput = Vector2.ClampMagnitude(moveInput, 1);
        if (moveState.Value != MoveState.vaulting)
        {

            if (currentLadder)
            {
                moveState.Value = MoveState.ladder;
            }
            else
            {
                if(moveState.Value == MoveState.vaulting)
                {

                }
                else
                {
                    if(CheckGround(out RaycastHit hit))
                    {
                        moveState.Value = MoveState.grounded;
                    }
                    else
                    {
                        moveState.Value = MoveState.airborne;
                    }
                }
            }


            rb.isKinematic = moveState.Value switch
            {
                MoveState.vaulting => true,
                MoveState.grounded => false,
                MoveState.airborne => false,
                MoveState.ladder => true,
                MoveState.mounted => true,
                MoveState.sliding => false,
                _ => false,
            };


            switch (moveState.Value)
            {
                case MoveState.grounded:
                    moveInput *= GameplayManager.BaseMoveSpeed * GameplayManager.Instance.moveSpeedMultiplier.Value;
                    MovePlayer(moveInput);
                    break;
                case MoveState.airborne:
                    moveInput *= GameplayManager.BaseAirControl * GameplayManager.Instance.airControlMultiplier.Value;
                    CheckVault(out RaycastHit hit);
                    MovePlayer(moveInput);
                    break;
                case MoveState.ladder:
                    MovePlayerOnLadder(moveInput);
                    break;
                case MoveState.mounted:
                    //Do nothing, we've mounted something
                    break;
                case MoveState.sliding:

                    break;
                case MoveState.vaulting:
                    
                    break;
                default:
                    break;
            }
        }
    }
    void MovePlayer(Vector2 moveInput)
    {
        Vector3 forward = Vector3.Cross(groundNormal, -clientRotationRoot.right);
        Vector3 right = Vector3.Cross(groundNormal, clientRotationRoot.forward);
        Debug.DrawRay(transform.position, forward, Color.blue);
        Debug.DrawRay(transform.position, right, Color.red);
        Debug.DrawRay(transform.position, groundNormal, Color.green);
        Vector3 moveVec = (forward * moveInput.y) + (right * moveInput.x);
        rb.AddForce(moveVec, mode: ForceMode.Acceleration);

        if(moveInput.y > 0)
        {
            CheckLadder();
        }
    }
    void CheckLadder()
    {
        if(Physics.Raycast(clientRotationRoot.TransformPoint(ladderGrabOffset), clientRotationRoot.forward,out RaycastHit hit, ladderGrabDistance, ladderGrabMask)
            && Vector3.Dot(hit.normal, clientRotationRoot.forward) < -0.75f)
        {
            currentLadder = hit.collider.GetComponentInParent<Ladder>();
            transform.position = (currentLadder.transform.forward * ladderNormalOffset) + new Vector3(currentLadder.transform.position.x, transform.position.y, currentLadder.transform.position.z);
        }
    }
    void MovePlayerOnLadder(Vector2 moveInput)
    {
        transform.position += new Vector3(0, (moveInput.y * ladderClimbSpeed * Time.fixedDeltaTime), 0);

        if(moveInput.y > 0 && Vector3.Distance(transform.position, currentLadder.transform.TransformPoint(currentLadder.endPosition)) <= 0.7f)
        {
            EndLadderClimb(true);
        }
        else if(moveInput.y < 0 && Vector3.Distance(transform.position, currentLadder.transform.TransformPoint(currentLadder.startPosition)) <= 0.7f)
        {
            EndLadderClimb(false);
        }
    }
    void EndLadderClimb(bool top)
    {
        currentLadder = null;
        VaultToPoint(clientRotationRoot.TransformPoint(top ? ladderDismountTopPosition : ladderDismountBottomPosition));
    }
    bool CheckVault(out RaycastHit hit)
    {
        Ray r1 = new(clientRotationRoot.TransformPoint(vaultDownwardCheckPosition), Vector3.down);
        Vector3 pos1 = clientRotationRoot.TransformPoint(vaultForwardCheckOffsetTop), pos2 = clientRotationRoot.TransformPoint(vaultForwardCheckOffsetBottom);
        Debug.DrawLine(pos1, pos2, Color.red, .2f);
        Debug.DrawRay(pos1, clientRotationRoot.forward * vaultForwardCheckDistance, Color.red, .2f);
        Debug.DrawRay(pos2, clientRotationRoot.forward * vaultForwardCheckDistance, Color.red, .2f);
        Debug.DrawRay(r1.origin, r1.direction, Color.yellow, .2f);
        if (Physics.CapsuleCast(pos1, pos2, 0.4f, clientRotationRoot.forward, out hit, vaultForwardCheckDistance, groundMask) &&
            Mathf.Abs(hit.normal.y) < 0.2f && Physics.Raycast(r1, out RaycastHit hit2, 2.5f, groundMask))
        {
            Ray r2 = new(hit2.point + Vector3.down * 0.05f, Vector3.up);
            Debug.DrawRay(r2.origin, r2.direction, Color.cyan, 2f);
            
            if(!Physics.Raycast(r2, out RaycastHit hit3, 2.04f, groundMask))
            {
                print("Vaulting...");
                VaultToPoint(hit2.point + vaultTargetOffset);
                return true;
            }
            return false;
        }
        return false;
    }
    void VaultToPoint(Vector3 point)
    {
        StartCoroutine(VaultToPointCoroutine(point));
    }

    IEnumerator VaultToPointCoroutine(Vector3 point)
    {
        moveState.Value = MoveState.vaulting;
        Vector3 start = transform.position;
        float speed = vaultSpeed / Vector3.Distance(start, point);
        float t = 0;
        while (t < 1)
        {
            t += Time.fixedDeltaTime * speed;
            Vector3 xzlerp = Vector3.Lerp(new Vector3(start.x, 0, start.z), new Vector3(point.x, 0 , point.z), vaultLerp_xz.Evaluate(t));
            Vector3 ylerp = Vector3.Lerp(new Vector3(0, start.y, 0), new Vector3(0, point.y, 0), vaultLerp_y.Evaluate(t));
            transform.position = xzlerp + ylerp;
            yield return new WaitForFixedUpdate();
        }
        moveState.Value = MoveState.airborne;
        rb.isKinematic = false;
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

            crouchInput.Value = PlayerManager.Instance.crouchInput;
            sprintInput.Value = PlayerManager.Instance.sprintInput;
        }
        UpdateRecoil_Frame();
    }
    void OnHealthChanged(float previous, float current)
    {
        healthBarImage.fillAmount = Mathf.InverseLerp(0, GameplayManager.MaxHealth, current);
        if(current <= 0)
        {
            //Kill the player
            if (IsServer)
            {
                if (GameplayManager.Instance.allowRespawns.Value)
                    timeLeftToRespawn.Value = GameplayManager.Instance.respawnTime.Value;
                Dead.Value = true;
            }
            if (IsOwner)
            {
                respawnPrompt.SetActive(true);
                if (GameplayManager.Instance.allowRespawns.Value)
                {

                }
                else
                {
                    respawnTimer.text = "No Respawn!";
                }
            }

            if(renderersToDisableOnDeath.Length > 0)
            {
                foreach (var item in renderersToDisableOnDeath)
                {
                    item.enabled = false;
                }
            }
            deathEvent?.Invoke();
            
        }
    }
    void OnShieldChanged(float previous, float current)
    {
        shieldBarImage.fillAmount = Mathf.InverseLerp(0, GameplayManager.MaxShield, current);
        if(current <= 0)
        {
            //Disable the player's shield
        }
    }
    void Respawn(bool firstSpawn)
    {
        Respawn_ServerRPC(firstSpawn);
    }
    [ServerRpc]
    void Respawn_ServerRPC(bool firstSpawn)
    {

        if (!GameplayManager.Instance || !SteamLobbyManager.Instance.InLobby || (!firstSpawn && !canRespawn.Value))
        {
            return;
        }
        Dead.Value = false;
        ResetHealth_ServerRPC();
        bool bravoTeam = GameplayManager.Instance.IsBravoTeam(SteamClient.SteamId);
        MapSceneData msd = FindAnyObjectByType<MapSceneData>();
        Transform spawnpoint = msd.GetSpawnPoint(bravoTeam, firstSpawn);

        respawnEvent?.Invoke();
        ReceiveRespawn_ClientRPC(spawnpoint.position, spawnpoint.eulerAngles);
        if (wm)
            wm.Respawn_ServerRPC();
    }
    [ClientRpc]
    void ReceiveRespawn_ClientRPC(Vector3 position, Vector3 rotation)
    {
        respawnEvent?.Invoke();
        respawnPrompt.SetActive(false);
        if (IsOwner)
        {
            transform.SetPositionAndRotation(position, Quaternion.Euler(rotation));
            spectatorPosition = Vector3.zero;
            spectatorRotation = Quaternion.identity;
            spectatorTransform.SetLocalPositionAndRotation(spectatorPosition, spectatorRotation);
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
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheckOffset, groundCheckRadius);
        Gizmos.DrawWireSphere(groundCheckOffset + (Vector3.down * groundCheckDistance), groundCheckRadius);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(vaultForwardCheckOffsetTop, 0.4f);
        Gizmos.DrawWireSphere(vaultForwardCheckOffsetBottom, 0.4f);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(vaultDownwardCheckPosition, 0.4f);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(ladderDismountBottomPosition, Vector3.one * 0.2f);
        Gizmos.DrawWireCube(ladderDismountTopPosition, Vector3.one * 0.2f);
        
    }
    public void TryJump()
    {
        if (Dead.Value)
        {
            if(GameplayManager.Instance.allowRespawns.Value && canRespawn.Value && timeLeftToRespawn.Value == 0)
            {
                Respawn(false);
            }
            return;
        }

        if (CheckVault(out RaycastHit hit) && grounded)
        {
            return;
        }

        if (!grounded && !currentLadder)
        {
            return;
        }
        
        currentLadder = null;
        moveState.Value = MoveState.airborne;
        rb.isKinematic = false;
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
        rb.AddForce(clientRotationRoot.rotation * new Vector3(moveInput.x, GameplayManager.BaseJumpSpeed, moveInput.y), ForceMode.Impulse);
        rb.linearVelocity += clientRotationRoot.rotation * new Vector3(0, GameplayManager.BaseJumpSpeed, 0);
    }
    [ServerRpc]
    void Jump_ServerRPC()
    {
        if (IsServer && GameplayManager.Instance)
        {
            ApplyJumpForce(MoveInput.Value);
        }
    }
    float flashtime = 0;
    public void ReceiveFlashbangEffect(float time, float maxTime)
    {
        if(time < flashtime || flashtime == 0 || flashtime == maxTime)
            flashtime = time;
        else
        {
            flashtime -= time / 2;
        }
        flashbangTime = maxTime;
        flashtime = Mathf.Max(flashtime, 0);
        print("received flashbang effect");
        if (!flashbangVolume.enabled)
        {
            StartCoroutine(FlashbangEffect());
        }       
    }
    IEnumerator FlashbangEffect()
    {
        flashbangVolume.enabled = true;
        while (flashtime < flashbangTime)
        {
            flashtime += Time.deltaTime;
            flashbangVolume.weight = flashbangWeightCurve.Evaluate(Mathf.InverseLerp(flashbangTime, 0, flashtime));
            yield return new WaitForEndOfFrame();
        }
        flashbangVolume.enabled = false;
    }

    public override void TakeDamage(float damageAmount)
    {
        currentHealth.Value -= damageAmount;
    }
    internal void ReceiveRecoil()
    {
        cameraRecoilLinear += GenerateRecoilVector(wm.GetRecoilProfile.minCamRecoilLinear, wm.GetRecoilProfile.maxCamRecoilLinear);
        Vector3 recoil = GenerateRecoilVector(wm.GetRecoilProfile.minCamRecoilEuler, wm.GetRecoilProfile.maxCamRecoilEuler);
        cameraRecoilAngular += recoil;
        aimPitchRecoil += recoil.x;
        weaponRecoilLinear += GenerateRecoilVector(wm.GetRecoilProfile.minWeaponRecoilLinear, wm.GetRecoilProfile.maxWeaponRecoilLinear);
        weaponRecoilAngular += GenerateRecoilVector(wm.GetRecoilProfile.minWeaponRecoilEuler, wm.GetRecoilProfile.maxWeaponRecoilEuler);

    }
    Vector3 GenerateRecoilVector(Vector3 min, Vector3 max)
    {
        Vector3 random = Random.insideUnitSphere;
        return new Vector3()
        {
            x = Mathf.Lerp(min.x, max.x, random.x),
            y = Mathf.Lerp(min.y, max.y, random.y),
            z = Mathf.Lerp(min.z, max.z, random.z)
        } * GameplayManager.Instance.recoilMultiplier.Value;
    }

}
