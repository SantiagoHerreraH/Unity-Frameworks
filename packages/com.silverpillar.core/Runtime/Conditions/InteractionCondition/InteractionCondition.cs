using UnityEngine;

namespace SilverPillar.Core
{
    using UnityEngine;
    using Sirenix.Serialization;

    [CreateAssetMenu(fileName = "InteractionCondition", menuName = "SilverPillar/Core/Conditions/InteractionCondition")]
    public class InteractionCondition : SaveableScriptableObject, IInteractionCondition
    {
        [OdinSerialize]
        private IInteractionCondition m_Condition;

        public bool SetGameObject(GameObject self)
        {
            return m_Condition.SetGameObject(self);
        }

#nullable enable
        public GameObject? GetGameObject()
        {
            return m_Condition.GetGameObject();
        }

        public bool IsFulfilled(GameObject target)
        {
            return m_Condition.IsFulfilled(target);
        }

        public IInteractionCondition Clone()
        {
            return m_Condition.Clone();
        }

        public IInteractionCondition Clone(GameObject gameObj)
        {
            var clone = m_Condition.Clone();
            clone.SetGameObject(gameObj);
            return clone;
        }
    }

}
