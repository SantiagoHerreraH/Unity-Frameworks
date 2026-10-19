using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class Not_CachedCondition : ICachedCondition
    {
        [OdinSerialize]
        private ICachedCondition m_Condition;
        public ICachedCondition Clone()
        {
            return new Not_CachedCondition { m_Condition = m_Condition.Clone() };
        }

        public GameObject GetGameObject()
        {
            return m_Condition.GetGameObject(); 
        }

        public bool IsFulfilled()
        {
            return !m_Condition.IsFulfilled();
        }

        public bool SetGameObject(GameObject gameObj)
        {
            return m_Condition.SetGameObject(gameObj);
        }
    }
}
