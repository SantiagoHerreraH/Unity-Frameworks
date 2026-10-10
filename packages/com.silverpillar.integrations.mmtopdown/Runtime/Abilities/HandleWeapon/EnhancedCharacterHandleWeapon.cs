using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using UnityEngine;
using UnityEngine.Events;

namespace SilverPillar.Integrations.MMTopDown
{
    public class EnhancedCharacterHandleWeapon : CharacterHandleWeapon
    {
        [Header("Super Weapon")]

        /// <summary>
        /// The initial super weapon owned by the character.
        /// </summary>
        [Tooltip("The initial super weapon owned by the character")]
        public Weapon InitialSuperWeapon;

        /// <summary>
        /// Whether the character is currently allowed to use the super weapon.
        /// </summary>
        [SerializeField]
        protected bool SuperWeaponAllowed;

        public void AllowSuperWeapon(bool allow)
        {
            SuperWeaponAllowed = allow;
        }


        [Header("Weapon Events")]

        [SerializeField, Tooltip("Triggered when any weapon starts shooting")]
        private UnityEvent m_OnStartShoot;

        [SerializeField, Tooltip("Triggered when any weapon finishes shooting")]
        private UnityEvent m_OnEndShoot;


        [Header("Non Super Weapon Events")]

        [SerializeField]
        private UnityEvent m_OnStartNonSuperShoot;

        [SerializeField]
        private UnityEvent m_OnEndNonSuperShoot;


        [Header("Super Weapon Events")]

        [SerializeField]
        private UnityEvent m_OnStartSuperShoot;

        [SerializeField]
        private UnityEvent m_OnEndSuperShoot;


        /// <summary>
        /// Runtime instance of the super weapon.
        /// </summary>
        [MMReadOnly]
        public Weapon CurrentSuperWeapon;

        /// <summary>
        /// Runtime instance of the normal weapon.
        /// </summary>
        [MMReadOnly]
        public Weapon CurrentNonSuperWeapon;

        
        protected GameInputManager m_GameInputManager;
        private bool m_IsShooting;
        private bool m_IsSuperShooting;
        private bool m_IsNonSuperShooting;

        // Initialization
        protected override void Initialization()
        {
            base.Initialization();
            m_GameInputManager = InputManager.Instance as GameInputManager;
        }

        /// <summary>
        /// Sets up the regular weapon through CharacterHandleWeapon,
        /// then creates the super weapon instance.
        /// </summary>
        public override void Setup()
        {
            base.Setup();

            // The weapon created by the base Setup is our normal weapon.
            if ((CurrentWeapon != null) &&
                (CurrentWeapon != CurrentSuperWeapon))
            {
                CurrentNonSuperWeapon = CurrentWeapon;
            }

            SetupSuperWeapon();

            // Normal weapon should be equipped by default.
            if (CurrentNonSuperWeapon != null)
            {
                EquipRuntimeWeapon(CurrentNonSuperWeapon);
            }
            else if (CurrentSuperWeapon != null)
            {
                EquipRuntimeWeapon(CurrentSuperWeapon);
            }
        }

        /// <summary>
        /// Instantiates and initializes the super weapon.
        /// </summary>
        protected virtual void SetupSuperWeapon()
        {
            if (InitialSuperWeapon == null)
            {
                return;
            }

            // Don't create it twice if Setup gets called again.
            if (CurrentSuperWeapon != null)
            {
                return;
            }

            Weapon previousWeapon = CurrentWeapon;

            // InstantiateWeapon performs all the normal TDE setup:
            // owner, aim, IK, animator parameters, etc.
            InstantiateWeapon(
                InitialSuperWeapon,
                InitialSuperWeapon.WeaponName,
                false);

            CurrentSuperWeapon = CurrentWeapon;

            // Restore the previously equipped normal weapon.
            if (previousWeapon != null)
            {
                EquipRuntimeWeapon(previousWeapon);
            }
        }


        /// <summary>
        /// Processes the standard ability and checks whether
        /// the current shooting session has ended.
        /// </summary>
        public override void ProcessAbility()
        {
            base.ProcessAbility();

            HandleShootSessionEnd();
        }


