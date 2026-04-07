using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Core
{
    public class SimpleActionController : SerializedMonoBehaviour
    {
        public enum WhenToAutoCallStartActions
        {
            OnStart,
            OnEnable,
            None
        }

        public enum WhenToAutoCallEndActions
        {
            OnDisable,
            OnDestroy,
            AfterTimeEnds,
            None
        }

        [FoldoutGroup("Auto Calling")]
        [SerializeField]
        private WhenToAutoCallStartActions m_WhenToAutoCallStartActions = WhenToAutoCallStartActions.None;
        [FoldoutGroup("Auto Calling")]
        [SerializeField]
        private WhenToAutoCallEndActions m_WhenToAutoCallEndActions = WhenToAutoCallEndActions.None; 
        [FoldoutGroup("Auto Calling")]
        [SerializeField, ShowIf(nameof(m_WhenToAutoCallEndActions), WhenToAutoCallEndActions.AfterTimeEnds)]
        private float m_Time;
        private float m_CurrentTime = 0f;


        [FoldoutGroup("Actions")]
        [OdinSerialize, ShowInInspector]
        private List<IAction> m_Actions = new();


        [FoldoutGroup("Optimizations")]
        [SerializeField, Tooltip("There are times when you won't need actions to call update action. Set this false so no unneccessary calls are made")]
        private bool m_CallUpdateActions = true;

        private bool m_Initialized = false;
        private bool m_AreActionsExecuting = false;

        private void Start()
        {
            if (m_WhenToAutoCallStartActions == WhenToAutoCallStartActions.OnStart)
            {
                StartActions();
            }
        }

        private void OnEnable()
        {
            if (m_WhenToAutoCallStartActions == WhenToAutoCallStartActions.OnEnable)
            {
                StartActions();
            }
        }

        private void OnDisable()
        {
            if (m_WhenToAutoCallEndActions == WhenToAutoCallEndActions.OnDisable)
            {
                EndActions();
            }
        }

        private void OnDestroy()
        {
            if (m_WhenToAutoCallEndActions == WhenToAutoCallEndActions.OnDestroy)
            {
                EndActions();
            }
        }

        private void Update()
        {
            if (m_AreActionsExecuting)
            {
                if (m_CallUpdateActions)
                {
                    foreach (var action in m_Actions)
                    {
                        action.UpdateAction();
                    }
                }

                if (m_WhenToAutoCallEndActions == WhenToAutoCallEndActions.AfterTimeEnds)
                {
                    m_CurrentTime += Time.deltaTime;

                    if (m_CurrentTime >= m_Time)
                    {
                        EndActions();
                    }
                }
            }
            
        }

        public void StartActions()
        {
            Initialize();
            m_AreActionsExecuting = true;
            m_CurrentTime = 0;

            foreach (var action in m_Actions)
            {
                action.StartAction();
            }
        }

        public void EndActions()
        {
            if (m_AreActionsExecuting)
            {
                foreach (var action in m_Actions)
                {
                    action.EndAction();
                }

                m_AreActionsExecuting = false;
            }
        }

        private void Initialize()
        {
            if (!m_Initialized)
            {
                foreach (var action in m_Actions)
                {
                    action.SetGameObject(gameObject);
                }
                m_Initialized = true;
            }
        }
    }
}
