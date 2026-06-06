using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class CachedScoreComparison_InteractionCondition : IInteractionCondition
    {
        [Title("If")]
        [SerializeField]
        private bool m_WhatToReturnIfCouldNotSetSelfObject;
        [OdinSerialize, ShowInInspector]
        private ICachedScore m_ScoreFromSelf;

        [Title("Condition")]
        [SerializeField]
        private FloatComparison.OperationType m_OperationType;

        [Title("Than")]
        [SerializeField]
        private bool m_WhatToReturnIfCouldNotSetTargetObject;
        [OdinSerialize, ShowInInspector]
        private ICachedScore m_ScoreFromTarget;

        private GameObject m_GameObject;
        private bool m_CouldSetSelf = false;

        /// <summary>
        /// Evaluates the condition based on the chosen operation type.
        /// </summary>
        public bool IsFulfilled(GameObject target)
        {
            if (m_ScoreFromSelf == null || m_ScoreFromTarget == null)
            {
                Debug.LogWarning("Comparison failed: One of the ICachedScore fields is null.");
                return false;
            }

            if (!m_CouldSetSelf)
            {
                return m_WhatToReturnIfCouldNotSetSelfObject;
            }


            if (!m_ScoreFromTarget.SetGameObject(target))
            {
                return m_WhatToReturnIfCouldNotSetTargetObject;
            }

            float valA = m_ScoreFromSelf.CalculateScore();
            float valB = m_ScoreFromTarget.CalculateScore();

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

            if (m_ScoreFromSelf != null) 
            {
                m_CouldSetSelf = m_ScoreFromSelf.SetGameObject(gameObj);

                success &= m_CouldSetSelf;
            }
            else
            {
                m_CouldSetSelf = false;
                success = false;
            }
            
            return success;
        }

        public GameObject GetGameObject() => m_GameObject;

        /// <summary>
        /// Deep copies the condition logic and internal scores.
        /// </summary>
        public IInteractionCondition Clone()
        {
            return new CachedScoreComparison_InteractionCondition
            {
                m_WhatToReturnIfCouldNotSetSelfObject = this.m_WhatToReturnIfCouldNotSetSelfObject,
                m_WhatToReturnIfCouldNotSetTargetObject = this.m_WhatToReturnIfCouldNotSetTargetObject,
                m_ScoreFromSelf = this.m_ScoreFromSelf?.Clone(),
                m_ScoreFromTarget = this.m_ScoreFromTarget?.Clone(),
                m_OperationType = this.m_OperationType,
                m_GameObject = this.m_GameObject,
                m_CouldSetSelf = this.m_CouldSetSelf
            };
        }
    }
}
