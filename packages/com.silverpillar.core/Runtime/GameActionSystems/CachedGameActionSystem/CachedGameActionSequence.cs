using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class CachedGameActionSequence : ICachedGameAction
    {
        public enum ExecutionType
        {
            InForwardOrder,
            InReverseOrder,
            InRandomOrder,
            ForwardThenReverse,
            ReverseThenForward
        }

        [SerializeField]
        private ExecutionType m_ExecutionType;

        [SerializeField, Min(0), ShowIf(nameof(m_ExecutionType), ExecutionType.InRandomOrder), Tooltip("Max Action Repetitions before it is forced to go to another action")]
        private int m_MaxActionRepetitions;

        [OdinSerialize, ShowInInspector]
        private List<ICachedGameAction> m_Actions = new();

        private int m_CurrentActionIdx = -1;
        private int m_LastRandomIdx = -1;
        private int m_ConsecutiveRandomCount = 0;
        private bool m_IsReversing = false;

        public void Execute()
        {
            if (m_Actions == null || m_Actions.Count == 0) return;

            UpdateIndex();

            if (m_CurrentActionIdx >= 0 && m_CurrentActionIdx < m_Actions.Count)
            {
                m_Actions[m_CurrentActionIdx]?.Execute();
            }
        }

        private void UpdateIndex()
        {
            int count = m_Actions.Count;

            switch (m_ExecutionType)
            {
                case ExecutionType.InForwardOrder:
                    m_CurrentActionIdx = (m_CurrentActionIdx + 1) % count;
                    break;

                case ExecutionType.InReverseOrder:
                    m_CurrentActionIdx = (m_CurrentActionIdx <= 0) ? count - 1 : m_CurrentActionIdx - 1;
                    break;

                case ExecutionType.InRandomOrder:
                    HandleRandomExecution(count);
                    break;

                case ExecutionType.ForwardThenReverse:
                    HandlePingPong(count, startForward: true);
                    break;

                case ExecutionType.ReverseThenForward:
                    HandlePingPong(count, startForward: false);
                    break;
            }
        }

        private void HandleRandomExecution(int actionCount)
        {
            if (actionCount == 1)
            {
                m_CurrentActionIdx = 0;
                return;
            }

            int nextIdx = UnityEngine.Random.Range(0, actionCount);

            if (nextIdx == m_LastRandomIdx)
            {
                m_ConsecutiveRandomCount++;
                if (m_ConsecutiveRandomCount >= m_MaxActionRepetitions)
                {
                    // Force a different index
                    nextIdx = (nextIdx + UnityEngine.Random.Range(1, actionCount)) % actionCount;
                    m_ConsecutiveRandomCount = 0;
                }
            }
            else
            {
                m_ConsecutiveRandomCount = 0;
            }

            m_CurrentActionIdx = nextIdx;
            m_LastRandomIdx = nextIdx;
        }

        private void HandlePingPong(int count, bool startForward)
        {
            // Initialize direction on first run
            if (m_CurrentActionIdx == -1)
            {
                m_IsReversing = !startForward;
                m_CurrentActionIdx = m_IsReversing ? count - 1 : 0;
                return;
            }

            if (!m_IsReversing)
            {
                if (m_CurrentActionIdx + 1 < count)
                {
                    m_CurrentActionIdx++;
                }
                else
                {
                    m_IsReversing = true;
                    m_CurrentActionIdx--;
                }
            }
            else
            {
                if (m_CurrentActionIdx - 1 >= 0)
                {
                    m_CurrentActionIdx--;
                }
                else
                {
                    m_IsReversing = false;
                    m_CurrentActionIdx++;
                }
            }

            // Clamp for safety if list size changed
            m_CurrentActionIdx = Mathf.Clamp(m_CurrentActionIdx, 0, count - 1);
        }

        public ICachedGameAction Clone()
        {
            var clone = new CachedGameActionSequence
            {
                m_ExecutionType = this.m_ExecutionType,
                m_MaxActionRepetitions = this.m_MaxActionRepetitions,
                m_Actions = new List<ICachedGameAction>()
            };

            foreach (var action in m_Actions)
            {
                clone.m_Actions.Add(action?.Clone());
            }

            return clone;
        }

#nullable enable

        public GameObject? GetGameObject()
        {
            if (m_Actions == null || m_Actions.Count == 0) return null;
            return m_Actions[0]?.GetGameObject();
        }

        public bool SetGameObject(GameObject gameObj)
        {
            if (m_Actions == null) return false;

            bool success = true;
            foreach (var action in m_Actions)
            {
                if (action != null)
                {
                    success &= action.SetGameObject(gameObj);
                }
            }
            return success;
        }
    }
}
