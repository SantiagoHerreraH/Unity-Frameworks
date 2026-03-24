using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace SilverPillar.Core
{
    [CreateAssetMenu(fileName = "InteractionScore", menuName = "SilverPillar/Core/InteractionScore")]
    public class CachedInteractionScore : SaveableScriptableObject, ICachedInteractionScore
    {
        [OdinSerialize, ShowInInspector]
        private ICachedInteractionScore m_ICachedInteractionScore;

        public ICachedInteractionScore Clone(GameObject gameObject)
        {
            var clone = m_ICachedInteractionScore.Clone();
            clone.SetGameObject(gameObject);
            return clone;
        }
        public ICachedInteractionScore Clone()
        {
            return m_ICachedInteractionScore.Clone();
        }

        public float CalculateScore(GameObject target)
        {
            return m_ICachedInteractionScore.CalculateScore(target);
        }

        public GameObject GetGameObject()
        {
            return m_ICachedInteractionScore.GetGameObject();
        }

        public bool SetGameObject(GameObject self)
        {
            return m_ICachedInteractionScore.SetGameObject((GameObject)self);
        }
    }
}

