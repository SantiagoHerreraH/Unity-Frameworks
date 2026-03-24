using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class CachedInteractionScoreGroup : ICachedInteractionScore
    {
        [SerializeField]
        private HowToCalculateScore m_HowToCalculateScore;
        [OdinSerialize, ShowInInspector]
        private List<ICachedInteractionScore> m_Scores = new();

        public CachedInteractionScoreGroup() { }
        public CachedInteractionScoreGroup(CachedInteractionScoreGroup other)
        {
            m_HowToCalculateScore = other.m_HowToCalculateScore;

            foreach (var score in other.m_Scores)
            {
                m_Scores.Add(score.Clone());
            }

        }

        public float CalculateScore(GameObject target)
        {
            if (m_Scores == null || m_Scores.Count == 0) return 0f;

            // get individual values first
            var values = m_Scores.Select(s => s.CalculateScore(target)).ToList();

            return ScoreTools.CalculateScore(m_HowToCalculateScore, values);
        }

        public ICachedInteractionScore Clone()
        {
            return new CachedInteractionScoreGroup(this);
        }

#nullable enable
        public GameObject? GetGameObject()
        {
            return m_Scores.Count > 0 ? m_Scores.First().GetGameObject() : null;
        }

        public bool SetGameObject(GameObject self)
        {
            bool allGood = true;

            foreach (var score in m_Scores)
            {
                if (score.SetGameObject(self))
                {
                    allGood = false;
                }
            }

            return allGood;
        }
    }
}

