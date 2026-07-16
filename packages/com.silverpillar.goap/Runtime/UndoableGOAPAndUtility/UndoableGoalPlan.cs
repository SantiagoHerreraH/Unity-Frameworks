
using System.Collections.Generic;
using UnityEngine;
using SilverPillar.Core;

namespace SilverPillar.GOAP
{
    [CreateAssetMenu(fileName = "GoalPlan", menuName = "SilverPillar/GOAP/GoalPlan")]
    public class UndoableGoalPlan : SaveableScriptableObject
    {
        [SerializeField]
        private List<CachedCondition> m_GoalsInOrder = new();
        public List<CachedCondition> GoalsInOrder { get { return m_GoalsInOrder; } }

        public UndoableGoalPlanInstance CreateInstance(GameObject gameObject)
        {
            return new UndoableGoalPlanInstance(this, gameObject);
        }
    }

    public class UndoableGoalPlanInstance
    {
        private List<ICachedCondition> m_GoalsInOrder = new();
        private Dictionary<ICachedCondition, CachedCondition> m_Instance_To_CachedCondition = new();

        public UndoableGoalPlanInstance() { }
        public UndoableGoalPlanInstance(UndoableGoalPlan goalPlan, GameObject gameObj)
        {
            var goalsInOrder = goalPlan.GoalsInOrder;
            foreach (var item in goalsInOrder)
            {
                var instance = item.Clone(gameObj);
                m_GoalsInOrder.Add(instance);
                m_Instance_To_CachedCondition.Add(instance, item);
            }
        }

        public bool SetGameObject(GameObject gameObject)
        {
            bool allGood = true;

            foreach (var item in m_GoalsInOrder)
            {
                allGood &= item.SetGameObject(gameObject);
            }

            return allGood;
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
