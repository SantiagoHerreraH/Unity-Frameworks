using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class ScoreGroup : IScore
    {
        [SerializeField]
        private HowToCalculateScore m_HowToCalculateScore;
        [OdinSerialize, ShowInInspector]
        private List<IScore> m_Scores = new();

        public float CalculateScore(GameObject gameObj)
        {
            if (m_Scores == null || m_Scores.Count == 0) return 0f;

            // get individual values first
            var values = m_Scores.Select(s => s.CalculateScore(gameObj)).ToList();

            return ScoreTools.CalculateScore(m_HowToCalculateScore, values);
        }
    }
}