        /// <summary>
        /// Starts shooting and triggers the corresponding shoot events.
        /// Start events are triggered only once per shooting session.
        /// </summary>
        public override void ShootStart()
        {
            bool canShoot =
                AbilityAuthorized
                && (CurrentWeapon != null)
                && (_condition.CurrentState ==
                    CharacterStates.CharacterConditions.Normal);

            if (canShoot && !m_IsShooting)
            {
                StartShoot();
            }

            base.ShootStart();
        }

        /// <summary>
        /// Marks the beginning of any weapon shooting session.
        /// </summary>
        protected virtual void StartShoot()
        {
            if (m_IsShooting)
            {
                return;
            }

            m_IsShooting = true;

            m_OnStartShoot?.Invoke();

            if (CurrentWeapon == CurrentSuperWeapon)
            {
                StartSuperShoot();
            }
            else if (CurrentWeapon == CurrentNonSuperWeapon)
            {
                StartNonSuperShoot();
            }
        }

        /// <summary>
        /// Marks the end of the current shooting session.
        /// </summary>
        protected virtual void EndShoot()
        {
            if (!m_IsShooting
                && !m_IsSuperShooting
                && !m_IsNonSuperShooting)
            {
                return;
            }

            if (m_IsSuperShooting)
            {
                EndSuperShoot();
            }

            if (m_IsNonSuperShooting)
            {
                EndNonSuperShoot();
            }

            if (m_IsShooting)
            {
                m_IsShooting = false;
                m_OnEndShoot?.Invoke();
            }
        }


        /// <summary>
        /// Marks the beginning of a normal weapon shooting session.
        /// </summary>
        protected virtual void StartNonSuperShoot()
        {
            if (m_IsNonSuperShooting)
            {
                return;
            }

            m_IsNonSuperShooting = true;
            m_OnStartNonSuperShoot?.Invoke();
        }

        /// <summary>
        /// Marks the end of a normal weapon shooting session.
        /// </summary>
        protected virtual void EndNonSuperShoot()
        {
            if (!m_IsNonSuperShooting)
            {
                return;
            }

            m_IsNonSuperShooting = false;
            m_OnEndNonSuperShoot?.Invoke();
        }


        /// <summary>
        /// Marks the beginning of a super weapon shooting session.
        /// </summary>
        protected virtual void StartSuperShoot()
        {
            if (m_IsSuperShooting)
            {
                return;
            }

            m_IsSuperShooting = true;
            m_OnStartSuperShoot?.Invoke();
        }

        /// <summary>
        /// Marks the end of a super weapon shooting session.
        /// </summary>
        protected virtual void EndSuperShoot()
        {
            if (!m_IsSuperShooting)
            {
                return;
            }

            m_IsSuperShooting = false;
            m_OnEndSuperShoot?.Invoke();
        }


        /// <summary>
        /// Checks whether the active shooting session has completely ended.
        /// </summary>
        protected virtual void HandleShootSessionEnd()
        {
            if (!m_IsShooting)
            {
                return;
            }

            if (CurrentWeapon == null)
            {
                EndShoot();
                return;
            }

            // An input is still requesting the weapon to shoot.
            if (IsShootInputActive())
            {
                return;
            }

            // Wait for the weapon itself to completely finish its attack.
            if (CurrentWeapon.WeaponState.CurrentState !=
                Weapon.WeaponStates.WeaponIdle)
            {
                return;
            }

            bool wasSuperShooting = m_IsSuperShooting;

            EndShoot();

            //DELETE THIS -----------------------------------------------------------------------------------------
            // Once the super attack has completely finished,
            // restore the standard weapon.
            if (wasSuperShooting &&
                (CurrentNonSuperWeapon != null))
            {
                //EquipNonSuperWeapon();
            }
        }


