using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Core
{
    public class CachedConditionMachine : SerializedMonoBehaviour
    {
        [OdinSerialize, ShowInInspector]
        private Dictionary<ConditionTag, ICachedCondition> m_Conditions = new();

        private bool m_IsInitialized = false;

        private void Awake()
        {
            Initialize();
        }

        public bool IsFulfilled(ConditionTag conditionTag)
        {
            Initialize();

            if (m_Conditions.ContainsKey(conditionTag))
            {
                return m_Conditions[conditionTag].IsFulfilled();
            }

            return false;
        }

        private void Initialize()
        {
            if (!m_IsInitialized)
            {
                var conditions = m_Conditions.Values;
                foreach (var condition in conditions)
                {
                    condition.SetGameObject(gameObject);
                }

                m_IsInitialized = true;
            }
        }
    }
}
