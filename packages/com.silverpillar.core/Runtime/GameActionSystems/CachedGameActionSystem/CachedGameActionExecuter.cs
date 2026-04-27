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

            // Constructor para facilitar la creación de datos
            public ActionData(ICachedGameAction action, int priority = 0)
            {
                GameAction = action;
                PriorityNumber = priority;
            }
        }

        public enum WhenToAutoCallActions
        {
            DontAutoCall,
            OnStart,
            OnEnable,
            OnDisable,
            OnUpdate,
            OnFixedUpdate
        }

        [Title("Auto Call Settings")]
        [SerializeField]
        private WhenToAutoCallActions m_WhenToAutoCallActions = WhenToAutoCallActions.DontAutoCall;

        [Title("Actions")]
        [OdinSerialize, ShowInInspector]
        private List<ActionData> m_Actions = new List<ActionData>();

        public List<ActionData> Actions => m_Actions;

        private bool m_Initialized = false;

        private void Awake()
        {
            Initialize();
        }

        private void Start()
        {
            if (m_WhenToAutoCallActions == WhenToAutoCallActions.OnStart) Execute();
        }

        private void OnEnable()
        {
            if (m_WhenToAutoCallActions == WhenToAutoCallActions.OnEnable) Execute();
        }

        private void OnDisable()
        {
            if (m_WhenToAutoCallActions == WhenToAutoCallActions.OnDisable) Execute();
        }

        private void Update()
        {
            if (m_WhenToAutoCallActions == WhenToAutoCallActions.OnUpdate) Execute();
        }

        private void FixedUpdate()
        {
            if (m_WhenToAutoCallActions == WhenToAutoCallActions.OnFixedUpdate) Execute();
        }

        private void Initialize()
        {
            if (m_Initialized) return;

            // IMPORTANTE: Usamos un for indexado porque ActionData es una struct.
            // foreach crea una copia y no modificaría el elemento original en la lista.
            for (int i = 0; i < m_Actions.Count; i++)
            {
                if (m_Actions[i].GameAction != null)
                {
                    m_Actions[i].GameAction.SetGameObject(gameObject);
                }
            }

            m_Initialized = true;
        }

        public void AddGameAction(ICachedGameAction gameAction, int priorityNumber = 0)
        {
            Initialize();
            gameAction.SetGameObject(gameObject);
            m_Actions.Add(new ActionData(gameAction, priorityNumber));
        }

        public void RemoveGameAction(ICachedGameAction gameAction)
        {
            m_Actions.RemoveAll(x => x.GameAction == gameAction);
        }

        public ICachedGameAction CloneGameAction(ICachedGameAction gameAction, int priorityNumber = 0)
        {
            Initialize();
            var clone = gameAction.Clone();
            clone.SetGameObject(gameObject);
            m_Actions.Add(new ActionData(clone, priorityNumber));

            return clone;
        }

        [Button, Title("Manual Execution")]
        public void Execute()
        {
            if (!m_Initialized) Initialize();

            foreach (var item in m_Actions)
            {
                item.GameAction?.Execute();
            }
        }
    }
}
