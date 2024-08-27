using opus.SteamIntegration;
using opus.Weapons;
using UnityEngine;
using UnityEngine.InputSystem;
namespace opus.Gameplay {
    public class PlayerManager : MonoBehaviour
    {
        public ScreenEffectsController sec;
        protected static PlayerManager instance;
        public static PlayerManager Instance { 
            get
            {
                if (!instance)
                {
                    return new GameObject("PlayerManager").AddComponent<PlayerManager>();
                }
                return instance;
            } 
        }

        private void Awake()
        {
            if (instance)
            {
                Destroy(gameObject);
                return;
            }
            
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        [SerializeField] protected GameObject menuCanvas;

        [SerializeField] protected bool paused;
        public bool InGame => !paused && SteamLobbyManager.Instance.InLobby;
        public PlayerCharacter pc;

        public Vector2 lookSpeed;
        public bool invertLookY;

        public float worldFOV, viewFOV, baseWorldFOV, baseViewFOV;
        float _worldFOV = -1, _viewFOV = -1;

        public Vector2 moveInput, lookInput;
        public bool sprintInput, crouchInput, fireInput, aimInput, jumpInput, reloadInput;
        public bool holdCrouch, holdSprint, holdAim;

        bool PlayerAlive => InGame && pc && !pc.Dead.Value;
        private void FixedUpdate()
        {
            if (pc)
            {
                if(_worldFOV != worldFOV)
                {
                    _worldFOV = worldFOV;
                    if (pc.cineCam)
                    {
                        pc.cineCam.Lens.FieldOfView = worldFOV; 
                    }
                }
                if(_viewFOV != viewFOV)
                {
                    _viewFOV = viewFOV;
                        pc.viewmodelCamera.fieldOfView = viewFOV;
                }
            }
        }
        public LayerMask cameraMask;

        public void SetPause(bool input)
        {
            if (SteamLobbyManager.Instance.InLobby)
            {
                paused = input;
            }
            else
            {
                paused = true;
            }

            if (paused)
            {
                fireInput = false;
                moveInput = Vector2.zero;
                lookInput = Vector2.zero;
                jumpInput = false;
                sprintInput = false;
            }

            menuCanvas.SetActive(!InGame);
            Cursor.lockState = InGame ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !InGame;
    }
        public void TogglePause()
        {
            SetPause(!paused);
        }
        #region InputCallbacks
        public void GetFireInput(InputAction.CallbackContext context)
        {
            if (InGame && PlayerAlive)
            {
                fireInput = context.ReadValueAsButton();
            }
            else
                fireInput = false;
        }
        public void GetReloadInput(InputAction.CallbackContext context)
        {
            if(InGame && PlayerAlive)
            {
                reloadInput = context.ReadValueAsButton();
            }
        }
        public void GetMoveInput(InputAction.CallbackContext context)
        {
            if (InGame)
            {
                moveInput = context.ReadValue<Vector2>();
            }
            else
            {
                moveInput = Vector2.zero;
            }
        }
        public void GetLookInput(InputAction.CallbackContext context)
        {
            if (InGame)
            {
                lookInput = context.ReadValue<Vector2>();
            }
            else
            {
                moveInput = Vector2.zero;
            }
        }
        public void GetPauseInput(InputAction.CallbackContext context)
        {
            if (context.performed)
                TogglePause();
        }
        public void GetJumpInput(InputAction.CallbackContext context)
        {
            if (InGame)
            {
                if (context.performed && pc)
                    pc.TryJump();
            }
            else
            {

            }
        }
        public void GetSprintInput(InputAction.CallbackContext context)
        {
            if (InGame)
            {
                //Sprint behaviour can vary between players. Some players want to hold sprint, while some want to toggle sprint.
                if (holdSprint)
                {
                    sprintInput = context.ReadValueAsButton();
                }
                else
                {
                    if (context.performed)
                    {
                        sprintInput = !sprintInput;
                    }
                }

            }
            else
            {
                sprintInput = false;
            }
        }
        public void GetSecondaryInput(InputAction.CallbackContext context)
        {
            if (InGame)
            {
                aimInput= context.ReadValueAsButton();
            }
            else
            {
                aimInput = false;
            }
        }
        public void GetHoldCrouchInput(InputAction.CallbackContext context)
        {
            if (InGame)
            {
                //The same thing applies for crouching. Some players want to hold crouch, some want to toggle crouch.
                //For crouching, however, this has been split between two input methods, since games typically have a binding for each one.
                crouchInput = context.ReadValueAsButton();
            }
            else
            {
                crouchInput = false;
            }
        }
        public void GetToggleCrouchInput(InputAction.CallbackContext context)
        {
            if (InGame)
            {
                if (context.performed)
                {
                    crouchInput = !crouchInput;
                }
            }
            else
            {
                crouchInput = false;
            }
        }

        public void GetEquipmentSwitchInput(InputAction.CallbackContext context)
        {
            if(InGame)
            {
                if (context.performed)
                {
                    Vector2 target = context.ReadValue<Vector2>();
                    float angle = Mathf.Atan2(target.y, target.x);
                    int quadrant = Mathf.RoundToInt(4 * angle / (2*Mathf.PI) + 4) % 4;
                    if(pc.wm.equipment.Count > quadrant)
                    {
                        if (pc.wm.equipmentList[quadrant] is BaseEquipment e)
                        {
                            if (e.CanSelect())
                            {
                                print("valid input, valid gear, selected successfully");
                                pc.wm.SwitchWeaponDirectly(quadrant);
                            }
                            else
                            {
                                print("valid input, valid gear, cannot select gear");
                            }
                        }
                        else
                        {
                            print("Invalid gear");
                        }
                    }
                    else
                    {
                        print("invalid input");
                    }
                }
            }
        }
        internal bool carryInput;
        public void GetCarryInput(InputAction.CallbackContext context)
        {
            carryInput = context.ReadValueAsButton();
        }
        internal bool interactInput;
        public void GetInteractInput(InputAction.CallbackContext context)
        {
            interactInput = context.ReadValueAsButton();
        }
        public void GetMeleeInput(InputAction.CallbackContext context)
        {
            if (InGame && context.performed)
            {
                pc.wm.TryMeleeAttack();
            }
        }
        #endregion
    }
}