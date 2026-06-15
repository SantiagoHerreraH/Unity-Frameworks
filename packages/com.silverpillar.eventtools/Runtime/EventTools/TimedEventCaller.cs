using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using SilverPillar.Core;
using Sirenix.Serialization;
using Sirenix.OdinInspector;

namespace SilverPillar.EventTools
{
    public class TimedEventCaller : SerializedMonoBehaviour
    {
        public enum WhenToAutoCallEvent
        {
            DontAutoCall,
            OnAwake,
            OnStart,
            OnEnable,
            OnUpdate,
            OnFixedUpdate,
            OnLateUpdate
        }

        [SerializeField]
        private WhenToAutoCallEvent m_WhenToAutoCall = WhenToAutoCallEvent.DontAutoCall;

        [OdinSerialize, ShowInInspector, Tooltip("Will always clamp to 0.")]
        private ICachedScore m_TimeToTrigger;

        [SerializeField, ReadOnly]
        private float m_CurrentTime;

        [SerializeField]
        private UnityEvent m_Event;

        private Coroutine m_EventCoroutine;
        private bool m_Initialized;

        private void Awake()
        {
            Initialize();

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
            Initialize();

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
            StopEventTimer();
        }

        private void OnDestroy()
        {
            StopEventTimer();
        }

        private void Initialize()
        {
            if (m_Initialized)
            {
                return;
            }

            m_TimeToTrigger?.SetGameObject(gameObject);

            m_Initialized = true;
        }

        public void ExecuteEvent()
        {
            Initialize();

            if (m_EventCoroutine != null)
            {
                return;
            }

            m_EventCoroutine = StartCoroutine(ExecuteEventCoroutine());
        }

        public void StopEventTimer()
        {
            if (m_EventCoroutine != null)
            {
                StopCoroutine(m_EventCoroutine);
                m_EventCoroutine = null;
            }

            m_CurrentTime = 0f;
        }

        public void RestartEventTimer()
        {
            StopEventTimer();
            ExecuteEvent();
        }

        private IEnumerator ExecuteEventCoroutine()
        {
            m_CurrentTime = 0f;

            float timeToTrigger = GetTimeToTrigger();

            while (m_CurrentTime < timeToTrigger)
            {
                m_CurrentTime += Time.deltaTime;
                yield return null;
            }

            m_CurrentTime = 0f;
            m_EventCoroutine = null;

            m_Event?.Invoke();
        }

        private float GetTimeToTrigger()
        {
            if (m_TimeToTrigger == null)
            {
                return 0f;
            }

            return Mathf.Max(0f, m_TimeToTrigger.CalculateScore());
        }
    }
}