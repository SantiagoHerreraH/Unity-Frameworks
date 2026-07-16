
using System.Collections.Generic;
using UnityEngine;
using SilverPillar.Core;

namespace SilverPillar.GOAP
{
    [CreateAssetMenu(fileName = "GoalPlan", menuName = "SilverPillar/GOAP/GoalPlan")]
    public class GoalPlan : SaveableScriptableObject
    {
        [SerializeField]
        private List<CachedCondition> m_GoalsInOrder = new();
        public List<CachedCondition> GoalsInOrder { get { return m_GoalsInOrder; } }

        public GoalPlanInstance CreateInstance(GameObject gameObject)
        {
            return new GoalPlanInstance(this, gameObject);
        }
    }

    public class GoalPlanInstance
    {
        private List<ICachedCondition> m_GoalsInOrder = new();
        private Dictionary<ICachedCondition, CachedCondition> m_Instance_To_CachedCondition = new();

        public GoalPlanInstance() { }
        public GoalPlanInstance(GoalPlan goalPlan, GameObject gameObj)
        {
            var goalsInOrder = goalPlan.GoalsInOrder;
            foreach (var item in goalsInOrder)
            {
                var instance = item.Clone(gameObj);
                m_GoalsInOrder.Add(instance);
                m_Instance_To_CachedCondition.Add(instance, item);
            }
        }

#nullable enable

        public CachedCondition? GetGoal()
        {
            foreach (var goal in m_GoalsInOrder)
            {
                if (!goal.IsFulfilled())
                {
                    return m_Instance_To_CachedCondition[goal];
                }
            }

            return null;
        }
    }
}