        /// <summary>
        /// Gets input and triggers methods based on what's been pressed.
        /// The super weapon is selected when the shoot button is initially
        /// pressed and remains selected for the duration of that attack.
        /// </summary>
        protected override void HandleInput()
        {
            if (!AbilityAuthorized
                || (_condition.CurrentState !=
                    CharacterStates.CharacterConditions.Normal))
            {
                return;
            }

            bool shootButtonDown =
                (_inputManager.ShootButton.State.CurrentState ==
                    MMInput.ButtonStates.ButtonDown);

            bool shootAxisDown =
                (_inputManager.ShootAxis ==
                    MMInput.ButtonStates.ButtonDown);

            bool shootButtonPressed =
                (_inputManager.ShootButton.State.CurrentState ==
                    MMInput.ButtonStates.ButtonPressed);

            bool shootAxisPressed =
                (_inputManager.ShootAxis ==
                    MMInput.ButtonStates.ButtonPressed);

            bool shootButtonUp =
                (_inputManager.ShootButton.State.CurrentState ==
                    MMInput.ButtonStates.ButtonUp);

            bool shootAxisUp =
                (_inputManager.ShootAxis ==
                    MMInput.ButtonStates.ButtonUp);

            bool superInput = m_GameInputManager ?
                (m_GameInputManager.SuperButton.State.CurrentState ==
                    MMInput.ButtonStates.ButtonDown ||
                    m_GameInputManager.SuperButton.State.CurrentState ==
                    MMInput.ButtonStates.ButtonPressed) : false; 

            // ---------------------------------------------------------
            // Weapon selection
            // ---------------------------------------------------------

            // A new ShootButton press decides whether the super or
            // normal weapon performs this attack.
            if (shootButtonDown)
            {
                if (superInput && SuperWeaponAllowed &&
                    (CurrentSuperWeapon != null))
                {
                    EquipSuperWeapon();
                }
                else
                {
                    EquipNonSuperWeapon();
                }
            }
            else if (shootAxisDown)
            {
                // ShootAxis continues to use the normal weapon.
                EquipNonSuperWeapon();
            }


            if (CurrentWeapon == null)
            {
                return;
            }


            bool inputAuthorized = CurrentWeapon.InputAuthorized;


            // ---------------------------------------------------------
            // Original CharacterHandleWeapon input behaviour
            // ---------------------------------------------------------

            if (ForceAlwaysShoot)
            {
                ShootStart();
            }

            if (inputAuthorized &&
                (shootButtonDown || shootAxisDown))
            {
                ShootStart();
            }


            bool buttonPressed =
                shootButtonPressed ||
                shootAxisPressed;


            if (inputAuthorized
                && ContinuousPress
                && (CurrentWeapon.TriggerMode ==
                    Weapon.TriggerModes.Auto)
                && buttonPressed)
            {
                ShootStart();
            }

            if (inputAuthorized
                && ContinuousPress
                && CurrentWeapon.IsAutoComboWeapon
                && buttonPressed)
            {
                ShootStart();
            }


            if (_inputManager.ReloadButton.State.CurrentState ==
                MMInput.ButtonStates.ButtonDown)
            {
                Reload();
            }


            if (inputAuthorized &&
                (shootButtonUp || shootAxisUp))
            {
                ShootStop();

                if (CurrentWeapon != null)
                {
                    CurrentWeapon.WeaponInputReleased();
                }
            }


            if ((CurrentWeapon != null)
                && (CurrentWeapon.WeaponState.CurrentState ==
                    Weapon.WeaponStates.WeaponDelayBetweenUses)
                && (_inputManager.ShootAxis ==
                    MMInput.ButtonStates.Off)
                && (_inputManager.ShootButton.State.CurrentState ==
                    MMInput.ButtonStates.Off)
                && !(UseSecondaryAxisThresholdToShoot
                    && (_inputManager.SecondaryMovement.magnitude >
                        _inputManager.Threshold.magnitude)))
            {
                CurrentWeapon.WeaponInputStop();
            }


            if (inputAuthorized
                && UseSecondaryAxisThresholdToShoot
                && (_inputManager.SecondaryMovement.magnitude >
                    _inputManager.Threshold.magnitude))
            {
                ShootStart();
            }
        }


