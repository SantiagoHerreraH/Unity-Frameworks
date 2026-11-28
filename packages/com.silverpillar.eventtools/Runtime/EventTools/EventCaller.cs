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

        [Title("Auto Call")]
        [SerializeField]
        private WhenToAutoCallEvent m_WhenToAutoCall = WhenToAutoCallEvent.DontAutoCall;


        [Title("Block Call")]
        [SerializeField]
        private bool m_BlockCallIfComponentDeactivated;
        [SerializeField]
        private bool m_BlockCallIfGameObjectDeactivated;

        private bool m_ShowPrint => m_BlockCallIfComponentDeactivated || m_BlockCallIfGameObjectDeactivated;
        [SerializeField, ShowIf(nameof(m_ShowPrint))]
        private bool m_PrintWarningIfCalledWhenDeactivated;

        [Title("Number Of Calls")]
        [SerializeField]
        private bool m_CallOnce = true;

        [OdinSerialize, ShowInInspector, Tooltip("If null, wil be called once"), HideIf(nameof(m_CallOnce))]
        private ICachedScore m_EventTriggerNumber;
        private float m_TriggerNumber;

        [SerializeField, HideIf(nameof(m_CallOnce))]
        private WhenToCalculateTriggerNumber m_WhenToCalculateTriggerNumber;

        [Title("Event")]
        [SerializeField]
        private UnityEvent m_Event;


        public enum WhenToCalculateTriggerNumber
        {
            Once,
            EveryTimeBeforeTriggering
        }


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
            if ((m_BlockCallIfComponentDeactivated && !enabled) || (m_BlockCallIfGameObjectDeactivated && !gameObject.activeInHierarchy))
            {
                if (m_PrintWarningIfCalledWhenDeactivated)
                {
                    Debug.LogWarning($"{nameof(ExecuteEvent)} called in {nameof(EventCaller)} in gameobject {gameObject.name}, but component or gameobject is disabled and can't execute when disabled.");
                }
                return;
            }
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
