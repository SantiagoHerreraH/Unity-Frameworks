using MoreMountains.CorgiEngine;
using SilverPillar.Modules;
using SilverPillar.Stats;
using UnityEngine;

namespace SilverPillar.Integrations.Corgi
{
    public class WallClingingVariables : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("If null will try to get from self")]
        private CharacterWallClinging m_CharacterWallClinging;
        [SerializeField, Tooltip("If null will try to get from self")]
        private StatController m_StatController;

        [Header("Stat Variables")]
        [SerializeField]
        private StatValueRange m_SlowFactor = new();

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

            if (m_CharacterWallClinging == null)
            {
                if (!gameObject.TryGetComponent(out m_CharacterWallClinging))
                {
                    Debug.LogError($"{nameof(m_CharacterWallClinging)} is NULL in {this.GetType().Name} on gameobject {gameObject.name}.");
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

            allGood &= IsStatTypeValid(m_SlowFactor.StatTypeToGetValueFrom.Get(), nameof(m_SlowFactor));


            return allGood;
        }

        private void CreateVariables()
        {
            m_StatController.CreateStatType(m_SlowFactor.StatTypeToGetValueFrom.Get());
        }

        private void SubscribeToEvents()
        {
            m_StatController.SubscribeOnCurrentStatChange(m_SlowFactor.StatTypeToGetValueFrom.Get(), UpdateSlowFactor);
        }

        private void UpdateSlowFactor(float pastStat, float currentStat)
        {
            m_CharacterWallClinging.WallClingingSlowFactor = m_SlowFactor.GetValue(currentStat);
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
