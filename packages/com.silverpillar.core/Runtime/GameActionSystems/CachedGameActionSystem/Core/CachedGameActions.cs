using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class CachedGameActions : ICachedGameAction
    {
        [OdinSerialize, ShowInInspector]
        private List<ICachedGameAction> m_Actions = new List<ICachedGameAction>();
        private GameObject m_GameObject;

        public ICachedGameAction Clone()
        {
            var clone = new CachedGameActions();
            clone.m_GameObject = m_GameObject;
            foreach (var action in m_Actions)
            {
                clone.m_Actions.Add(action?.Clone());
            }
            return clone;
        }

        public void Execute()
        {
            if (m_Actions == null) return;

            foreach (var action in m_Actions)
            {
                action?.Execute();
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
            foreach (var action in m_Actions)
            {
                if (action != null)
                {
                    if (!action.SetGameObject(gameObj))
                        allSucceeded = false;
                }
            }
            return allSucceeded;
        }
    }
}
