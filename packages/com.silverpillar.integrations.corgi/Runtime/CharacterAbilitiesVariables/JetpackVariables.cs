using MoreMountains.CorgiEngine;
using SilverPillar.Modules;
using SilverPillar.Stats;
using UnityEngine;

namespace SilverPillar.Integrations.Corgi
{
    public class JetpackVariables : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("If null will try to get from self")]
        private CharacterJetpack m_CharacterJetpack;
        [SerializeField, Tooltip("If null will try to get from self")]
        private StatController m_StatController;
        [SerializeField, Tooltip("If null will try to get from self")]
        private BoolModuleController m_BoolModuleController;

        [Header("Stat Variables")]
        [SerializeField]
        private StatValueRange m_JetpackForce = new();
        [SerializeField]
        private StatValueRange m_JetpackFuelDuration = new();
        [SerializeField]
        private StatValueRange m_JetpackRefuelCooldown = new();
        [SerializeField]
        private StatValueRange m_JetpackRefuelSpeed = new();

        [Header("Bool Module Variables")]
        [SerializeField]
        private BoolModuleType m_JetpackPermitted;

        private void Awake()
        {
            if (ReferencesAreCorrect())
            {
                CreateVariables();
                SubscribeToEvents();
            }
        }

        private bool ReferencesAreCorrect()
        {
            bool allGood = true;

            if (m_CharacterJetpack == null)
            {
                if (!gameObject.TryGetComponent(out m_CharacterJetpack))
                {
                    Debug.LogError($"{nameof(m_CharacterJetpack)} is NULL in {this.GetType().Name} on gameobject {gameObject.name}.");
                    allGood = false;
                }
            }

            if (m_StatController == null)
            {
                if (!gameObject.TryGetComponent(out m_StatController))
                {
                    Debug.LogError($"{nameof(m_StatController)} is NULL in {this.GetType().Name} on gameobject {gameObject.name}.");
                    allGood = false;
                }
            }

            if (m_BoolModuleController == null)
            {
                if (!gameObject.TryGetComponent(out m_BoolModuleController))
                {
                    Debug.LogError($"{nameof(m_BoolModuleController)} is NULL in {this.GetType().Name} on gameobject {gameObject.name}.");
                    allGood = false;
                }
            }

            allGood &= IsStatTypeValid(m_JetpackForce.StatTypeToGetValueFrom.Get(), nameof(m_JetpackForce));
            allGood &= IsStatTypeValid(m_JetpackFuelDuration.StatTypeToGetValueFrom.Get(), nameof(m_JetpackFuelDuration));
            allGood &= IsStatTypeValid(m_JetpackRefuelCooldown.StatTypeToGetValueFrom.Get(), nameof(m_JetpackRefuelCooldown));
            allGood &= IsStatTypeValid(m_JetpackRefuelSpeed.StatTypeToGetValueFrom.Get(), nameof(m_JetpackRefuelSpeed));

            allGood &= IsBoolModuleValid(m_JetpackPermitted, nameof(m_JetpackPermitted));

            return allGood;
        }

        private void CreateVariables()
        {
            m_StatController.CreateStatType(m_JetpackForce.StatTypeToGetValueFrom.Get());
            m_StatController.CreateStatType(m_JetpackFuelDuration.StatTypeToGetValueFrom.Get());
            m_StatController.CreateStatType(m_JetpackRefuelCooldown.StatTypeToGetValueFrom.Get());
            m_StatController.CreateStatType(m_JetpackRefuelSpeed.StatTypeToGetValueFrom.Get());

            m_BoolModuleController.CreateBoolModule(m_JetpackPermitted, false);
        }

        private void SubscribeToEvents()
        {
            m_StatController.SubscribeOnCurrentStatChange(m_JetpackForce.StatTypeToGetValueFrom.Get(), UpdateJetpackForce);
            m_StatController.SubscribeOnCurrentStatChange(m_JetpackFuelDuration.StatTypeToGetValueFrom.Get(), UpdateJetpackFuelDuration);
            m_StatController.SubscribeOnCurrentStatChange(m_JetpackRefuelCooldown.StatTypeToGetValueFrom.Get(), UpdateJetpackRefuelCooldown);
            m_StatController.SubscribeOnCurrentStatChange(m_JetpackRefuelSpeed.StatTypeToGetValueFrom.Get(), UpdateRefuelSpeed);

            m_BoolModuleController.SubscribeOnSetState(m_JetpackPermitted, UpdateJetpackPermitted);
        }

        private void UpdateJetpackForce(float pastStat, float currentStat)
        {
            m_CharacterJetpack.JetpackForce = m_JetpackForce.GetValue(currentStat);
        }

        private void UpdateJetpackFuelDuration(float pastStat, float currentStat)
        {
            m_CharacterJetpack.JetpackFuelDuration = m_JetpackFuelDuration.GetValue(currentStat);
        }

        private void UpdateJetpackRefuelCooldown(float pastStat, float currentStat)
        {
            m_CharacterJetpack.JetpackRefuelCooldown = m_JetpackRefuelCooldown.GetValue(currentStat);
        }

        private void UpdateRefuelSpeed(float pastStat, float currentStat)
        {
            m_CharacterJetpack.RefuelSpeed = m_JetpackRefuelSpeed.GetValue(currentStat);
        }

        private void UpdateJetpackPermitted(bool state)
        {
            m_CharacterJetpack.AbilityPermitted = state;
        }

        private bool IsStatTypeValid(StatType statType, string variableName)
        {
            if (statType == null)
            {
                Debug.LogError($"StatType is NULL in {variableName} in {this.GetType().Name} in gameobject {gameObject.name}.");
                return false;
            }

            return true;
        }

        private bool IsBoolModuleValid(BoolModuleType type, string variableName)
        {
            if (type == null)
            {
                Debug.LogError($"BoolModule is NULL in {variableName} in {this.GetType().Name} in gameobject {gameObject.name}.");
                return false;
            }

            return true;
        }
    }
}
