
using Sirenix.Serialization;
using System.Collections.Generic;
using UnityEngine;
using SilverPillar.Core;

namespace SilverPillar.GOAP
{
    [CreateAssetMenu(fileName = "GoalPlan", menuName = "GOAP/GoalPlan")]
    public class GoalPlan : SaveableScriptableObject
    {
        [OdinSerialize]
        public SortedSet<ConditionGroup> GoalsInOrder;
    }
}
