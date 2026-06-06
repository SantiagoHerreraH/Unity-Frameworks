using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace SilverPillar.Core
{
    [CreateAssetMenu(fileName = "CachedScoreChooserData", menuName = "SilverPillar/Core/ChoosingSystem/CachedScoreChooserData")]
    public class CachedScoreChooserData : SaveableScriptableObject, ICachedScore, IChoose
    {
        [OdinSerialize, ShowInInspector]
        private ChooseCachedScore m_ChooseCachedScore;

        public void Choose()
        {
            m_ChooseCachedScore.Choose();
        }

        public ICachedScore Clone()
        {
            return m_ChooseCachedScore.Clone();
        }

        public GameObject GetGameObject()
        {
            return m_ChooseCachedScore.GetGameObject();
        }

        public float CalculateScore()
        {
            return m_ChooseCachedScore.CalculateScore();
        }

        public bool SetGameObject(GameObject gameObj)
        {
            return m_ChooseCachedScore.SetGameObject(gameObj);
        }
    }
}
