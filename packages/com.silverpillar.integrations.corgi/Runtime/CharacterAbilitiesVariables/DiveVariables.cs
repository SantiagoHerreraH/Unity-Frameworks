using MoreMountains.CorgiEngine;
using SilverPillar.Stats;
using UnityEngine;

namespace SilverPillar.Integrations.Corgi
{
    public class DiveVariables : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("If null will try to get from self")]
        private CharacterDive m_CharacterDive;
        [SerializeField, Tooltip("If null will try to get from self")]
        private StatController m_StatController;

        [Header("Stat Variables")]
        [SerializeField]
        private StatValueRange m_DiveAcceleration = new();

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

            if (m_CharacterDive == null)
            {
                if (!gameObject.TryGetComponent(out m_CharacterDive))
                {
                    Debug.LogError($"{nameof(m_CharacterDive)} is NULL in {nameof(DiveVariables)} on gameobject {gameObject.name}.");
                    allGood = false;
                }
            }

            if (m_StatController == null)
            {
                if (!gameObject.TryGetComponent(out m_StatController))
                {
                    Debug.LogError($"{nameof(m_StatController)} is NULL in {nameof(DiveVariables)} on gameobject {gameObject.name}.");
                    allGood = false;
                }
            }


            allGood &= IsStatTypeValid(m_DiveAcceleration.StatTypeToGetValueFrom.Get(), nameof(m_DiveAcceleration));

            return allGood;
        }

        private void CreateVariables()
        {
            m_StatController.CreateStatType(m_DiveAcceleration.StatTypeToGetValueFrom.Get());
        }

        private void SubscribeToEvents()
        {
            m_StatController.SubscribeOnCurrentStatChange(m_DiveAcceleration.StatTypeToGetValueFrom.Get(), UpdateDiveAcceleration);
        }

        private void UpdateDiveAcceleration(float pastStat, float currentStat)
        {
            m_CharacterDive.DiveAcceleration = m_DiveAcceleration.GetValue(currentStat);
        }

        private bool IsStatTypeValid(StatType statType, string variableName)
        {
            if (statType == null)
            {
                Debug.LogError($"StatType is NULL in {variableName} in {nameof(DiveVariables)} in gameobject {gameObject.name}.");
                return false;
            }

            return true;
        }
    }
}
