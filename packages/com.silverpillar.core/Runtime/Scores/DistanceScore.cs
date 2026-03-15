using UnityEngine;

namespace Pillar
{
    public class DistanceScore : SaveableInteractionScore
    {
        [SerializeField]
        private ValueTransformation m_ScoreValueRange;

        public override float CalculateScore(GameObject self, GameObject other)
        {
            float distance = (self.transform.position - other.transform.position).magnitude;

            return m_ScoreValueRange.TransformValue(distance);
        }
    }
}

