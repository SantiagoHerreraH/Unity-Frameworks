using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace SilverPillar.Core
{
    [CreateAssetMenu(fileName = "CachedInteractionScoreChooserData", menuName = "SilverPillar/Core/ChoosingSystem/CachedInteractionScoreChooserData")]
    public class CachedInteractionScoreChooserData : SaveableScriptableObject, ICachedInteractionScore, IChoose
    {
        [OdinSerialize, ShowInInspector]
        private ChooseCachedInteractionScore m_ChooseCachedInteractionScore;

        public void Choose()
        {
            m_ChooseCachedInteractionScore.Choose();
        }

        public ICachedInteractionScore Clone()
        {
            return m_ChooseCachedInteractionScore.Clone();
        }

        public GameObject GetGameObject()
        {
            return m_ChooseCachedInteractionScore.GetGameObject();
        }

        public float CalculateScore(GameObject target)
        {
            return m_ChooseCachedInteractionScore.CalculateScore(target);
        }

        public bool SetGameObject(GameObject gameObj)
        {
            return m_ChooseCachedInteractionScore.SetGameObject(gameObj);
        }
    }
}
