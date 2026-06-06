using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace SilverPillar.Core
{
    [CreateAssetMenu(fileName = "CachedConditionChooserData", menuName = "SilverPillar/Core/ChoosingSystem/CachedConditionChooserData")]
    public class CachedConditionChooserData : SaveableScriptableObject, ICachedCondition, IChoose
    {
        [OdinSerialize, ShowInInspector]
        private ChooseCachedCondition m_ChooseCachedCondition;

        public void Choose()
        {
            m_ChooseCachedCondition.Choose();
        }

        public ICachedCondition Clone()
        {
            return m_ChooseCachedCondition.Clone();
        }

        public GameObject GetGameObject()
        {
            return m_ChooseCachedCondition.GetGameObject();
        }

        public bool IsFulfilled()
        {
            return m_ChooseCachedCondition.IsFulfilled();
        }

        public bool SetGameObject(GameObject gameObj)
        {
            return m_ChooseCachedCondition.SetGameObject(gameObj);  
        }
    }
}
