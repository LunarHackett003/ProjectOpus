
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Opus
{
    public class InputCollector : NetworkBehaviour
    {
        public ControlScheme cs;
        public PlayerManager playerManager;

        public Vector2 moveInput, lookInput;
        Vector2 lookDelta, oldLook;
        public Vector2 LookDelta => lookDelta;
        public float lookSpeed;
        public float lookClamp = 89;
        bool jumpInput;
        public bool interactInput;
        public bool scoreboardInput, sprintInput;
        public bool primaryInput, secondaryInput;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsOwner)
            {
                AssignInputEvents();
                playerManager = GetComponent<PlayerManager>();
            }
        }

        void AssignInputEvents()
        {
            cs = new();
            cs.Enable();
            print("registering input callbacks");
            cs.Player.Move.performed += OnMove;
            cs.Player.Move.canceled += OnMove;
            cs.Player.Look.performed += OnLook;
            cs.Player.Look.canceled += OnLook;
            cs.Player.Jump.performed += OnJump;
            cs.Player.Jump.canceled += OnJump;
            cs.Player.Interact.performed += OnInteract;
            cs.Player.Interact.canceled += OnInteract;
            cs.Player.Scoreboard.performed += OnScoreboard;
            cs.Player.Sprint.performed += OnSprint;
            cs.Player.Sprint.canceled += OnSprint;
            cs.Player.Fire.performed += OnFire;
            cs.Player.Fire.canceled += OnFire;
            cs.Player.SecondaryInput.performed += OnSecondaryInput;
            cs.Player.SecondaryInput.canceled += OnSecondaryInput;
            cs.Player.Pause.performed += OnPause;
            cs.Player.Pause.canceled += OnPause;
        }
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            cs.Player.Move.performed -= OnMove;
            cs.Player.Move.canceled -= OnMove;
            cs.Player.Look.performed -= OnLook;
            cs.Player.Look.canceled -= OnLook;
            cs.Player.Jump.performed -= OnJump;
            cs.Player.Jump.canceled -= OnJump;
            cs.Player.Interact.performed -= OnInteract;
            cs.Player.Interact.canceled -= OnInteract;
            cs.Player.Scoreboard.performed -= OnScoreboard;
            cs.Player.Sprint.performed -= OnSprint;
            cs.Player.Sprint.canceled -= OnSprint;
            cs.Player.Fire.performed -= OnFire;
            cs.Player.Fire.canceled -= OnFire;
            cs.Player.SecondaryInput.performed -= OnSecondaryInput;
            cs.Player.SecondaryInput.canceled -= OnSecondaryInput;
            cs.Player.Pause.performed -= OnPause;
            cs.Player.Pause.canceled -= OnPause;
            cs.Disable();
            cs.Dispose();
        }

        private void OnPause(InputAction.CallbackContext obj)
        {
            if (obj.performed)
            {
                print("Pausing or unpausing game");
                PauseMenu.Instance.PauseGame(!PauseMenu.Instance.GamePaused);
            }
        }

        private void OnSecondaryInput(InputAction.CallbackContext obj)
        {
            secondaryInput = obj.ReadValueAsButton();
        }

        private void OnFire(InputAction.CallbackContext obj)
        {
            primaryInput = obj.ReadValueAsButton();
        }

        private void OnSprint(InputAction.CallbackContext obj)
        {
            sprintInput = obj.ReadValueAsButton();
        }

        private void OnScoreboard(InputAction.CallbackContext obj)
        {
            scoreboardInput = !scoreboardInput;
            if(Scoreboard.Instance != null)
            {
                Scoreboard.Instance.ShowScoreboard(scoreboardInput);
            }
        }

        private void Update()
        {
            if(oldLook != lookInput)
            {
                lookDelta = lookInput - oldLook;
                oldLook = lookInput;
            }
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            moveInput = context.ReadValue<Vector2>();
        }
        public void OnLook(InputAction.CallbackContext context)
        {
            oldLook = lookInput;
            lookInput += lookSpeed * Time.deltaTime * context.ReadValue<Vector2>();
            lookInput.y = Mathf.Clamp(lookInput.y, -lookClamp, lookClamp);
        }
        public void OnJump(InputAction.CallbackContext context)
        {
            jumpInput = context.ReadValueAsButton();
        }
        public void OnInteract(InputAction.CallbackContext context)
        {
            interactInput = context.ReadValueAsButton();
        }
        public bool TryConsumeJump()
        {
            bool jumped = jumpInput;
            jumpInput = false;
            return jumped;
        }
    }
}
