using SilverPillar.Core;
using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.Serialization;
using Sirenix.OdinInspector;

namespace SilverPillar.Notifier
{
    [Serializable]
    public class AddChannelNotifierActionsToExecute_CachedGameAction : ICachedGameAction
    {
        public enum WhereToAddActions
        {
            ActionsToExecuteOnListener,
            ActionsToExecuteOnNotifier
        }

        [OdinSerialize, ShowInInspector]
        private List<ICachedGameAction> m_GameActionsToAdd;

        [SerializeField]
        private WhereToAddActions m_WhereToAddActions;

        [SerializeField]
        private SelfType m_OnWhichNotifierToAddActions;

        [SerializeField, ShowIf(nameof(m_OnWhichNotifierToAddActions), SelfType.CustomGameObject)]
        private ChannelNotifier m_ChannelNotifier;

        private GameObject m_GameObject;

        public ICachedGameAction Clone()
        {
            return new AddChannelNotifierActionsToExecute_CachedGameAction
            {
                m_GameActionsToAdd = CloneGameActions(m_GameActionsToAdd),
                m_WhereToAddActions = m_WhereToAddActions,
                m_OnWhichNotifierToAddActions = m_OnWhichNotifierToAddActions,
                m_ChannelNotifier = m_ChannelNotifier,
                m_GameObject = m_GameObject
            };
        }

        public void Execute()
        {
            ChannelNotifier notifier = GetChannelNotifier();

            if (notifier == null || m_GameActionsToAdd == null)
                return;

            switch (m_WhereToAddActions)
            {
                case WhereToAddActions.ActionsToExecuteOnListener:
                    notifier.AddActionsToExecuteOnListener(m_GameActionsToAdd);
                    break;

                case WhereToAddActions.ActionsToExecuteOnNotifier:
                    notifier.AddActionsToExecuteOnSelf(m_GameActionsToAdd);
                    break;
            }
        }

        public GameObject GetGameObject()
        {
            ChannelNotifier notifier = GetChannelNotifier();

            if (notifier != null)
                return notifier.gameObject;

            return m_GameObject;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            m_GameObject = gameObj;

            return GetChannelNotifier() != null;
        }

        private ChannelNotifier GetChannelNotifier()
        {
            switch (m_OnWhichNotifierToAddActions)
            {
                case SelfType.ThisGameObject:
                    if (m_GameObject == null)
                        return null;

                    return m_GameObject.GetComponent<ChannelNotifier>();

                case SelfType.CustomGameObject:
                    return m_ChannelNotifier;

                default:
                    if (m_GameObject == null)
                        return null;

                    return m_GameObject.GetComponent<ChannelNotifier>();
            }
        }

        private static List<ICachedGameAction> CloneGameActions(List<ICachedGameAction> actions)
        {
            if (actions == null)
                return null;

            List<ICachedGameAction> clones = new(actions.Count);

            foreach (ICachedGameAction action in actions)
            {
                if (action == null)
                {
                    continue;
                }

                clones.Add(action.Clone());
            }

            return clones;
        }
    }
}