        /// <summary>
        /// Returns true if any configured input is currently requesting
        /// the weapon to shoot.
        /// </summary>
        protected virtual bool IsShootInputActive()
        {
            if (ForceAlwaysShoot)
            {
                return true;
            }

            if (_inputManager == null)
            {
                return false;
            }

            bool buttonActive =
                (_inputManager.ShootButton.State.CurrentState ==
                    MMInput.ButtonStates.ButtonDown)
                ||
                (_inputManager.ShootButton.State.CurrentState ==
                    MMInput.ButtonStates.ButtonPressed);

            bool axisActive =
                (_inputManager.ShootAxis ==
                    MMInput.ButtonStates.ButtonDown)
                ||
                (_inputManager.ShootAxis ==
                    MMInput.ButtonStates.ButtonPressed);

            bool secondaryAxisActive =
                UseSecondaryAxisThresholdToShoot
                && (_inputManager.SecondaryMovement.magnitude >
                    _inputManager.Threshold.magnitude);

            return buttonActive
                || axisActive
                || secondaryAxisActive;
        }

        /// <summary>
        /// Returns true once no shooting input is active.
        /// </summary>
        protected virtual bool IsShootInputReleased()
        {
            return !IsShootInputActive();
        }


        /// <summary>
        /// Switches CurrentWeapon to the super weapon.
        /// </summary>
        public virtual void EquipSuperWeapon()
        {
            if (!SuperWeaponAllowed)
            {
                return;
            }

            if (CurrentSuperWeapon == null)
            {
                return;
            }

            EquipRuntimeWeapon(CurrentSuperWeapon);
        }

        /// <summary>
        /// Switches CurrentWeapon back to the normal weapon.
        /// </summary>
        public virtual void EquipNonSuperWeapon()
        {
            if (CurrentNonSuperWeapon == null)
            {
                return;
            }

            EquipRuntimeWeapon(CurrentNonSuperWeapon);
        }


        /// <summary>
        /// Switches between already-instantiated weapons without
        /// destroying them.
        /// </summary>
        protected virtual void EquipRuntimeWeapon(Weapon weapon)
        {
            if (weapon == null)
            {
                return;
            }

            if (CurrentWeapon == weapon)
            {
                return;
            }

            Weapon previousWeapon = CurrentWeapon;

            // If we're switching weapon in the middle of an active
            // shooting session, that session is now over.
            if ((previousWeapon != null) && m_IsShooting)
            {
                EndShoot();
            }

            if (previousWeapon != null)
            {
                previousWeapon.TurnWeaponOff();

                if (_weaponAim != null)
                {
                    _weaponAim.RemoveReticle();
                }

                previousWeapon.gameObject.SetActive(false);
            }

            CurrentWeapon = weapon;

            if (!CurrentWeapon.gameObject.activeSelf)
            {
                CurrentWeapon.gameObject.SetActive(true);
            }

            // Refresh the references CharacterHandleWeapon normally
            // updates when a new weapon is instantiated.
            _weaponAim =
                CurrentWeapon.gameObject.MMGetComponentNoAlloc<WeaponAim>();

            HandleWeaponAim();
            HandleWeaponIK();

            HandleWeaponModel(
                CurrentWeapon,
                CurrentWeapon.WeaponID,
                false,
                CurrentWeapon);

            InitializeAnimatorParameters();

            OnWeaponChange?.Invoke();
        }


        /// <summary>
        /// Changes the super weapon using the weapon's own
        /// WeaponName as its ID.
        /// </summary>
        public virtual void ChangeSuperWeapon(Weapon newWeapon)
        {
            ChangeSuperWeapon(
                newWeapon,
                newWeapon != null
                    ? newWeapon.WeaponName
                    : string.Empty,
                false);
        }

