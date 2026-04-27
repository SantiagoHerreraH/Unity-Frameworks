using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class CachedScoreComparison_CachedCondition : ICachedCondition
    {
        [Title("If")]
        [OdinSerialize, ShowInInspector]
        private ICachedScore m_First;

        [Title("Condition")]
        [SerializeField]
        private FloatComparison.OperationType m_OperationType;

        [Title("Than")]
        [OdinSerialize, ShowInInspector]
        private ICachedScore m_Second;

        private GameObject m_GameObject;

        /// <summary>
        /// Evaluates the condition based on the chosen operation type.
        /// </summary>
        public bool IsFulfilled()
        {
            if (m_First == null || m_Second == null)
            {
                Debug.LogWarning("Comparison failed: One of the ICachedScore fields is null.");
                return false;
            }

            float valA = m_First.CalculateScore();
            float valB = m_Second.CalculateScore();

            return m_OperationType switch
            {
                FloatComparison.OperationType.Equal => Mathf.Approximately(valA, valB),
                FloatComparison.OperationType.NotEqual => !Mathf.Approximately(valA, valB),
                FloatComparison.OperationType.Greater => valA > valB,
                FloatComparison.OperationType.GreaterOrEqual => valA >= valB,
                FloatComparison.OperationType.Less => valA < valB,
                FloatComparison.OperationType.LessOrEqual => valA <= valB,
                _ => false
            };
        }

        /// <summary>
        /// Pass the GameObject reference down to both score providers.
        /// </summary>
        public bool SetGameObject(GameObject gameObj)
        {
            if (gameObj == null) return false;

            m_GameObject = gameObj;
            bool success = true;

            if (m_First != null) success &= m_First.SetGameObject(gameObj);
            if (m_Second != null) success &= m_Second.SetGameObject(gameObj);

            return success;
        }

        public GameObject GetGameObject() => m_GameObject;

        /// <summary>
        /// Deep copies the condition logic and internal scores.
        /// </summary>
        public ICachedCondition Clone()
        {
            return new CachedScoreComparison_CachedCondition
            {
                m_First = this.m_First?.Clone(),
                m_Second = this.m_Second?.Clone(),
                m_OperationType = this.m_OperationType,
                m_GameObject = this.m_GameObject
            };
        }
    }
}
