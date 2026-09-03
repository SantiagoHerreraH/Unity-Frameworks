using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class GameInputManager : InputManager
{
    /// a set of input actions to use to read input on
    public GameInputActions InputActions;
    /// the position of the mouse
    public override Vector2 MousePosition => Mouse.current.position.ReadValue();

    protected Vector2 _primaryMovementInput;
    protected Vector2 _secondaryMovementInput;


    public virtual MMInput.IMButton ThrowButton { get; protected set; }
    public virtual MMInput.IMButton GrabButton { get; protected set; }

    protected override void Awake()
    {
        base.Awake();
        InputActions = new GameInputActions();
    }

    /// <summary>
    /// On init we register to all our actions
    /// </summary>
    protected override void Initialization()
    {
        base.Initialization();

        ButtonList.Add(ThrowButton = new MMInput.IMButton(PlayerID, "Throw", ThrowButtonDown, ThrowButtonPressed, ThrowButtonUp));
        ButtonList.Add(GrabButton = new MMInput.IMButton(PlayerID, "Grab", GrabButtonDown, GrabButtonPressed, GrabButtonUp));

        InputActions.PlayerControls.PrimaryMovement.performed += context =>
        {
            _primaryMovementInput = context.ReadValue<Vector2>();
            TestForceDesktop();
        };
        InputActions.PlayerControls.PrimaryMovement.canceled += context =>
        {
            _primaryMovementInput = context.ReadValue<Vector2>();
            TestForceDesktop();
        };
        InputActions.PlayerControls.SecondaryMovement.performed += context => _secondaryMovementInput = context.ReadValue<Vector2>();
        InputActions.PlayerControls.SecondaryMovement.canceled += context => _secondaryMovementInput = context.ReadValue<Vector2>();
        InputActions.PlayerControls.CameraRotation.performed += context => _cameraRotationInput = context.ReadValue<float>();
        InputActions.PlayerControls.CameraRotation.canceled += context => _cameraRotationInput = context.ReadValue<float>();

        InputActions.PlayerControls.Jump.performed += context => { BindButton(context, JumpButton); };
        InputActions.PlayerControls.Run.performed += context => { BindButton(context, RunButton); };
        InputActions.PlayerControls.Dash.performed += context => { BindButton(context, DashButton); };
        InputActions.PlayerControls.Crouch.performed += context => { BindButton(context, CrouchButton); };
        InputActions.PlayerControls.Shoot.performed += context => { BindButton(context, ShootButton); };
        InputActions.PlayerControls.SecondaryShoot.performed += context => { BindButton(context, SecondaryShootButton); };
        InputActions.PlayerControls.Interact.performed += context => { BindButton(context, InteractButton); };
        InputActions.PlayerControls.Reload.performed += context => { BindButton(context, ReloadButton); };
        InputActions.PlayerControls.Pause.performed += context => { BindButton(context, PauseButton); };
        InputActions.PlayerControls.SwitchWeapon.performed += context => { BindButton(context, SwitchWeaponButton); };
        InputActions.PlayerControls.SwitchCharacter.performed += context => { BindButton(context, SwitchCharacterButton); };
        InputActions.PlayerControls.TimeControl.performed += context => { BindButton(context, TimeControlButton); };

        InputActions.PlayerControls.Throw.performed += context => { BindButton(context, ThrowButton); };
        InputActions.PlayerControls.Grab.performed += context => { BindButton(context, GrabButton); };
    }

    protected virtual void TestForceDesktop()
    {
        if ((Mathf.Abs(_primaryMovement.x) > Threshold.x) ||
         (Mathf.Abs(_primaryMovement.y) > Threshold.y))
        {
            _primaryAxisActiveTimestamp = Time.unscaledTime;

            if (IsMobile && ForceDesktopIfPrimaryAxisActive)
            {
                IsMobile = false;
                IsPrimaryAxisActive = true;
                if (GUIManager.HasInstance) { GUIManager.Instance.SetMobileControlsActive(false); }
            }
        }

    }

    protected override void Update()
    {
        TestAutoRevert();
        _primaryMovement = ApplyCameraRotation(_primaryMovementInput);
        _secondaryMovement = ApplyCameraRotation(_secondaryMovementInput);
    }

    protected virtual void TestAutoRevert()
    {
        if (!IsMobile && ForceDesktopIfPrimaryAxisActive && AutoRevertToMobileIfPrimaryAxisInactive)
        {
            if (Time.unscaledTime - _primaryAxisActiveTimestamp > AutoRevertToMobileIfPrimaryAxisInactiveDuration)
            {
                if (GUIManager.HasInstance) { GUIManager.Instance.SetMobileControlsActive(true, MovementControl); }
                IsMobile = true;
                IsPrimaryAxisActive = false;
            }
        }
    }

    /// <summary>
    /// Changes the state of our button based on the input value
    /// </summary>
    /// <param name="context"></param>
    /// <param name="imButton"></param>
    protected virtual void BindButton(InputAction.CallbackContext context, MMInput.IMButton imButton)
    {
        if (!InputDetectionActive)
        {
            return;
        }

        var control = context.control;

        if (control is ButtonControl button)
        {
            if (button.wasPressedThisFrame)
            {
                imButton.State.ChangeState(MMInput.ButtonStates.ButtonDown);
            }
            if (button.wasReleasedThisFrame
                || (imButton.State.CurrentState == MMInput.ButtonStates.ButtonPressed && !button.isPressed))
            {
                imButton.State.ChangeState(MMInput.ButtonStates.ButtonUp);
            }
        }
    }


    public virtual void ThrowButtonDown() { ThrowButton.State.ChangeState(MMInput.ButtonStates.ButtonDown); }
    public virtual void ThrowButtonPressed() { ThrowButton.State.ChangeState(MMInput.ButtonStates.ButtonPressed); }
    public virtual void ThrowButtonUp() { ThrowButton.State.ChangeState(MMInput.ButtonStates.ButtonUp); }

    public virtual void GrabButtonDown() { GrabButton.State.ChangeState(MMInput.ButtonStates.ButtonDown); }
    public virtual void GrabButtonPressed() { GrabButton.State.ChangeState(MMInput.ButtonStates.ButtonPressed); }
    public virtual void GrabButtonUp() { GrabButton.State.ChangeState(MMInput.ButtonStates.ButtonUp); }

    protected override void GetInputButtons()
    {
        // useless now
    }

    public override void SetMovement()
    {
        //do nothing
    }

    public override void SetSecondaryMovement()
    {
        //do nothing
    }

    protected override void SetShootAxis()
    {
        //do nothing
    }

    protected override void SetCameraRotationAxis()
    {
        // do nothing
    }

    protected override void TestPrimaryAxis()
    {
        // do nothing
    }

    /// <summary>
    /// On enable we enable our input actions
    /// </summary>
    protected virtual void OnEnable()
    {
        InputActions.Enable();
    }

    /// <summary>
    /// On disable we disable our input actions
    /// </summary>
    protected virtual void OnDisable()
    {
        InputActions.Disable();
    }
}
