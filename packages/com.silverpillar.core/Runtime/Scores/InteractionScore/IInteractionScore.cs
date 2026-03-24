using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Core
{
    public interface IInteractionScore
    {
        public float CalculateScore(GameObject self, GameObject target);
    }
}

