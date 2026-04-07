using UnityEngine;

namespace SilverPillar.Core
{
    [CreateAssetMenu(fileName = "Condition", menuName = "SilverPillar/Core/Conditions/Condition")]
    public class Condition : SaveableScriptableObject, ICondition
    {
        [SerializeField]
        private ICondition m_Condition = null;
        public bool IsFulfilled(GameObject gameObj)
        {
            return m_Condition.IsFulfilled(gameObj);
        }
    }
}
