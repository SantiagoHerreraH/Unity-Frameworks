using UnityEngine;
using SilverPillar.Core;

namespace SilverPillar.Stats
{
    [CreateAssetMenu(fileName = "StatDeltaScore", menuName = "SilverPillar/Stats/StatDeltaScore")]
    public class StatDeltaScore : IInteractionScore
    {
        [SerializeField]
        private EntityStatIdentity m_CompareThis;
        [SerializeField]
        private EntityStatIdentity m_AgainstThis;
        [SerializeField]
        private ValueRange m_ScoreValueRange;

        public float CalculateScore(GameObject self, GameObject target)
        {
            float delta = Mathf.Abs(m_CompareThis.GetValue(self, target) - m_AgainstThis.GetValue(self, target));

            return m_ScoreValueRange.GetValue(delta);
        }
    }
}