        /// <summary>
        /// Changes the character's super weapon while preserving
        /// the normal weapon.
        /// </summary>
        public virtual void ChangeSuperWeapon(
            Weapon newWeapon,
            string weaponID,
            bool combo = false)
        {
            // Changing the currently active super weapon ends its
            // existing shooting session.
            if ((CurrentWeapon == CurrentSuperWeapon) &&
                m_IsShooting)
            {
                EndShoot();
            }

            // Combo changes should operate on the currently
            // equipped super weapon.
            if (combo)
            {
                bool superWasEquipped =
                    (CurrentSuperWeapon != null)
                    && (CurrentWeapon == CurrentSuperWeapon);

                Weapon previouslyEquippedWeapon = CurrentWeapon;

                CurrentWeapon = CurrentSuperWeapon;

                base.ChangeWeapon(
                    newWeapon,
                    weaponID,
                    true);

                CurrentSuperWeapon = CurrentWeapon;

                if (!superWasEquipped)
                {
                    CurrentWeapon = previouslyEquippedWeapon;
                }

                return;
            }


            bool wasSuperWeaponEquipped =
                (CurrentSuperWeapon != null)
                && (CurrentWeapon == CurrentSuperWeapon);

            Weapon normalWeapon = CurrentNonSuperWeapon;

            CurrentWeapon = CurrentSuperWeapon;

            if ((CurrentWeapon != null)
                && !CurrentWeapon.gameObject.activeSelf)
            {
                CurrentWeapon.gameObject.SetActive(true);
            }

            base.ChangeWeapon(
                newWeapon,
                weaponID,
                false);

            CurrentSuperWeapon = CurrentWeapon;


            if (wasSuperWeaponEquipped)
            {
                if (CurrentSuperWeapon != null)
                {
                    EquipRuntimeWeapon(CurrentSuperWeapon);
                }
                else if (normalWeapon != null)
                {
                    EquipRuntimeWeapon(normalWeapon);
                }
            }
            else
            {
                if (CurrentSuperWeapon != null)
                {
                    CurrentSuperWeapon.TurnWeaponOff();
                    CurrentSuperWeapon.gameObject.SetActive(false);
                }

                CurrentWeapon = null;

                if (normalWeapon != null)
                {
                    EquipRuntimeWeapon(normalWeapon);
                }
            }
        }


        /// <summary>
        /// Changes the NORMAL weapon while preserving the super weapon.
        ///
        /// This is important for PickableWeapon and other systems that
        /// use CharacterHandleWeapon.ChangeWeapon().
        /// </summary>
        public override void ChangeWeapon(
            Weapon newWeapon,
            string weaponID,
            bool combo = false)
        {
            // If the currently equipped normal weapon is being used,
            // replacing it ends that shooting session.
            if ((CurrentWeapon == CurrentNonSuperWeapon)
                && m_IsShooting)
            {
                EndShoot();
            }

            // Combo weapons should retain the original TDE behaviour.
            if (combo)
            {
                base.ChangeWeapon(
                    newWeapon,
                    weaponID,
                    true);

                CurrentNonSuperWeapon = CurrentWeapon;
                return;
            }


            bool superWasEquipped =
                (CurrentSuperWeapon != null)
                && (CurrentWeapon == CurrentSuperWeapon);

            Weapon superWeapon = CurrentSuperWeapon;


            // Temporarily make the normal weapon the weapon managed
            // by the base ChangeWeapon implementation.
            CurrentWeapon = CurrentNonSuperWeapon;

            if ((CurrentWeapon != null)
                && !CurrentWeapon.gameObject.activeSelf)
            {
                CurrentWeapon.gameObject.SetActive(true);
            }


            base.ChangeWeapon(
                newWeapon,
                weaponID,
                false);

            CurrentNonSuperWeapon = CurrentWeapon;


            // If the super weapon was being used, restore it.
            if (superWasEquipped &&
                (superWeapon != null))
            {
                EquipRuntimeWeapon(superWeapon);
            }
        }


        /// <summary>
        /// Cleans up both weapon instances when the character dies.
        /// </summary>
        protected override void OnDeath()
        {
            // End whichever shooting session is currently active.
            EndShoot();

            base.OnDeath();

            if (CurrentSuperWeapon != null)
            {
                CurrentSuperWeapon.TurnWeaponOff();
                Destroy(CurrentSuperWeapon.gameObject);
            }

            CurrentSuperWeapon = null;
            CurrentNonSuperWeapon = null;
            CurrentWeapon = null;

            m_IsShooting = false;
            m_IsSuperShooting = false;
            m_IsNonSuperShooting = false;
        }
    }
}