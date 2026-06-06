using SilverPillar.Core;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace SilverPillar.Stats
{

    [Serializable]
    public class Stat_CachedScore : ICachedScore
    {
        [SerializeField]
        private SelfType m_FromWhichStatController;
        [SerializeField, ShowIf(nameof(m_FromWhichStatController), SelfType.CustomGameObject)]
        private StatController m_StatController;
        [SerializeField]
        private StatType m_StatTypeToGetValueFrom;
        [SerializeField]
        private StatVariable m_StatVariable;

        private GameObject m_Self;

        public Stat_CachedScore() { }
        public Stat_CachedScore(Stat_CachedScore other)
        {
            m_FromWhichStatController = other.m_FromWhichStatController;
            m_StatController = other.m_StatController;
            m_StatTypeToGetValueFrom = other.m_StatTypeToGetValueFrom;
            m_StatVariable = other.m_StatVariable;
            m_Self = other.m_Self;
        }

        public float CalculateScore()
        {
            if (m_StatController != null)
            {
                return m_StatController.GetStat(m_StatTypeToGetValueFrom, m_StatVariable);
            }

            return 0;
        }

        public ICachedScore Clone()
        {
            return new Stat_CachedScore(this);
        }

        public GameObject GetGameObject() => m_Self;

        public bool SetGameObject(GameObject self)
        {
            bool allGood = false;

            if (m_FromWhichStatController == SelfType.ThisGameObject && self != null)
            {
                allGood = self.TryGetComponent(out m_StatController);
            }
            else if (m_FromWhichStatController == SelfType.CustomGameObject)
            {
                allGood = m_StatController != null;
            }

            return allGood;
        }
    }
}


