using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SilverPillar.Core
{
    [CreateAssetMenu(fileName = "InteractionScore", menuName = "SilverPillar/Core/InteractionScore")]
    public class InteractionScore : SaveableScriptableObject, IInteractionScore
    {
        [OdinSerialize, ShowInInspector]
        private IInteractionScore m_IInteractionScore;

        public float CalculateScore(GameObject self, GameObject target)
        {
            return m_IInteractionScore.CalculateScore(self, target);
        }
    }
}

