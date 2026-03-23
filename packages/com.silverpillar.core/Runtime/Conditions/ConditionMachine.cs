using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Core
{
    public class ConditionMachine : SerializedMonoBehaviour
    {
        [OdinSerialize, ShowInInspector]
        private Dictionary<ConditionTag, ICondition> m_Conditions = new();

        public bool IsFulfilled(ConditionTag conditionTag)
        {
            if (m_Conditions.ContainsKey(conditionTag))
            {
                return m_Conditions[conditionTag].IsFulfilled(gameObject);
            }

            return false;
        }
    }
}
