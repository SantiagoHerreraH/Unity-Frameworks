using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SilverPillar.EventTools
{
    public class EventSequence : MonoBehaviour
    {
        public enum WhenToAutoCall
        {
            DontAutoCall,
            OnAwake,
            OnStart,
            OnEnable,
            OnDisable,
            OnDestroy
        }

        [Title("Auto Call")]
        [SerializeField]
        private WhenToAutoCall m_WhenToStartSequence;

        [SerializeField]
        private WhenToAutoCall m_WhenToEndSequence;

        [Title("Locking")]
        [InfoBox("If the Sequence is locked, it won't trigger other events if you call any function")]
        [SerializeField]
        private bool m_IsLocked = false;

        [Title("Sequence")]
        [SerializeField]
        private List<UnityEvent> m_EventSequence = new();

        private int m_CurrentIndex = 0;

        // ──────────────────────────────────────────────────────────────
        // Unity lifecycle — autocalling
        // ──────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (m_WhenToStartSequence == WhenToAutoCall.OnAwake) StartSequence();
            if (m_WhenToEndSequence == WhenToAutoCall.OnAwake) EndSequence();
        }

        private void Start()
        {
            if (m_WhenToStartSequence == WhenToAutoCall.OnStart) StartSequence();
            if (m_WhenToEndSequence == WhenToAutoCall.OnStart) EndSequence();
        }

        private void OnEnable()
        {
            if (m_WhenToStartSequence == WhenToAutoCall.OnEnable) StartSequence();
            if (m_WhenToEndSequence == WhenToAutoCall.OnEnable) EndSequence();
        }

        private void OnDisable()
        {
            if (m_WhenToStartSequence == WhenToAutoCall.OnDisable) StartSequence();
            if (m_WhenToEndSequence == WhenToAutoCall.OnDisable) EndSequence();
        }

        private void OnDestroy()
        {
            if (m_WhenToStartSequence == WhenToAutoCall.OnDestroy) StartSequence();
            if (m_WhenToEndSequence == WhenToAutoCall.OnDestroy) EndSequence();
        }

        // ──────────────────────────────────────────────────────────────
        // Locking
        // ──────────────────────────────────────────────────────────────

        public void Lock() => m_IsLocked = true;
        public void Unlock() => m_IsLocked = false;
        public void ToggleLock() => m_IsLocked = !m_IsLocked;
        public bool IsLocked() => m_IsLocked;

        // ──────────────────────────────────────────────────────────────
        // Sequence control
        // ──────────────────────────────────────────────────────────────

        public void TriggerNextEvent()
        {
            if (m_IsLocked || m_EventSequence.Count == 0) return;

            m_EventSequence[m_CurrentIndex].Invoke();
            m_CurrentIndex = (m_CurrentIndex + 1) % m_EventSequence.Count;
        }

        public void EndSequence()
        {
            if (m_IsLocked || m_EventSequence.Count == 0) return;

            m_CurrentIndex = m_EventSequence.Count - 1;
            m_EventSequence[m_CurrentIndex].Invoke();
            m_CurrentIndex = (m_CurrentIndex + 1) % m_EventSequence.Count;
        }

        public void StartSequence()
        {
            if (m_IsLocked || m_EventSequence.Count == 0) return;

            m_CurrentIndex = 0;
            m_EventSequence[m_CurrentIndex].Invoke();
            m_CurrentIndex = (m_CurrentIndex + 1) % m_EventSequence.Count;
        }
    }
}
