using MoreMountains.CorgiEngine;
using SilverPillar.Stats;
using UnityEngine;

namespace SilverPillar.Integrations.Corgi
{
    public class RunVariables : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("If null will try to get from self")]
        private CharacterRun m_CharacterRun;
        [SerializeField, Tooltip("If null will try to get from self")]
        private StatController m_StatController;

        [Header("Stat Variables")]
        [SerializeField]
        private StatValueRange m_RunSpeed = new();

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

            if (m_CharacterRun == null)
            {
                if (!gameObject.TryGetComponent(out m_CharacterRun))
                {
                    Debug.LogError($"{nameof(m_CharacterRun)} is NULL in {nameof(RunVariables)} on gameobject {gameObject.name}.");
                    allGood = false;
                }
            }

            if (m_StatController == null)
            {
                if (!gameObject.TryGetComponent(out m_StatController))
                {
                    Debug.LogError($"{nameof(m_StatController)} is NULL in {nameof(RunVariables)} on gameobject {gameObject.name}.");
                    allGood = false;
                }
            }


            allGood &= IsStatTypeValid(m_RunSpeed.StatTypeToGetValueFrom.Get(), nameof(m_RunSpeed));

            return allGood;
        }

        private void CreateVariables()
        {
            m_StatController.CreateStatType(m_RunSpeed.StatTypeToGetValueFrom.Get());
        }

        private void SubscribeToEvents()
        {
            m_StatController.SubscribeOnCurrentStatChange(m_RunSpeed.StatTypeToGetValueFrom.Get(), UpdateRunSpeed);
        }

        private void UpdateRunSpeed(float pastStat, float currentStat)
        {
            m_CharacterRun.RunSpeed = m_RunSpeed.GetValue(currentStat);
        }

        private bool IsStatTypeValid(StatType statType, string variableName)
        {
            if (statType == null)
            {
                Debug.LogError($"StatType is NULL in {variableName} in {nameof(RunVariables)} in gameobject {gameObject.name}.");
                return false;
            }

            return true;
        }
    }
}
