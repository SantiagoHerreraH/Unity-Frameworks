using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Core
{
    public enum ScoreOperationType
    {
        Add,
        Subtract,
        Multiply,
        Divide,
        Power,
        Root
    }

    [Serializable]
    public class ScoreOperation_CachedScore : ICachedScore
    {
       
        [SerializeField]
        private ScoreOperationType m_Operation;

        [OdinSerialize, ShowInInspector]
        private ICachedScore m_First;

        [OdinSerialize, ShowInInspector, HideIf(nameof(m_Operation), ScoreOperationType.Root)]
        private ICachedScore m_Second;

        public float CalculateScore()
        {
            float v1 = m_First != null ? m_First.CalculateScore() : 0f;
            float v2 = m_Second != null ? m_Second.CalculateScore() : 0f;

            return m_Operation switch
            {
                ScoreOperationType.Add => v1 + v2,
                ScoreOperationType.Subtract => v1 - v2,
                ScoreOperationType.Multiply => v1 * v2,
                ScoreOperationType.Divide => v2 != 0 ? v1 / v2 : 0f,
                ScoreOperationType.Power => Mathf.Pow(v1, v2),

                // Now treated as a binary operation: v1 is the radicand, v2 is the index (n)
                ScoreOperationType.Root => CalculateRoot(v1, v2),

                _ => 0f
            };
        }

        private float CalculateRoot(float baseValue, float index)
        {
            // Prevent division by zero in the exponent (1/n)
            if (index == 0) return 0f;

            // Handle negative base values
            if (baseValue < 0)
            {
                // Mathematically, we can calculate the root of a negative number if the index is odd (e.g., cubic root)
                if (index % 2 != 0)
                {
                    return -Mathf.Pow(Mathf.Abs(baseValue), 1f / index);
                }

                // Return 0 for even roots of negative numbers to avoid NaN (Imaginary numbers)
                return 0f;
            }

            // Standard n-th root calculation: x^(1/n)
            return Mathf.Pow(baseValue, 1f / index);
        }



        public ICachedScore Clone()
        {
            return new ScoreOperation_CachedScore
            {
                m_Operation = this.m_Operation,
                m_First = this.m_First?.Clone(),
                m_Second = this.m_Second?.Clone()
            };
        }

        public GameObject GetGameObject()
        {
            return m_First?.GetGameObject();
        }

        public bool SetGameObject(GameObject self)
        {
            bool firstOk = m_First != null && m_First.SetGameObject(self);

            // If it's SquareRoot, we don't care about the second operand
            if (m_Operation == ScoreOperationType.Root) return firstOk;

            bool secondOk = m_Second != null && m_Second.SetGameObject(self);
            return firstOk && secondOk;
        }
    }
}
