using UnityEngine;
using SilverPillar.Core;

namespace SilverPillar.Stats
{
    public class StatScore : IScore
    {
        [SerializeField]
        private StatVariableValueRange m_StatScore;

        public float CalculateScore(GameObject gameObj)
        {
            StatController statController = null;

            statController = gameObj.GetComponent<StatController>();

            if (statController != null)
            {
                return m_StatScore.GetValue(statController);
            }

            return 0;
        }
    }
}

