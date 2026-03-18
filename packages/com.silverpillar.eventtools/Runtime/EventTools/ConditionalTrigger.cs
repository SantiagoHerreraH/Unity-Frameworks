
using SilverPillar.Core;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.Events;

namespace SilverPillar.EventTools
{
    public class ConditionalTrigger : MonoBehaviour
    {
        [OdinSerialize]
        private ConditionGroupData Condition = new();

        [SerializeField]
        private UnityEvent m_OnTrue;

        [SerializeField]
        private UnityEvent m_OnFalse;

        public void Trigger()
        {
            if (Condition.IsFulfilled(gameObject))
            {
                m_OnTrue.Invoke();
            }
            else
            {
                m_OnFalse.Invoke();
            }
        }


    }


}
