using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using UnityEngine;

namespace SilverPillar.Integrations.MMTopDown
{
    public class EnhancedCharacterHandleSecondaryWeapon : CharacterHandleSecondaryWeapon
    {

        /// <summary>
        /// Gets input and triggers methods based on what's been pressed
        /// </summary>
        protected override void HandleInput()
        {
            if (!AbilityAuthorized
                || (_condition.CurrentState != CharacterStates.CharacterConditions.Normal)
                || (CurrentWeapon == null))
            {
                return;
            }

            bool inputAuthorized = true;
            if (CurrentWeapon != null)
            {
                inputAuthorized = CurrentWeapon.InputAuthorized;
            }

            if (inputAuthorized && ((_inputManager.SecondaryShootButton.State.CurrentState == MMInput.ButtonStates.ButtonDown) || (_inputManager.SecondaryShootAxis == MMInput.ButtonStates.ButtonDown)))
            {
                ShootStart();
            }

            bool buttonPressed =
                (_inputManager.SecondaryShootButton.State.CurrentState == MMInput.ButtonStates.ButtonPressed) ||
                (_inputManager.SecondaryShootAxis == MMInput.ButtonStates.ButtonPressed);

            if (inputAuthorized && ContinuousPress && (CurrentWeapon.TriggerMode == Weapon.TriggerModes.Auto) && buttonPressed)
            {
                ShootStart();
            }

            if (_inputManager.ReloadButton.State.CurrentState == MMInput.ButtonStates.ButtonDown)
            {
                Reload();
            }

            if (inputAuthorized && ((_inputManager.SecondaryShootButton.State.CurrentState == MMInput.ButtonStates.ButtonUp) || (_inputManager.SecondaryShootAxis == MMInput.ButtonStates.ButtonUp)))
            {
                ShootStop();
                CurrentWeapon.WeaponInputReleased();
            }

            if ((CurrentWeapon.WeaponState.CurrentState == Weapon.WeaponStates.WeaponDelayBetweenUses)
                && ((_inputManager.SecondaryShootAxis == MMInput.ButtonStates.Off) && (_inputManager.SecondaryShootButton.State.CurrentState == MMInput.ButtonStates.Off))
                && !(UseSecondaryAxisThresholdToShoot && (_inputManager.SecondaryMovement.magnitude > _inputManager.Threshold.magnitude)))
            {
                CurrentWeapon.WeaponInputStop();
            }

            if (inputAuthorized && UseSecondaryAxisThresholdToShoot && (_inputManager.SecondaryMovement.magnitude > _inputManager.Threshold.magnitude))
            {
                ShootStart();
            }
        }
    }
}
