using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class CachedCondition_Condition : ICondition
    {
        [OdinSerialize]
        private ICachedCondition m_CachedCondition = null;

        public bool IsFulfilled(GameObject gameObj)
        {
            m_CachedCondition.SetGameObject(gameObj);
            return m_CachedCondition.IsFulfilled();
        }
    }
}
