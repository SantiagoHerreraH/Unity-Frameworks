using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class CachedSequenceModifier_Action : IAction
    {
        public enum CustomTargetType
        {
            Self,
            Custom
        }

        public enum OperationTypeOnStart
        {
            Add,
            Clone
        }

        public enum OperationTypeOnEnd
        {
            Remove,
            None
        }

        [FoldoutGroup("Targets")]
        [SerializeField]
        private CustomTargetType m_From;
        [FoldoutGroup("Targets")]
        [SerializeField, ShowIf(nameof(m_From), CustomTargetType.Custom)]
        private SimpleCachedGameActionSequence m_CustomFrom;
        [FoldoutGroup("Targets")]
        [SerializeField]
        private CustomTargetType m_To;
        [FoldoutGroup("Targets")]
        [SerializeField, ShowIf(nameof(m_To), CustomTargetType.Custom)]
        private SimpleCachedGameActionSequence m_CustomTo;

        [FoldoutGroup("Operations")]
        [SerializeField]
        private OperationTypeOnStart m_OperationTypeOnStart;
        [FoldoutGroup("Operations")]
        [SerializeField]
        private OperationTypeOnEnd m_OperationTypeOnEnd;

        private SimpleCachedGameActionSequence m_Self = null;
        private List<ICachedGameAction> m_GameActions = new();

        private SimpleCachedGameActionSequence m_FromSequence = null;
        private SimpleCachedGameActionSequence m_ToSequence = null;

        public IAction Clone()
        {
            var clone = new CachedSequenceModifier_Action
            {
                m_From = this.m_From,
                m_CustomFrom = this.m_CustomFrom,
                m_To = this.m_To,
                m_CustomTo = this.m_CustomTo,
                m_OperationTypeOnStart = this.m_OperationTypeOnStart,
                m_OperationTypeOnEnd = this.m_OperationTypeOnEnd,

                m_Self = this.m_Self,
                m_FromSequence = this.m_FromSequence,
                m_ToSequence = this.m_ToSequence,

                m_GameActions = new List<ICachedGameAction>(this.m_GameActions)
            };

            return clone;
        }


        public GameObject GetGameObject()
        {
            return m_Self != null ? m_Self.gameObject : null;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            if (gameObj != null)
            {
                return gameObj.TryGetComponent<SimpleCachedGameActionSequence>(out m_Self);
            }
            return false;
        }

        public void StartAction()
        {
            m_FromSequence = (m_From == CustomTargetType.Self) ? m_Self : m_CustomFrom;
            m_ToSequence = (m_To == CustomTargetType.Self) ? m_Self : m_CustomTo;

            if (m_FromSequence == null || m_ToSequence == null) return;

            m_GameActions.Clear();

            List<ICachedGameAction> sourceActions = new();
            sourceActions.AddRange(m_FromSequence.ActionsToExecuteBeforeSequence);
            sourceActions.AddRange(m_FromSequence.ActionsInSequence);
            sourceActions.AddRange(m_FromSequence.ActionsToExecuteAfterSequence);

            foreach (var item in sourceActions)
            {
                ICachedGameAction actionToAdd = null;

                switch (m_OperationTypeOnStart)
                {
                    case OperationTypeOnStart.Add:
                        actionToAdd = item;
                        m_ToSequence.AddActionToSequence(item);
                        break;
                    case OperationTypeOnStart.Clone:
                        actionToAdd = m_ToSequence.CloneActionToSequence(item);
                        break;
                    default:
                        break;
                }

                if (actionToAdd != null)
                {
                    m_GameActions.Add(actionToAdd);
                }
            }
        }

        public void UpdateAction()
        {
        }

        public void EndAction()
        {
            if (m_ToSequence == null) return;

            if (m_OperationTypeOnEnd == OperationTypeOnEnd.Remove)
            {
                foreach (var item in m_GameActions)
                {
                    m_ToSequence.RemoveActionFromSequence(item);
                }
            }

            m_GameActions.Clear();
        }
    }
}
