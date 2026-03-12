using Pillar;
using Sirenix.Serialization;
using System.Collections.Generic;
using UnityEngine;

namespace Pillar
{
    [CreateAssetMenu(fileName = "GoalPlan", menuName = "GOAP/GoalPlan")]
    public class GoalPlan : SaveableScriptableObject
    {
        [OdinSerialize]
        public SortedSet<ConditionGroup> GoalsInOrder;
    }
}
