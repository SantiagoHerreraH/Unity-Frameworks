using MoreMountains.CorgiEngine;
using SilverPillar.Modules;
using SilverPillar.Stats;
using UnityEngine;

namespace SilverPillar.Integrations.Corgi
{
    public class GlideVariables : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("If null will try to get from self")]
        private CharacterGlide m_CharacterGlide;
        [SerializeField, Tooltip("If null will try to get from self")]
        private StatController m_StatController;
        [SerializeField, Tooltip("If null will try to get from self")]
        private BoolModuleController m_BoolModuleController;

        [Header("Stat Variables")]
        [SerializeField]
        private StatValueRange m_VerticalForce = new();

        [Header("Bool Module Variables")]
        [SerializeField]
        private BoolModuleType m_GlidePermitted;

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

            if (m_CharacterGlide == null)
            {
                if (!gameObject.TryGetComponent(out m_CharacterGlide))
                {
                    Debug.LogError($"{nameof(m_CharacterGlide)} is NULL in {this.GetType().Name} on gameobject {gameObject.name}.");
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

            allGood &= IsStatTypeValid(m_VerticalForce.StatTypeToGetValueFrom.Get(), nameof(m_VerticalForce));

            allGood &= IsBoolModuleValid(m_GlidePermitted, nameof(m_GlidePermitted));

            return allGood;
        }

        private void CreateVariables()
        {
            m_StatController.CreateStatType(m_VerticalForce.StatTypeToGetValueFrom.Get());

            m_BoolModuleController.CreateBoolModule(m_GlidePermitted, false);
        }

        private void SubscribeToEvents()
        {
            m_StatController.SubscribeOnCurrentStatChange(m_VerticalForce.StatTypeToGetValueFrom.Get(), UpdateVerticalForce);

            m_BoolModuleController.SubscribeOnSetState(m_GlidePermitted, UpdateGlidePermitted);
        }

        private void UpdateVerticalForce(float pastStat, float currentStat)
        {
            m_CharacterGlide.VerticalForce = m_VerticalForce.GetValue(currentStat);
        }

        private void UpdateGlidePermitted(bool state)
        {
            m_CharacterGlide.AbilityPermitted = state;
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
