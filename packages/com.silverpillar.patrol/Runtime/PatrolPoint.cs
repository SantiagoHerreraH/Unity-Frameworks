using SilverPillar.Core;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SilverPillar.Patrol
{
    public class PatrolPoint : SerializedMonoBehaviour
    {
        private static List<PatrolPoint> m_PatrolPoints = new();
        public static List<PatrolPoint> PatrolPoints => m_PatrolPoints;

        [Title("Settings")]
        [SerializeField]
        private bool m_IsRoot;
        public bool IsRoot => m_IsRoot;

        public enum ExecutionOrder
        {
            ActionThenEvent,
            EventThenAction
        }
        [Title("Actions")]
        [SerializeField]
        private ExecutionOrder m_ExecutionOrder;
        [OdinSerialize, ShowInInspector]
        private ICachedGameAction m_ActionOnEntityArrived;
        [SerializeField]
        private UnityEvent<GameObject> m_OnEntityArrived;

        private void Awake()
        {
            m_PatrolPoints.Add(this);
        }

        private void OnDestroy()
        {
            m_PatrolPoints.Remove(this);
        }

        public void OnEntityArrived(GameObject arrivedEntity)
        {
            switch (m_ExecutionOrder)
            {
                case ExecutionOrder.ActionThenEvent:
                    ExecuteAction(arrivedEntity);
                    m_OnEntityArrived?.Invoke(arrivedEntity);
                    break;
                case ExecutionOrder.EventThenAction:
                    m_OnEntityArrived?.Invoke(arrivedEntity);
                    ExecuteAction(arrivedEntity);
                    break;
                default:
                    break;
            }

        }

        private void ExecuteAction(GameObject arrivedEntity)
        {
            if (m_ActionOnEntityArrived != null && m_ActionOnEntityArrived.SetGameObject(arrivedEntity))
            {
                m_ActionOnEntityArrived.Execute();
            }
        }
    }
}
