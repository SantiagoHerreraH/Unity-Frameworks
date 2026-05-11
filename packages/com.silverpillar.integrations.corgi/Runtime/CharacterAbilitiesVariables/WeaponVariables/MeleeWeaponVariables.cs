using MoreMountains.CorgiEngine;
using SilverPillar.Modules;
using SilverPillar.Stats;
using Sirenix.OdinInspector;
using UnityEngine;

namespace SilverPillar.Integrations.Corgi
{
    public class MeleeWeaponVariables : WeaponVariables
    {
        [BoxGroup("References")]
        [SerializeField, Tooltip("If null will try to get from self")]
        private MeleeWeapon m_MeleeWeapon;

        [BoxGroup("Melee Weapon")]
        [SerializeField]
        private StatValueRange m_ActiveDuration;
        [BoxGroup("Melee Weapon")]
        [SerializeField]
        private StatValueRange m_InvincibilityDuration;

        protected new bool ReferencesAreCorrect()
        {
            bool allGood = true;
            allGood &= base.ReferencesAreCorrect();

            if (m_MeleeWeapon == null)
            {
                if (!gameObject.TryGetComponent(out m_MeleeWeapon))
                {
                    Debug.LogError($"{nameof(m_MeleeWeapon)} is NULL in {nameof(MeleeWeaponVariables)} on gameobject {gameObject.name}.");
                    allGood = false;
                }
            }

            allGood &= IsStatTypeValid(m_ActiveDuration.StatTypeToGetValueFrom.Get(), nameof(m_ActiveDuration));
            allGood &= IsStatTypeValid(m_InvincibilityDuration.StatTypeToGetValueFrom.Get(), nameof(m_InvincibilityDuration));

            return allGood;
        }

        protected new void CreateVariables()
        {
            base.CreateVariables();

            m_StatController.CreateStatType(m_ActiveDuration.StatTypeToGetValueFrom.Get());
            m_StatController.CreateStatType(m_InvincibilityDuration.StatTypeToGetValueFrom.Get());
        }

        protected new void SubscribeToEvents()
        {
            base.SubscribeToEvents();

            m_StatController.SubscribeOnCurrentStatChange(m_ActiveDuration.StatTypeToGetValueFrom.Get(), UpdateActiveDuration);
            m_StatController.SubscribeOnCurrentStatChange(m_InvincibilityDuration.StatTypeToGetValueFrom.Get(), UpdateInvincibilityDuration);
        }

        private void UpdateActiveDuration(float pastValue, float currentValue)
        {
            m_MeleeWeapon.ActiveDuration = m_ActiveDuration.GetValue(currentValue);
        }

        private void UpdateInvincibilityDuration(float pastValue, float currentValue)
        {
            m_MeleeWeapon.InvincibilityDuration = m_InvincibilityDuration.GetValue(currentValue);
        }
    }
}