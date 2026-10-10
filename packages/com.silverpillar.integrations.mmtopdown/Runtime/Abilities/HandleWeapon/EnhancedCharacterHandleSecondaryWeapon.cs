using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using UnityEngine;

namespace SilverPillar.Integrations.MMTopDown
{
    public class EnhancedCharacterHandleSecondaryWeapon : EnhancedCharacterHandleWeapon
    {
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
                (_inputManager.SecondaryShootButton.State.CurrentState == MMInput.ButtonStates.ButtonDown);

            bool shootAxisDown =
                (_inputManager.SecondaryShootAxis == MMInput.ButtonStates.ButtonDown);

            bool shootButtonPressed =
                (_inputManager.SecondaryShootButton.State.CurrentState == MMInput.ButtonStates.ButtonPressed);

            bool shootAxisPressed =
                (_inputManager.SecondaryShootAxis == MMInput.ButtonStates.ButtonPressed);

            bool shootButtonUp =
                (_inputManager.SecondaryShootButton.State.CurrentState == MMInput.ButtonStates.ButtonUp);

            bool shootAxisUp =
                (_inputManager.SecondaryShootAxis == MMInput.ButtonStates.ButtonUp);

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
                && (_inputManager.SecondaryShootAxis ==
                    MMInput.ButtonStates.Off)
                && (_inputManager.SecondaryShootButton.State.CurrentState ==
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
        protected override bool IsShootInputActive()
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
                (_inputManager.SecondaryShootButton.State.CurrentState ==
                    MMInput.ButtonStates.ButtonDown)
                ||
                (_inputManager.SecondaryShootButton.State.CurrentState ==
                    MMInput.ButtonStates.ButtonPressed);

            bool axisActive =
                (_inputManager.SecondaryShootAxis ==
                    MMInput.ButtonStates.ButtonDown)
                ||
                (_inputManager.SecondaryShootAxis ==
                    MMInput.ButtonStates.ButtonPressed);

            bool secondaryAxisActive =
                UseSecondaryAxisThresholdToShoot
                && (_inputManager.SecondaryMovement.magnitude >
                    _inputManager.Threshold.magnitude);

            return buttonActive
                || axisActive
                || secondaryAxisActive;
        }
    }
}
