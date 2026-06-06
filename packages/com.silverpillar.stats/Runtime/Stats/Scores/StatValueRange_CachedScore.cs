using SilverPillar.Core;
using System;
using UnityEngine;

namespace SilverPillar.Stats
{

#nullable enable

    [Serializable]
    public class StatValueRange_CachedScore : ICachedScore
    {
        [SerializeField]
        private StatVariableValueRange m_StatScore;

        private GameObject? m_Target;

        public float CalculateScore()
        {
            if (m_Target != null && m_Target.TryGetComponent<StatController>(out var statController))
            {
                return m_StatScore.GetValue(statController);
            }

            return 0;
        }

        public ICachedScore Clone()
        {
            return new StatValueRange_CachedScore { m_StatScore = this.m_StatScore, m_Target = this.m_Target };
        }

        public GameObject? GetGameObject() => m_Target;

        public bool SetGameObject(GameObject self)
        {
            m_Target = self;
            return m_Target != null;
        }
    }
}


