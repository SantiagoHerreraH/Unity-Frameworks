using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace SilverPillar.Core
{
    [CreateAssetMenu(fileName = "InteractionConditionChooserData", menuName = "SilverPillar/Core/ChoosingSystem/InteractionConditionChooserData")]
    public class InteractionConditionChooserData : SaveableScriptableObject, IInteractionCondition, IChoose
    {
        [OdinSerialize, ShowInInspector]
        private ChooseInteractionCondition m_ChooseCachedCondition;

        public void Choose()
        {
            m_ChooseCachedCondition.Choose();
        }

        public IInteractionCondition Clone()
        {
            return m_ChooseCachedCondition.Clone();
        }

        public GameObject GetGameObject()
        {
            return m_ChooseCachedCondition.GetGameObject();
        }

        public bool IsFulfilled(GameObject target)
        {
            return m_ChooseCachedCondition.IsFulfilled(target);
        }

        public bool SetGameObject(GameObject gameObj)
        {
            return m_ChooseCachedCondition.SetGameObject(gameObj);
        }
    }
}

