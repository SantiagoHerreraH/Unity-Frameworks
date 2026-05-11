using MoreMountains.CorgiEngine;
using UnityEngine;
using SilverPillar.Stats;
using SilverPillar.Modules;

namespace SilverPillar.Integrations.Corgi
{
    public class DashVariables : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("If null will try to get from self")]
        private CharacterDash m_CharacterDash;
        [SerializeField, Tooltip("If null will try to get from self")]
        private StatController m_StatController;
        [SerializeField, Tooltip("If null will try to get from self")]
        private BoolModuleController m_BoolModuleController;

        [Header("Stat Variables")]
        [SerializeField]
        private StatValueRange m_DashDistance = new();
        [SerializeField]
        private StatValueRange m_DashForce = new();
        [SerializeField]
        private StatValueRange m_DashCooldown = new();
        [SerializeField]
        private StatValueRange m_SuccessiveDashAmount = new();
        [SerializeField]
        private StatValueRange m_SuccessiveDashResetDuration = new();

        [Header("Bool Module Variables")]
        [SerializeField]
        private BoolModuleType m_InvincibleWhileDashing;

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

            if (m_CharacterDash == null)
            {
                if (!gameObject.TryGetComponent(out m_CharacterDash))
                {
                    Debug.LogError($"{nameof(m_CharacterDash)} is NULL in {nameof(DashVariables)} on gameobject {gameObject.name}.");
                    allGood = false;
                }
            }

            if (m_StatController == null)
            {
                if (!gameObject.TryGetComponent(out m_StatController))
                {
                    Debug.LogError($"{nameof(m_StatController)} is NULL in {nameof(DashVariables)} on gameobject {gameObject.name}.");
                    allGood = false;
                }
            }

            if (m_BoolModuleController == null)
            {
                if (!gameObject.TryGetComponent(out m_BoolModuleController))
                {
                    Debug.LogError($"{nameof(m_BoolModuleController)} is NULL in {nameof(DashVariables)} on gameobject {gameObject.name}.");
                    allGood = false;
                }
            }

            allGood &= IsStatTypeValid(m_DashDistance.StatTypeToGetValueFrom.Get(), nameof(m_DashDistance));
            allGood &= IsStatTypeValid(m_DashForce.StatTypeToGetValueFrom.Get(), nameof(m_DashForce));
            allGood &= IsStatTypeValid(m_DashCooldown.StatTypeToGetValueFrom.Get(), nameof(m_DashCooldown));
            allGood &= IsStatTypeValid(m_SuccessiveDashAmount.StatTypeToGetValueFrom.Get(), nameof(m_SuccessiveDashAmount));
            allGood &= IsStatTypeValid(m_SuccessiveDashResetDuration.StatTypeToGetValueFrom.Get(), nameof(m_SuccessiveDashResetDuration));

            allGood &= IsBoolModuleValid(m_InvincibleWhileDashing, nameof(m_InvincibleWhileDashing));

            return allGood;
        }

        private void CreateVariables()
        {
            m_StatController.CreateStatType(m_DashDistance.StatTypeToGetValueFrom.Get());
            m_StatController.CreateStatType(m_DashForce.StatTypeToGetValueFrom.Get());
            m_StatController.CreateStatType(m_DashCooldown.StatTypeToGetValueFrom.Get());
            m_StatController.CreateStatType(m_SuccessiveDashAmount.StatTypeToGetValueFrom.Get());
            m_StatController.CreateStatType(m_SuccessiveDashResetDuration.StatTypeToGetValueFrom.Get());

            m_BoolModuleController.CreateBoolModule(m_InvincibleWhileDashing, false);
        }

        private void SubscribeToEvents()
        {
            m_StatController.SubscribeOnCurrentStatChange(m_DashDistance.StatTypeToGetValueFrom.Get(), UpdateDashDistance);
            m_StatController.SubscribeOnCurrentStatChange(m_DashForce.StatTypeToGetValueFrom.Get(), UpdateDashForce);
            m_StatController.SubscribeOnCurrentStatChange(m_DashCooldown.StatTypeToGetValueFrom.Get(), UpdateDashCooldown);
            m_StatController.SubscribeOnCurrentStatChange(m_SuccessiveDashAmount.StatTypeToGetValueFrom.Get(), UpdateSuccessiveDashAmount);
            m_StatController.SubscribeOnCurrentStatChange(m_SuccessiveDashResetDuration.StatTypeToGetValueFrom.Get(), UpdateSuccessiveDashResetDuration);

            m_BoolModuleController.SubscribeOnSetState(m_InvincibleWhileDashing, UpdateInvincibleWhileDashing);
        }

        private void UpdateDashDistance(float pastStat, float currentStat)
        {
            m_CharacterDash.DashDistance = m_DashDistance.GetValue(currentStat);
        }

        private void UpdateDashForce(float pastStat, float currentStat)
        {
            m_CharacterDash.DashForce = m_DashForce.GetValue(currentStat);
        }

        private void UpdateDashCooldown(float pastStat, float currentStat)
        {
            m_CharacterDash.DashCooldown = m_DashCooldown.GetValue(currentStat);
        }

        private void UpdateSuccessiveDashAmount(float pastStat, float currentStat)
        {
            m_CharacterDash.SuccessiveDashAmount = (int)m_SuccessiveDashAmount.GetValue(currentStat);
        }

        private void UpdateSuccessiveDashResetDuration(float pastStat, float currentStat)
        {
            m_CharacterDash.SuccessiveDashResetDuration = m_SuccessiveDashResetDuration.GetValue(currentStat);
        }

        private void UpdateInvincibleWhileDashing(bool state)
        {
            m_CharacterDash.InvincibleWhileDashing = state;
        }

        private bool IsStatTypeValid(StatType statType, string variableName)
        {
            if (statType == null)
            {
                Debug.LogError($"StatType is NULL in {variableName} in {nameof(DashVariables)} in gameobject {gameObject.name}.");
                return false;
            }

            return true;
        }

        private bool IsBoolModuleValid(BoolModuleType type, string variableName)
        {
            if (type == null)
            {
                Debug.LogError($"BoolModule is NULL in {variableName} in {nameof(DashVariables)} in gameobject {gameObject.name}.");
                return false;
            }

            return true;
        }
    }
}