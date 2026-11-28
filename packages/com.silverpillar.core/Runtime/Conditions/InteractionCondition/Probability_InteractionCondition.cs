using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class Probability_InteractionCondition : IInteractionCondition
    {
        private enum ProbabilityOf
        {
            OfReturningTrue,
            OfReturningFalse
        }

        private enum RandomType
        {
            RandomController,
            UnityRandom
        }

        [SerializeField]
        private RandomType m_RandomType;

        [SerializeField]
        private ProbabilityOf m_ProbabilityOfWhat;

        [OdinSerialize, ShowInInspector, Tooltip("From 0 to 100")]
        private ICachedInteractionScore m_Percentage;

        private GameObject m_Self;

        public IInteractionCondition Clone()
        {
            return new Probability_InteractionCondition
            {
                m_ProbabilityOfWhat = m_ProbabilityOfWhat,
                m_Percentage = m_Percentage?.Clone(),
                m_Self = m_Self
            };
        }

        public GameObject GetGameObject()
        {
            return m_Self;
        }

        public bool IsFulfilled(GameObject target)
        {
            if (m_Percentage == null)
            {
                Debug.LogError(
                    $"{nameof(Probability_InteractionCondition)} " +
                    "could not evaluate its condition because " +
                    "no percentage score has been assigned.",
                    m_Self);

                return false;
            }

            float percentage = m_Percentage.CalculateScore(target);

            bool result;

            if (percentage <= 0f)
            {
                result = false;
            }
            else if (percentage >= 100f)
            {
                result = true;
            }
            else
            {
                switch (m_RandomType)
                {
                    case RandomType.RandomController:
                        result = (RandomController.Instance.GetRandomFloat() * 100f) < percentage;
                        break;
                    case RandomType.UnityRandom:
                        result = (UnityEngine.Random.value * 100f) < percentage;
                        break;
                    default:
                        result = false;
                        break;
                }

            }

            return m_ProbabilityOfWhat == ProbabilityOf.OfReturningTrue
                ? result
                : !result;
        }

        public bool SetGameObject(GameObject self)
        {
            m_Self = self;

            if (self == null)
            {
                Debug.LogError(
                    $"{nameof(Probability_InteractionCondition)} " +
                    "could not initialize because the provided " +
                    "GameObject is null.");

                return false;
            }

            if (m_Percentage == null)
            {
                Debug.LogError(
                    $"{nameof(Probability_InteractionCondition)} " +
                    $"on '{self.name}' could not initialize because " +
                    "no percentage score has been assigned.",
                    self);

                return false;
            }

            return m_Percentage.SetGameObject(self);
        }
    }
}