using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pillar
{
    [CreateAssetMenu(fileName = "CachedGameAction", menuName = "Scriptable Objects/CachedGameAction")]
    public class CachedGameAction : SaveableScriptableObject
    {
        [OdinSerialize]
        private ICachedGameAction m_CachedGameAction;

        public ICachedGameAction Clone(GameObject gameObj)
        {
            var result = m_CachedGameAction.Clone();
            result.SetGameObject(gameObj);
            return result;
        }
    }

    [Serializable]
    public class CachedGameActionGroup
    {
        [SerializeField]
        private List<SO_Ref<CachedGameAction>> m_Actions = new();
        private List<ICachedGameAction> m_Actions_Instances = new();

        private GameObject m_GameObject;

        public void SetGameObject(GameObject gameObject)
        {
            m_GameObject = gameObject;

            if (m_Actions.Count == 0)
            {
                foreach (var item in m_Actions)
                {
                    m_Actions_Instances.Add(item.Get().Clone(gameObject));
                }
            }
            else
            {
                foreach (var item in m_Actions_Instances)
                {
                    item.SetGameObject(gameObject);
                }
            }


        }

        public void Execute()
        {
            foreach (var item in m_Actions_Instances)
            {
                item.Execute();
            }
        }

        public void AddAction(SO_Ref<CachedGameAction> cachedGameAction)
        {
            if (!m_Actions.Contains(cachedGameAction))
            {
                m_Actions.Add(cachedGameAction);
                m_Actions_Instances.Add(cachedGameAction.Get().Clone(m_GameObject));
            }
        }

        public void RemoveAction(SO_Ref<CachedGameAction> cachedGameAction)
        {
            int index = m_Actions.IndexOf(cachedGameAction);
            if (index >= 0)
            {
                m_Actions.RemoveAt(index);
                m_Actions_Instances.RemoveAt(index);
            }
        }
    }
}

