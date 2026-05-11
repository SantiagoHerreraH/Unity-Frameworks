using MoreMountains.CorgiEngine;
using SilverPillar.Modules;
using SilverPillar.Stats;
using UnityEngine;

namespace SilverPillar.Integrations.Corgi
{
    public class CrouchVariables : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("If null will try to get from self")]
        private CharacterCrouch m_CharacterCrouch;
        [SerializeField, Tooltip("If null will try to get from self")]
        private StatController m_StatController;

        [Header("Stat Variables")]
        [SerializeField]
        private StatValueRange m_CrawlSpeed = new();

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

            if (m_CharacterCrouch == null)
            {
                if (!gameObject.TryGetComponent(out m_CharacterCrouch))
                {
                    Debug.LogError($"{nameof(m_CharacterCrouch)} is NULL in {nameof(CrouchVariables)} on gameobject {gameObject.name}.");
                    allGood = false;
                }
            }

            if (m_StatController == null)
            {
                if (!gameObject.TryGetComponent(out m_StatController))
                {
                    Debug.LogError($"{nameof(m_StatController)} is NULL in {nameof(CrouchVariables)} on gameobject {gameObject.name}.");
                    allGood = false;
                }
            }


            allGood &= IsStatTypeValid(m_CrawlSpeed.StatTypeToGetValueFrom.Get(), nameof(m_CrawlSpeed));
           
            return allGood;
        }

        private void CreateVariables()
        {
            m_StatController.CreateStatType(m_CrawlSpeed.StatTypeToGetValueFrom.Get());
        }

        private void SubscribeToEvents()
        {
            m_StatController.SubscribeOnCurrentStatChange(m_CrawlSpeed.StatTypeToGetValueFrom.Get(), UpdateCrawlSpeed);
        }

        private void UpdateCrawlSpeed(float pastStat, float currentStat)
        {
            m_CharacterCrouch.CrawlSpeed = m_CrawlSpeed.GetValue(currentStat);
        }

        private bool IsStatTypeValid(StatType statType, string variableName)
        {
            if (statType == null)
            {
                Debug.LogError($"StatType is NULL in {variableName} in {nameof(CrouchVariables)} in gameobject {gameObject.name}.");
                return false;
            }

            return true;
        }
    }
}
