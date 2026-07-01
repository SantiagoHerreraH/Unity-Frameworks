using UnityEngine;
using SilverPillar.Core;
using Sirenix.OdinInspector;
using MoreMountains.Feedbacks;

namespace SilverPillar.Integrations.Feel
{
    public class MMFPlayerDuration_CachedScore : ICachedScore
    {
        [SerializeField]
        private SelfType m_FromWhichPlayer;
        [SerializeField, ShowIf(nameof(m_FromWhichPlayer), SelfType.CustomGameObject)]
        private MMF_Player m_Player;

        [SerializeField]
        private float m_Offset = 0;

        private GameObject m_Self;

        public MMFPlayerDuration_CachedScore() { }
        public MMFPlayerDuration_CachedScore(MMFPlayerDuration_CachedScore other)
        {
            m_FromWhichPlayer = other.m_FromWhichPlayer;
            m_Player = other.m_Player;
            m_Self = other.m_Self;
            m_Offset = other.m_Offset;
        }

        public float CalculateScore()
        {
            if (m_Player != null)
            {
                return m_Player.TotalDuration + m_Offset;
            }

            return 0f;
        }

        public ICachedScore Clone()
        {
            return new MMFPlayerDuration_CachedScore(this);
        }

        public GameObject GetGameObject()
        {
            return m_Self;
        }

        public bool SetGameObject(GameObject self)
        {
            m_Self = self;

            if (m_Self != null && m_FromWhichPlayer == SelfType.ThisGameObject)
            {
                m_Player = self.GetComponent<MMF_Player>();
            }

            return m_Self != null && m_Player != null;
        }
    }
}
