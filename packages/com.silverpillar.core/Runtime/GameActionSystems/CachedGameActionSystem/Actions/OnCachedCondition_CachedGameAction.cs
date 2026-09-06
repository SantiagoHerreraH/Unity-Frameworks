using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class OnCachedCondition_CachedGameAction : ICachedGameAction
    {
        [OdinSerialize, ShowInInspector]
        private ICachedCondition m_Condition;
        [OdinSerialize, ShowInInspector]
        private ICachedGameAction m_Action;
        private GameObject m_Self;

        public OnCachedCondition_CachedGameAction() { }
        public OnCachedCondition_CachedGameAction(OnCachedCondition_CachedGameAction other)
        {
            m_Condition = other.m_Condition.Clone();
            m_Action = other.m_Action.Clone();
            m_Self = other.m_Self;
        }

        public ICachedGameAction Clone()
        {
            return new OnCachedCondition_CachedGameAction(this);
        }

        public void Execute()
        {
            if (m_Condition.IsFulfilled())
            {
                m_Action.Execute();
            }
        }

        public GameObject GetGameObject()
        {
            return m_Self;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            m_Self = gameObj;
            bool allGood = m_Action.SetGameObject(gameObj);
            allGood &= m_Condition.SetGameObject(gameObj);
            return allGood;
        }
    }
}
