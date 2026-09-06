using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class ActionGroup : IAction
    {
        [OdinSerialize, ShowInInspector]
        private List<IAction> m_Actions = new List<IAction>();

        private GameObject m_GameObject;

        public ActionGroup()
        {
        }

        public ActionGroup(ActionGroup other)
        {
            if (other == null) return;

            this.m_GameObject = other.m_GameObject;

            if (other.m_Actions != null)
            {
                this.m_Actions = new List<IAction>();
                foreach (var action in other.m_Actions)
                {
                    this.m_Actions.Add(action != null ? action.Clone() : null);
                }
            }
        }

        public IAction Clone()
        {
            return new ActionGroup(this);
        }

        public void StartAction()
        {
            if (m_Actions == null) return;

            foreach (var action in m_Actions)
            {
                action?.StartAction();
            }
        }

        public void UpdateAction()
        {
            if (m_Actions == null) return;

            foreach (var action in m_Actions)
            {
                action?.UpdateAction();
            }
        }

        public void EndAction()
        {
            if (m_Actions == null) return;

            foreach (var action in m_Actions)
            {
                action?.EndAction();
            }
        }

        public GameObject GetGameObject()
        {
            return m_GameObject;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            m_GameObject = gameObj;
            bool allSucceeded = true;

            if (m_Actions != null)
            {
                foreach (var action in m_Actions)
                {
                    if (action != null)
                    {
                        if (!action.SetGameObject(gameObj))
                        {
                            allSucceeded = false;
                        }
                    }
                }
            }

            return allSucceeded;
        }
    }
}
