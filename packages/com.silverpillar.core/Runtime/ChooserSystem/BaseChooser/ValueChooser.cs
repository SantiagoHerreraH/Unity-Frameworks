using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace SilverPillar.Core
{
    public class ValueChooser<T> : SerializedMonoBehaviour, IChoose
    {
        [Serializable]
        public class ValueEvent : UnityEvent<T> { }

        [Title("Settings")]
        [SerializeField]
        private WhenToAutomaticallyChoose m_WhenToAutomaticallyChoose;

        [Title("Data")]
        [OdinSerialize, ShowInInspector]
        private ChooseValue<T> m_ChooseValue;

        private enum EventOrder
        {
            All_Chosen_NotChosen,
            All_NotChosen_Chosen,
            Chosen_NotChosen_All,
            Chosen_All_NotChosen,
            NotChosen_Chosen_All,
            NotChosen_All_Chosen
        }

        [Title("Event Order")]
        [SerializeField]
        private EventOrder m_EventOrder;

        [Title("Event On Chosen")]
        [SerializeField]
        private ValueEvent m_OnValueChosen;

        [Title("Event On Not Chosen")]
        [SerializeField]
        private ValueEvent m_OnValueNotChosen;

        [Title("Events On All")]
        [SerializeField]
        private ValueEvent m_OnChosenCalled;

        private void Awake()
        {
            if (m_WhenToAutomaticallyChoose == WhenToAutomaticallyChoose.OnAwake)
                Choose();
        }

        private void Start()
        {
            if (m_WhenToAutomaticallyChoose == WhenToAutomaticallyChoose.OnStart)
                Choose();
        }

        private void OnEnable()
        {
            if (m_WhenToAutomaticallyChoose == WhenToAutomaticallyChoose.OnEnable)
                Choose();
        }

        private void Update()
        {
            if (m_WhenToAutomaticallyChoose == WhenToAutomaticallyChoose.OnUpdate)
                Choose();
        }

        private void FixedUpdate()
        {
            if (m_WhenToAutomaticallyChoose == WhenToAutomaticallyChoose.OnFixedUpdate)
                Choose();
        }

        private void LateUpdate()
        {
            if (m_WhenToAutomaticallyChoose == WhenToAutomaticallyChoose.OnLateUpdate)
                Choose();
        }

        private void OnDisable()
        {
            if (m_WhenToAutomaticallyChoose == WhenToAutomaticallyChoose.OnDisable)
                Choose();
        }
        public void Choose()
        {
            if (m_ChooseValue == null)
                return;

            m_ChooseValue.Choose();

            var chosen = m_ChooseValue.GetChosenValues();
            var notChosen = m_ChooseValue.GetNotChosenValues();

            void InvokeChosen()
            {
                if (chosen == null || m_OnValueChosen == null)
                    return;

                foreach (var value in chosen)
                    m_OnValueChosen?.Invoke(value);
            }

            void InvokeNotChosen()
            {
                if (notChosen == null || m_OnValueNotChosen == null)
                    return;

                foreach (var value in notChosen)
                    m_OnValueNotChosen?.Invoke(value);
            }

            void InvokeAll()
            {
                if (m_OnChosenCalled == null)
                {
                    return;
                }

                if (chosen != null)
                {
                    foreach (var value in chosen)
                        m_OnChosenCalled?.Invoke(value);
                }

                if (notChosen != null)
                {
                    foreach (var value in notChosen)
                        m_OnChosenCalled?.Invoke(value);
                }
            }

            switch (m_EventOrder)
            {
                case EventOrder.All_Chosen_NotChosen:
                    InvokeAll();
                    InvokeChosen();
                    InvokeNotChosen();
                    break;

                case EventOrder.All_NotChosen_Chosen:
                    InvokeAll();
                    InvokeNotChosen();
                    InvokeChosen();
                    break;

                case EventOrder.Chosen_NotChosen_All:
                    InvokeChosen();
                    InvokeNotChosen();
                    InvokeAll();
                    break;

                case EventOrder.Chosen_All_NotChosen:
                    InvokeChosen();
                    InvokeAll();
                    InvokeNotChosen();
                    break;

                case EventOrder.NotChosen_Chosen_All:
                    InvokeNotChosen();
                    InvokeChosen();
                    InvokeAll();
                    break;

                case EventOrder.NotChosen_All_Chosen:
                    InvokeNotChosen();
                    InvokeAll();
                    InvokeChosen();
                    break;
            }
        }
    }
}