using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Core
{
    public class CachedGameActionExecuter : SerializedMonoBehaviour
    {
        [Serializable]
        public struct ActionData
        {
            [OdinSerialize, ShowInInspector]
            public ICachedGameAction GameAction;

            [HideInInspector]
            public int PriorityNumber;

            public ActionData(ICachedGameAction action, int priority = 0)
            {
                GameAction = action;
                PriorityNumber = priority;
            }
        }

        public enum WhenToAutoCallActions
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

        [Title("Auto Call Settings")]
        [SerializeField]
        private WhenToAutoCallActions m_WhenToAutoCallActions = WhenToAutoCallActions.DontAutoCall;

        [Title("Actions")]
        [SerializeField]
        private SelfType m_ExecuteOnWho;

        [SerializeField, ShowIf(nameof(m_ExecuteOnWho), SelfType.CustomGameObject), Tooltip("If this is null will get self")]
        private GameObject m_ChosenGameObject;

        [OdinSerialize, ShowInInspector]
        private List<ActionData> m_Actions = new List<ActionData>();

        public List<ActionData> Actions => m_Actions;

        private bool m_Initialized = false;

        private void Awake()
        {
            Initialize();

            if (m_WhenToAutoCallActions == WhenToAutoCallActions.OnAwake)
                Execute();
        }

        private void Start()
        {
            if (m_WhenToAutoCallActions == WhenToAutoCallActions.OnStart)
                Execute();
        }

        private void OnEnable()
        {
            if (m_WhenToAutoCallActions == WhenToAutoCallActions.OnEnable)
                Execute();
        }

        private void OnDisable()
        {
            if (m_WhenToAutoCallActions == WhenToAutoCallActions.OnDisable)
                Execute();
        }

        private void OnDestroy()
        {
            if (m_WhenToAutoCallActions == WhenToAutoCallActions.OnDestroy)
                Execute();
        }

        private void Update()
        {
            if (m_WhenToAutoCallActions == WhenToAutoCallActions.OnUpdate)
                Execute();
        }

        private void FixedUpdate()
        {
            if (m_WhenToAutoCallActions == WhenToAutoCallActions.OnFixedUpdate)
                Execute();
        }

        private void LateUpdate()
        {
            if (m_WhenToAutoCallActions == WhenToAutoCallActions.OnLateUpdate)
                Execute();
        }

        private bool Initialize()
        {
            if (m_Initialized)
                return true;

            if (m_ExecuteOnWho == SelfType.ThisGameObject || m_ChosenGameObject == null)
                m_ChosenGameObject = gameObject;

            SortActionsByPriority();

            m_Initialized = SetGameObject(m_ChosenGameObject);
            return m_Initialized;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            m_ChosenGameObject = gameObj;

            bool allGood = m_ChosenGameObject != null;

            for (int i = 0; i < m_Actions.Count; i++)
            {
                if (m_Actions[i].GameAction != null)
                    allGood &= m_Actions[i].GameAction.SetGameObject(m_ChosenGameObject);
                else
                    allGood = false;
            }

            return allGood;
        }

        public GameObject GetGameObject()
        {
            return m_ChosenGameObject;
        }

        public void AddGameAction(ICachedGameAction gameAction, int priorityNumber = 0)
        {
            if (gameAction == null)
                return;

            Initialize();

            gameAction.SetGameObject(m_ChosenGameObject);

            m_Actions.Add(new ActionData(gameAction, priorityNumber));
            SortActionsByPriority();
        }

        public void RemoveGameAction(ICachedGameAction gameAction)
        {
            if (gameAction == null)
                return;

            m_Actions.RemoveAll(x => x.GameAction == gameAction);
        }

        public void ClearGameActions()
        {
            m_Actions.Clear();
        }

        public ICachedGameAction CloneGameAction(ICachedGameAction gameAction, int priorityNumber = 0)
        {
            if (gameAction == null)
                return null;

            Initialize();

            ICachedGameAction clone = gameAction.Clone();

            if (clone == null)
                return null;

            clone.SetGameObject(m_ChosenGameObject);

            m_Actions.Add(new ActionData(clone, priorityNumber));
            SortActionsByPriority();

            return clone;
        }

        [Button, Title("Manual Execution")]
        public void Execute()
        {
            if (!Initialize())
                return;

            SortActionsByPriority();

            for (int i = 0; i < m_Actions.Count; i++)
            {
                m_Actions[i].GameAction?.Execute();
            }
        }

        public void Execute(GameObject gameObject)
        {
            SetGameObject(gameObject);
            Execute();
        }

        private void SortActionsByPriority()
        {
            m_Actions.Sort((a, b) => a.PriorityNumber.CompareTo(b.PriorityNumber));
        }
    }
}