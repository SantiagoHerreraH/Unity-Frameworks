using UnityEngine;

namespace SilverPillar.Core
{
    public class ConditionTag_Condition : ICondition
    {
        [SerializeField]
        private ConditionTag m_ConditionTag = null;

        public bool IsFulfilled(GameObject gameObj)
        {
            ConditionMachine machine = null;

            if (gameObj.TryGetComponent<ConditionMachine>(out machine))
            {
                return machine.IsFulfilled(m_ConditionTag);
            }

            return false;
        }
    }
}
