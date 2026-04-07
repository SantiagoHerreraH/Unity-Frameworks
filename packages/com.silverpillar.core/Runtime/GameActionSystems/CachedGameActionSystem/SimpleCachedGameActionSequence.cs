using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Core
{
    public class SimpleCachedGameActionSequence : SerializedMonoBehaviour
    {
        [OdinSerialize, ShowInInspector]
        private List<ICachedGameAction> m_ActionsToExecuteBeforeSequence = new();
        [OdinSerialize, ShowInInspector]
        private List<ICachedGameAction> m_ActionsInSequence = new();
        [OdinSerialize, ShowInInspector]
        private List<ICachedGameAction> m_ActionsToExecuteAfterSequence = new();


        public List<ICachedGameAction> ActionsToExecuteBeforeSequence { get { return m_ActionsToExecuteBeforeSequence; } }
        public List<ICachedGameAction> ActionsInSequence { get { return m_ActionsInSequence; } }
        public List<ICachedGameAction> ActionsToExecuteAfterSequence { get { return m_ActionsToExecuteAfterSequence; } }

        private bool m_Initialized = false;

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (!m_Initialized)
            {
                foreach (var item in m_ActionsToExecuteBeforeSequence)
                {
                    item.SetGameObject(gameObject);
                }

                foreach (var item in m_ActionsInSequence)
                {
                    item.SetGameObject(gameObject);
                }

                foreach (var item in m_ActionsToExecuteAfterSequence)
                {
                    item.SetGameObject(gameObject);
                }

                m_Initialized = true;
            }
        }

        public void AddActionToSequence(ICachedGameAction gameAction)
        {
            Initialize();
            m_ActionsInSequence.Add(gameAction);
        }
        public void RemoveActionFromSequence(ICachedGameAction gameAction)
        {
            Initialize();
            m_ActionsInSequence.Remove(gameAction);
        }

        public ICachedGameAction CloneActionToSequence(ICachedGameAction gameAction)
        {
            Initialize();
            var clone = gameAction.Clone();
            clone.SetGameObject(gameObject);
            m_ActionsInSequence.Add(clone);

            return clone;
        }

        public void TriggerSequence()
        {
            foreach (var item in m_ActionsToExecuteBeforeSequence)
            {
                item.Execute();
            }

            foreach (var item in m_ActionsInSequence)
            {
                item.Execute();
            }

            foreach (var item in m_ActionsToExecuteAfterSequence)
            {
                item.Execute();
            }

        }
    }
}
