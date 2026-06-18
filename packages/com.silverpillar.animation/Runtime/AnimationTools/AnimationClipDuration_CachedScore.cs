using SilverPillar.Core;
using UnityEngine;

namespace SilverPillar.Animation
{
    public class AnimationClipDuration_CachedScore : ICachedScore
    {
        [SerializeField]
        private AnimationClip m_AnimationClip;
        [SerializeField, Tooltip("Multiplied by animation clip duration")]
        private float m_DurationMultiplier = 1;
        [SerializeField, Tooltip("Value to add after speed multiplication")]
        private float m_Offset = 0;

        private GameObject m_Self;

        public AnimationClipDuration_CachedScore()
        {

        }

        public AnimationClipDuration_CachedScore(AnimationClipDuration_CachedScore other)
        {
            m_AnimationClip = other.m_AnimationClip;
            m_DurationMultiplier = other.m_DurationMultiplier;
            m_Offset = other.m_Offset;
        }

        public float CalculateScore()
        {
            return m_AnimationClip == null ? 0 : (m_AnimationClip.length * m_DurationMultiplier) + m_Offset;
        }

        public ICachedScore Clone()
        {
            return new AnimationClipDuration_CachedScore(this);
        }

        public GameObject GetGameObject()
        {
            return m_Self;
        }

        public bool SetGameObject(GameObject self)
        {
            m_Self = self;
            return m_Self != null;
        }
    }
}
