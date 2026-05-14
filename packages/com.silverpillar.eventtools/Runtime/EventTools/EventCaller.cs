using UnityEngine;
using UnityEngine.Events;

namespace SilverPillar.EventTools
{
    public class EventCaller : MonoBehaviour
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
            m_Event?.Invoke();
        }
    }
}
