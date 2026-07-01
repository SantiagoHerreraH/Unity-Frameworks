using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SilverPillar.Core
{
    public class QueuedAction : SerializedMonoBehaviour
    {
        public enum WhenToStartQueuingAction
        {
            OnAwake,
            OnStart,
            OnEnable
        }

        public enum WhenToStopQueuingAction
        {
            OnDisable,
            OnDestroy
        }

        [Serializable]
        public class Data
        {
            [Title("Settings")]
            [SerializeField]
            private Queue m_Queue;
            public Queue Queue => m_Queue;

            [SerializeField]
            private WhenToStartQueuingAction m_WhenToStartQueuingAction;
            public WhenToStartQueuingAction WhenToStartQueuingAction => m_WhenToStartQueuingAction;

            [SerializeField]
            private WhenToStopQueuingAction m_WhenToStopQueuingAction;
            public WhenToStopQueuingAction WhenToStopQueuingAction => m_WhenToStopQueuingAction;

            [OdinSerialize, ShowInInspector]
            private ICachedScore m_Priority;
            private float m_CurrentPriority;

            [SerializeField, Tooltip("Negative is infinite. Zero means execute once and do not requeue.")]
            private int m_NumberOfTimesToQueueAgainAfterExecution;

            [Title("Execution")]
            [OdinSerialize, ShowInInspector]
            private ICachedGameAction m_GameAction;

            [SerializeField]
            private UnityEvent<GameObject> m_OnGameAction;

            private GameObject m_Self;
            private int m_RemainingTimesToQueueAgainAfterExecution;
            private int m_CurrentQueueIndex = 0;

            public void Initialize(GameObject gameObj)
            {
                m_Self = gameObj;
                m_RemainingTimesToQueueAgainAfterExecution = m_NumberOfTimesToQueueAgainAfterExecution;

                m_GameAction?.SetGameObject(gameObj);
                m_Priority?.SetGameObject(gameObj);
            }

            public void Execute()
            {
                if (m_Self == null)
                {
                    return;
                }

                m_GameAction?.Execute();
                m_OnGameAction?.Invoke(m_Self);
            }

            public void SetCurrentQueueIndex(int currentQueueIndex)
            {
                m_CurrentQueueIndex = currentQueueIndex;
            }

            public float CalculatePriority()
            {
                if (m_Priority == null)
                {
                    return 0f;
                }

                m_CurrentPriority =  m_Priority.CalculateScore() + m_CurrentQueueIndex;

                return m_CurrentPriority;
            }

            public float GetPriority()
            {
                return m_CurrentPriority;
            }

            public float OffsetPriority(float offset)
            {
                return m_CurrentPriority + offset;
            }

            public bool ShouldQueueAgainAfterExecution()
            {
                if (m_Self == null)
                {
                    return false;
                }

                // Negative means infinite requeue.
                if (m_NumberOfTimesToQueueAgainAfterExecution < 0)
                {
                    return true;
                }

                if (m_RemainingTimesToQueueAgainAfterExecution <= 0)
                {
                    return false;
                }

                m_RemainingTimesToQueueAgainAfterExecution--;
                return true;
            }
        }

        [OdinSerialize, ShowInInspector]
        private List<Data> m_QueueActionData = new();

        private void Awake()
        {
            RegisterData(WhenToStartQueuingAction.OnAwake);
        }

        private void Start()
        {
            RegisterData(WhenToStartQueuingAction.OnStart);
        }

        private void OnEnable()
        {
            RegisterData(WhenToStartQueuingAction.OnEnable);
        }

        private void OnDisable()
        {
            UnregisterData(WhenToStopQueuingAction.OnDisable);
        }

        private void OnDestroy()
        {
            UnregisterData(WhenToStopQueuingAction.OnDestroy);
        }

        public void ExecuteNextInQueue(Queue queuedActionChannel)
        {
            QueueManager.Instance.ExecuteAndPop(queuedActionChannel);
        }

        private void RegisterData(WhenToStartQueuingAction when)
        {
            for (int i = 0; i < m_QueueActionData.Count; i++)
            {
                Data data = m_QueueActionData[i];

                if (data == null)
                {
                    Debug.LogError($"{nameof(QueuedAction)} has a null queue action data entry.", this);
                    continue;
                }

                if (data.WhenToStartQueuingAction != when)
                {
                    continue;
                }

                if (data.Queue == null)
                {
                    Debug.LogError($"{nameof(QueuedAction)} cannot register because the queue channel is null.", this);
                    continue;
                }

                data.Initialize(gameObject);

                if (QueueManager.Instance == null)
                {
                    Debug.LogError($"{nameof(QueueManager)} instance was not found.", this);
                    continue;
                }

                QueueManager.Instance.AddQueuedAction(data.Queue, data);
            }
        }

        private void UnregisterData(WhenToStopQueuingAction when)
        {
            for (int i = 0; i < m_QueueActionData.Count; i++)
            {
                Data data = m_QueueActionData[i];

                if (data == null)
                {
                    continue;
                }

                if (data.WhenToStopQueuingAction != when)
                {
                    continue;
                }

                if (data.Queue == null)
                {
                    Debug.LogError($"{nameof(QueuedAction)} cannot unregister because the queue channel is null.", this);
                    continue;
                }

                if (QueueManager.Instance == null)
                {
                    continue;
                }

                QueueManager.Instance.RemoveQueuedAction(data.Queue, data);
            }
        }
    }
}