using MoreMountains.CorgiEngine;
using SilverPillar.Stats;
using UnityEngine;

namespace SilverPillar.Integrations.Corgi
{
    public class SwimVariables : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("If null will try to get from self")]
        private CharacterSwim m_CharacterSwim;
        [SerializeField, Tooltip("If null will try to get from self")]
        private StatController m_StatController;

        [Header("Stat Variables")]
        [SerializeField]
        private StatValueRange m_SwimHeight = new();

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

            if (m_CharacterSwim == null)
            {
                if (!gameObject.TryGetComponent(out m_CharacterSwim))
                {
                    Debug.LogError($"{nameof(m_CharacterSwim)} is NULL in {this.GetType().Name} on gameobject {gameObject.name}.");
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

            allGood &= IsStatTypeValid(m_SwimHeight.StatTypeToGetValueFrom.Get(), nameof(m_SwimHeight));


            return allGood;
        }

        private void CreateVariables()
        {
            m_StatController.CreateStatType(m_SwimHeight.StatTypeToGetValueFrom.Get());
        }

        private void SubscribeToEvents()
        {
            m_StatController.SubscribeOnCurrentStatChange(m_SwimHeight.StatTypeToGetValueFrom.Get(), UpdateSwimHeight);
        }

        private void UpdateSwimHeight(float pastStat, float currentStat)
        {
            m_CharacterSwim.SwimHeight = m_SwimHeight.GetValue(currentStat);
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
    }
}
