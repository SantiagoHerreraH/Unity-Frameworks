using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SilverPillar.Core
{
    public class ScriptableObjectCachedGameActionMachine : MonoBehaviour
    {
        [Serializable]
        public class ActionData
        {
            public ActionName ActionName;

            [SerializeField]
            private CachedConditionData m_Conditions = new();

            [SerializeField]
            private CachedGameActionGroup m_Actions = new();


            [SerializeField]
            private UnityEvent m_OnExecute = new();

            public void SetGameObject(GameObject gameObject)
            {
                m_Conditions.SetGameObject(gameObject);
                m_Actions.SetGameObject(gameObject);
            }

            public bool Execute()
            {
                if (m_Conditions.IsFulfilled())
                {
                    m_Actions.Execute();
                    m_OnExecute.Invoke();
                    return true;
                }

                return false;
            }

            public void AddCondition(SO_Ref<CachedCondition> cachedCondition)
            {
                m_Conditions.AddCondition(cachedCondition);
            }

            public void RemoveCondition(SO_Ref<CachedCondition> cachedCondition)
            {
                m_Conditions.RemoveCondition(cachedCondition);
            }

            public void AddAction(SO_Ref<CachedGameAction> cachedGameAction)
            {
                m_Actions.AddAction(cachedGameAction);
            }

            public void RemoveAction(SO_Ref<CachedGameAction> cachedGameAction)
            {
                m_Actions.RemoveAction(cachedGameAction);
            }

        }

        public enum WhenToExecute
        {
            Update,
            FixedUpdate,
            LateUpdate,
            Start,
            OnEnable,
            None
        }

        [Title("Execution Settings")]
        [SerializeField]
        private WhenToExecute m_WhenToExecute = WhenToExecute.Update;

        [Title("Conditions")]
        [SerializeField]
        private CachedConditionData m_ConditionsToCheckAllActions = new();

        [Title("Actions")]
        [SerializeField]
        private List<ActionData> m_ActionData = new();

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            m_ConditionsToCheckAllActions.SetGameObject(gameObject);

            foreach (var item in m_ActionData)
            {
                item.SetGameObject(gameObject);
            }

            if (m_WhenToExecute == WhenToExecute.Start)
            {
                Execute();
            }
        }
        private void OnEnable()
        {
            if (m_WhenToExecute == WhenToExecute.OnEnable)
            {
                Execute();
            }
        }

        void Update()
        {
            if (m_WhenToExecute == WhenToExecute.Update)
            {
                Execute();
            }
        }

        private void FixedUpdate()
        {
            if (m_WhenToExecute == WhenToExecute.FixedUpdate)
            {
                Execute();
            }
        }

        private void LateUpdate()
        {
            if (m_WhenToExecute == WhenToExecute.LateUpdate)
            {
                Execute();
            }
        }

        public void Execute()
        {
            if (m_ConditionsToCheckAllActions.IsFulfilled())
            {
                foreach (var item in m_ActionData)
                {
                    if (item.Execute())
                    {
                        return;
                    }
                }
            }
        }

        public void AddCondition(ActionName actionName, SO_Ref<CachedCondition> cachedCondition)
        {
            ActionData data = Get(actionName);
            data?.AddCondition(cachedCondition);
        }

        public void RemoveCondition(ActionName actionName, SO_Ref<CachedCondition> cachedCondition)
        {
            ActionData data = Get(actionName);
            data?.RemoveCondition(cachedCondition);
        }

        public void AddAction(ActionName actionName, SO_Ref<CachedGameAction> cachedGameAction)
        {
            ActionData data = Get(actionName);
            data?.AddAction(cachedGameAction);
        }

        public void RemoveAction(ActionName actionName, SO_Ref<CachedGameAction> cachedGameAction)
        {
            ActionData data = Get(actionName);
            data?.RemoveAction(cachedGameAction);
        }

        private ActionData Get(ActionName name)
        {
            foreach (var item in m_ActionData)
            {
                if (item.ActionName == name)
                {
                    return item;
                }
            }

            return null;
        }
    }
}


