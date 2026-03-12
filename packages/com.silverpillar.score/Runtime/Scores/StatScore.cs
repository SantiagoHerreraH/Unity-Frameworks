using UnityEngine;

namespace Pillar
{
    public class StatScore : SaveableInteractionScore
    {
        [SerializeField]
        private StatValueRange m_StatScore;
        [SerializeField]
        private TargetType m_TargetType;

        public override float CalculateScore(GameObject self, GameObject target)
        {
            StatController statController = null;

            switch (m_TargetType)
            {
                case TargetType.Self:
                    statController = self.GetComponent<StatController>();

                    

                    break;
                case TargetType.Other:

                    statController = target.GetComponent<StatController>();

                    break;
                default:
                    break;
            }

            if (statController != null)
            {
                return m_StatScore.GetValue(statController);
            }

            return 0;
        }
    }
}

