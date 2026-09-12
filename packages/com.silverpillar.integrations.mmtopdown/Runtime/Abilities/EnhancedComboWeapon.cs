using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using System.Linq;
using UnityEngine;
using static Codice.Client.Common.Connection.AskCredentialsToUser;

namespace SilverPillar.Integrations.MMTopDown
{
    public class EnhancedComboWeapon : ComboWeapon
    {
        private enum WhereToGetWeaponsFrom
        {
            SelfComponents,
            SelfAndActiveChildComponents,
            SelfAndAllChildComponents
        }

        [Header("Settings")]
        [SerializeField]
        private WhereToGetWeaponsFrom m_WhereToGetWeaponsFrom;
        [SerializeField]
        private bool m_ActivateWeaponGameObjectOnUseAndDeactiveWhenStopUsing;
        [SerializeField]
        private bool m_OverrideCharacterHandleWeaponOwner;
        [SerializeField, MMCondition(nameof(m_OverrideCharacterHandleWeaponOwner), true)]
        public Character m_OwnerCharacter;
        [SerializeField, MMCondition(nameof(m_OverrideCharacterHandleWeaponOwner), true)]
        public CharacterHandleWeapon m_OwnerCharacterHandleWeapon;

        /// <summary>
		/// Grabs all Weapon components and initializes them
		/// </summary>
		public override void Initialization()
        {
            switch (m_WhereToGetWeaponsFrom)
            {
                case WhereToGetWeaponsFrom.SelfComponents:
                    Weapons = GetComponents<Weapon>();
                    break;
                case WhereToGetWeaponsFrom.SelfAndActiveChildComponents:
                    Weapons = GetComponentsInChildren<Weapon>();
                    break;
                case WhereToGetWeaponsFrom.SelfAndAllChildComponents:
                    Weapons = GetComponentsInChildren<Weapon>(true);
                    break;
                default:
                    break;
            }

            _weaponAutoShoot = this.gameObject.GetComponent<WeaponAutoShoot>();
            InitializeUnusedWeapons();

            if (Weapons.Length > 0)
            {
                Activate(Weapons[_currentWeaponIndex]);
            }
        }

        /// <summary>
		/// Initializes all unused weapons
		/// </summary>
		protected override void InitializeUnusedWeapons()
        {
            if (Weapons.Length > 0 && m_OverrideCharacterHandleWeaponOwner)
            {
                Weapons[_currentWeaponIndex].SetOwner(m_OwnerCharacter, m_OwnerCharacterHandleWeapon);
                Weapons[_currentWeaponIndex].Initialization();
                Weapons[_currentWeaponIndex].SetComboWeapon(this);
                OwnerCharacterHandleWeapon = Weapons[_currentWeaponIndex].CharacterHandleWeapon;
            }


            for (int i = 0; i < Weapons.Length; i++)
            {
                if (i != _currentWeaponIndex)
                {
                    Weapons[i].SetOwner(Weapons[_currentWeaponIndex].Owner, Weapons[_currentWeaponIndex].CharacterHandleWeapon);
                    Weapons[i].Initialization();
                    Weapons[i].WeaponCurrentlyActive = false;

                    if (m_ActivateWeaponGameObjectOnUseAndDeactiveWhenStopUsing)
                    {
                        Deactivate(Weapons[i]);
                    }
                }

                Weapons[i].SetComboWeapon(this);
            }
        }


        public override void ProceedToNextWeapon()
        {
            if (Weapons.Length == 0)
            {
                return;
            }

            OwnerCharacterHandleWeapon = Weapons[_currentWeaponIndex].CharacterHandleWeapon;

            int newIndex = 0;
            if (OwnerCharacterHandleWeapon != null)
            {
                if (Weapons.Length > 1)
                {
                    if (_currentWeaponIndex < Weapons.Length - 1)
                    {
                        newIndex = _currentWeaponIndex + 1;
                    }
                    else
                    {
                        newIndex = 0;
                    }

                    _countdownActive = true;
                    TimeSinceLastWeaponStopped = 0f;

                    Deactivate(Weapons[_currentWeaponIndex]);
                    _currentWeaponIndex = newIndex;
                    Activate(Weapons[newIndex]);

                    if (_weaponAutoShoot != null)
                    {
                        _weaponAutoShoot.SetCurrentWeapon(Weapons[newIndex]);
                    }
                }
            }
        }

        private void Activate(Weapon weapon)
        {
            if (m_ActivateWeaponGameObjectOnUseAndDeactiveWhenStopUsing)
            {
                weapon.gameObject.SetActive(true);
            }

            OwnerCharacterHandleWeapon.CurrentWeapon = weapon;
            OwnerCharacterHandleWeapon.CurrentWeapon.WeaponCurrentlyActive = false;
            OwnerCharacterHandleWeapon.ChangeWeapon(weapon, weapon.WeaponName, true);
            OwnerCharacterHandleWeapon.CurrentWeapon.WeaponCurrentlyActive = true;
        }

        private void Deactivate(Weapon weapon)
        {
            if (m_ActivateWeaponGameObjectOnUseAndDeactiveWhenStopUsing)
            {
                weapon.gameObject.SetActive(false);
            }

        }
    }
}
