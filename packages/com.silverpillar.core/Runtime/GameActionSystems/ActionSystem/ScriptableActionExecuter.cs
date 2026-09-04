using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SilverPillar.Core
{
    public class ScriptableActionExecuter : MonoBehaviour
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
        [SerializeField]
        private List<ScriptableAction> m_Actions = new();

        private List<IAction> m_ActionInstances = new();


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
                    foreach (var action in m_ActionInstances)
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

        public void ChangeAction(ScriptableAction action)
        {
            m_Actions.Clear();
            m_Actions.Add(action);
            m_Initialized = false;
            Initialize();
        }

        public void ChangeActions(List<ScriptableAction> actions)
        {

            m_Actions.Clear();
            m_Actions.AddRange(actions);
            m_Initialized = false;
            Initialize();
        }

        public void AddAction(ScriptableAction action)
        {
            m_Actions.Add(action);
            m_ActionInstances.Add(action.Clone());
            m_ActionInstances.Last().SetGameObject(gameObject);
        }

        public void RemoveAction(ScriptableAction action)
        {
            m_Actions.Remove(action);

            m_Initialized = false;
            Initialize();
        }

        public void StartActions()
        {
            Initialize();
            m_AreActionsExecuting = true;
            m_CurrentTime = 0;

            foreach (var action in m_ActionInstances)
            {
                action.StartAction();
            }
        }

        public void EndActions()
        {
            if (m_AreActionsExecuting)
            {
                foreach (var action in m_ActionInstances)
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
                m_ActionInstances.Clear();
                foreach (var item in m_Actions)
                {
                    m_ActionInstances.Add(item.Clone());
                }
                foreach (var action in m_ActionInstances)
                {
                    action.SetGameObject(gameObject);
                }
                m_Initialized = true;
            }
        }
    }
}
