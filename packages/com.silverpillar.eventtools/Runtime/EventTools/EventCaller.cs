using SilverPillar.Core;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.Events;

namespace SilverPillar.EventTools
{
    public class EventCaller : SerializedMonoBehaviour
    {
        public enum WhenToAutoCallEvent
        {
            DontAutoCall,
            OnAwake,
            OnStart,
            OnEnable,
            OnUpdate,
            OnFixedUpdate,
            OnLateUpdate,
            OnDisable,
            OnDestroy
        }

        [SerializeField]
        private WhenToAutoCallEvent m_WhenToAutoCall = WhenToAutoCallEvent.DontAutoCall;

        [SerializeField]
        private UnityEvent m_Event;

        [SerializeField]
        private bool m_CallOnce = true;

        [OdinSerialize, ShowInInspector, Tooltip("If null, wil be called once"), HideIf(nameof(m_CallOnce))]
        private ICachedScore m_EventTriggerNumber;
        private float m_TriggerNumber;

        public enum WhenToCalculateTriggerNumber
        {
            Once,
            EveryTimeBeforeTriggering
        }

        [SerializeField, HideIf(nameof(m_CallOnce))]
        private WhenToCalculateTriggerNumber m_WhenToCalculateTriggerNumber;

        private void Awake()
        {
            if (m_WhenToAutoCall == WhenToAutoCallEvent.OnAwake)
            {
                ExecuteEvent();
            }
        }

        private void Start()
        {
            if (m_WhenToAutoCall == WhenToAutoCallEvent.OnStart)
            {
                ExecuteEvent();
            }
        }

        private void OnEnable()
        {
            if (m_WhenToAutoCall == WhenToAutoCallEvent.OnEnable)
            {
                ExecuteEvent();
            }
        }

        private void Update()
        {
            if (m_WhenToAutoCall == WhenToAutoCallEvent.OnUpdate)
            {
                ExecuteEvent();
            }
        }

        private void FixedUpdate()
        {
            if (m_WhenToAutoCall == WhenToAutoCallEvent.OnFixedUpdate)
            {
                ExecuteEvent();
            }
        }

        private void LateUpdate()
        {
            if (m_WhenToAutoCall == WhenToAutoCallEvent.OnLateUpdate)
            {
                ExecuteEvent();
            }
        }

        private void OnDisable()
        {
            if (m_WhenToAutoCall == WhenToAutoCallEvent.OnDisable)
            {
                ExecuteEvent();
            }
        }

        private void OnDestroy()
        {
            if (m_WhenToAutoCall == WhenToAutoCallEvent.OnDestroy)
            {
                ExecuteEvent();
            }
        }

        public void ExecuteEvent()
        {
            if (m_CallOnce)
            {
                m_Event?.Invoke();
                return;
            }

            Initialize();

            if (m_WhenToCalculateTriggerNumber == WhenToCalculateTriggerNumber.EveryTimeBeforeTriggering)
            {
                m_TriggerNumber = m_EventTriggerNumber != null ? m_EventTriggerNumber.CalculateScore() : 1;
            }

            for (int i = 0; i < m_TriggerNumber; i++)
            {
                m_Event?.Invoke();
            }
        }

        private bool m_Initialized = false;
        private void Initialize()
        {
            if (m_Initialized)
            {
                return;
            }
            m_EventTriggerNumber?.SetGameObject(gameObject);

            if (m_WhenToCalculateTriggerNumber == WhenToCalculateTriggerNumber.Once)
            {
                m_TriggerNumber = m_EventTriggerNumber != null ? m_EventTriggerNumber.CalculateScore() : 1;
            }

            m_Initialized = true;
        }
    }
}
