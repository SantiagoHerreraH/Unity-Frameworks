using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SilverPillar.Core;

namespace SilverPillar.GOAP
{

    public class BrainInstance
    {
        private ActionListInstance m_ActionListInstance = null;
        private GoalPlanInstance   m_GoalPlanInstance   = null;
        private Brain m_Brain = null;
        private GameObject m_GameObject = null;
        public GameObject GameObject { get { return m_GameObject; } }
        public BrainInstance(Brain brain, GameObject gameObject) 
        {
            m_Brain = brain;
            m_GameObject = gameObject;
            m_ActionListInstance = brain.ActionList.CreateInstance(gameObject);
            m_GoalPlanInstance = brain.GoalPlan.CreateInstance(gameObject);
        }


        public ActionInstance GetActionInstance()
        {
            ActionInstance actionInstance = null;
            CachedCondition chosenGoal = m_GoalPlanInstance.GetGoal();

            if (chosenGoal != null)
            {
                List<Action> currentPossibleActions = m_ActionListInstance.GetCurrentPossibleActions();
                List<Action> actionsThatLeadToGoal = m_ActionListInstance.GetActionsThatLeadToGoal(chosenGoal);
                Action chosenAction  = m_Brain.GetAction(m_GameObject, currentPossibleActions, actionsThatLeadToGoal);
                actionInstance = m_ActionListInstance.GetInstance(chosenAction);
            }
            else
            {
                actionInstance = m_ActionListInstance.GetRandomInstance();
            }

            return actionInstance;

        }
    }

    [CreateAssetMenu(fileName = "Brain", menuName = "SilverPillar/GOAP/Brain")]
    public class Brain : SaveableScriptableObject
    {
        [Title("Creation Data")]
        [SerializeField] private HowToChooseCurrentAction m_HowToChooseCurrentAction;
        [SerializeField] private GoalPlan m_GoalPlan;
        [SerializeField] private ActionList m_ActionList;

        public HowToChooseCurrentAction HowToChooseCurrentAction { get { return m_HowToChooseCurrentAction; } }
        public GoalPlan GoalPlan {  get { return m_GoalPlan; } }
        public ActionList ActionList { get { return m_ActionList; }}

        [Title("When To Recreate")]
        [SerializeField]
        private bool m_RecreateGraphOnValidate = true;
        private bool m_IsDirty = false;

        [OdinSerialize, SerializeField, HideInInspector]
        private Dictionary<Action, ActionNode> m_ActionToNode = new();

        // cache: (start,target) -> cost/path
        [OdinSerialize, SerializeField, HideInInspector]
        private Dictionary<long, float> m_ActionPath_To_PathCost = new();

        [OdinSerialize, SerializeField, HideInInspector]
        private Dictionary<long, List<Action>> m_ActionPath_To_Path = new();

        // -------- Priority Queue Item (no tuples) --------
        private readonly struct PQItem
        {
            public readonly float Cost;
            public readonly int Tie;
            public readonly Action Action;

            public PQItem(float cost, int tie, Action action)
            {
                Cost = cost;
                Tie = tie;
                Action = action;
            }
        }

        private sealed class PQItemComparer : IComparer<PQItem>
        {
            public int Compare(PQItem x, PQItem y)
            {
                int c = x.Cost.CompareTo(y.Cost);
                if (c != 0) return c;

                c = x.Tie.CompareTo(y.Tie);
                if (c != 0) return c;

                // Keep SortedSet happy even on equal costs/ties
                return x.Action.GetInstanceID().CompareTo(y.Action.GetInstanceID());
            }
        }

        private void OnEnable()
        {
            m_ActionToNode?.Clear();

        }

        private void OnValidate()
        {
            if (m_RecreateGraphOnValidate)
            {
                RecreateGraph();
            }
            else
            {

                m_IsDirty = true;
            }
        }

        [Button(ButtonSizes.Medium)]
        private void RecreateGraph()
        {
            m_IsDirty = true;
            EnsureGraphIsBuilt();
        }

        public BrainInstance CreateInstance(GameObject gameObject)
        {
            return new BrainInstance(this, gameObject);
        }

        public static long CombineHashCodes(int hash1, int hash2)
        {
            return ((long)(uint)hash1 << 32) | (uint)hash2;
        }

        private static long MakeKey(Action start, Action target)
        {
            return CombineHashCodes(start.GetInstanceID(), target.GetInstanceID());
        }

        private void EnsureGraphIsBuilt()
        {
            if (m_IsDirty)
            {
                if (m_ActionToNode != null)
                {
                    m_ActionToNode.Clear();
                }
                m_ActionPath_To_PathCost.Clear();
                m_ActionPath_To_Path.Clear();
            }
            if (m_ActionToNode.Count == 0)
                CreateGraph();
        }

        private void CreateGraph()
        {
            m_IsDirty = false;

            var possibleActions = m_ActionList.PossibleActions.ToList();

            m_ActionToNode.EnsureCapacity(possibleActions.Count);

            // Create nodes
            foreach (var action in possibleActions)
            {
                var node = new ActionNode { Action = action };
                m_ActionToNode[action] = node;
            }

            // Create edges (directed)
            for (int i = 0; i < possibleActions.Count; i++)
            {
                for (int j = i; j < possibleActions.Count; j++)
                {
                    var a = possibleActions[i];
                    var b = possibleActions[j];

                    var nodeA = m_ActionToNode[a];
                    var nodeB = m_ActionToNode[b];

                    // b -> a
                    if (a.IsChildrenActionOfOther(b))
                    {
                        nodeA.Parents.Add(b);
                        nodeB.Children.Add(a);
                    }

                    // a -> b
                    if (a.IsParentActionOfOther(b))
                    {
                        nodeB.Parents.Add(a);
                        nodeA.Children.Add(b);
                    }
                }
            }

            for (int i = 0; i < possibleActions.Count; i++)
            {
                for (int j = i; j < possibleActions.Count; j++)
                {
                    CalculateShortestPath(possibleActions[i], possibleActions[j]);
                }
            }
        }

        /// <summary>
        /// Dijkstra shortest-path (action sequence) from startAction to targetAction.
        /// Returns empty list if unreachable.
        /// </summary>
        private List<Action> CalculateShortestPath(Action startAction, Action targetAction)
        {
            if (startAction == null || targetAction == null) return new List<Action>();

            if (!m_ActionToNode.ContainsKey(startAction) || !m_ActionToNode.ContainsKey(targetAction))
                return new List<Action>();

            long key = MakeKey(startAction, targetAction);
            if (m_ActionPath_To_Path.TryGetValue(key, out var cachedPath))
                return new List<Action>(cachedPath); // copy to protect cache

            var dist = new Dictionary<Action, float>(64);
            var prev = new Dictionary<Action, Action>(64);
            var visited = new HashSet<Action>();

            int tie = 0;
            var pq = new SortedSet<PQItem>(new PQItemComparer());

            dist[startAction] = startAction.Cost;
            pq.Add(new PQItem(startAction.Cost, tie++, startAction));

            bool found = false;

            while (pq.Count > 0)
            {
                var curItem = pq.Min;
                pq.Remove(curItem);

                var cur = curItem.Action;
                if (!visited.Add(cur)) continue;

                if (ReferenceEquals(cur, targetAction))
                {
                    found = true;
                    break;
                }

                var node = m_ActionToNode[cur];
                foreach (var child in node.Children)
                {
                    if (child == null) continue;
                    if (visited.Contains(child)) continue;

                    float newCost = curItem.Cost + child.Cost;

                    if (!dist.TryGetValue(child, out float oldCost) || newCost < oldCost)
                    {
                        dist[child] = newCost;
                        prev[child] = cur;
                        pq.Add(new PQItem(newCost, tie++, child));
                    }
                }
            }

            if (!found)
            {
                m_ActionPath_To_Path[key] = new List<Action>();
                m_ActionPath_To_PathCost[key] = Mathf.Infinity;
                return new List<Action>();
            }

            // Reconstruct
            var path = new List<Action>();
            var step = targetAction;

            while (true)
            {
                path.Add(step);
                if (ReferenceEquals(step, startAction)) break;

                if (!prev.TryGetValue(step, out var p))
                {
                    path.Clear();
                    break;
                }

                step = p;
            }

            path.Reverse();

            m_ActionPath_To_Path[key] = new List<Action>(path);
            m_ActionPath_To_PathCost[key] = dist.TryGetValue(targetAction, out var cst) ? cst : Mathf.Infinity;

            return path;
        }

        public Action GetAction(GameObject gameObj, List<Action> currentPossibleActions, List<Action> actionsThatLeadToGoal)
        {
            EnsureGraphIsBuilt();

            Action chosenAction = null;

            float chosenValue = 0;
            float currentValue = 0;

            switch (m_HowToChooseCurrentAction)
            {
                case HowToChooseCurrentAction.MinPathCost:
                    chosenValue = Mathf.Infinity;
                    break;
                case HowToChooseCurrentAction.MaxScore:
                    chosenValue = -Mathf.Infinity;
                    break;
                case HowToChooseCurrentAction.MaxScoreMinusMinPathCost:
                    chosenValue = -Mathf.Infinity;
                    break;
                case HowToChooseCurrentAction.MaxScoreDividedByMinPathCost:
                    chosenValue = -Mathf.Infinity;
                    break;
                default:
                    break;
            }

            foreach (var currentPossibleAction in currentPossibleActions)
            {
                foreach (var goalAction in actionsThatLeadToGoal)
                {
                    var key = MakeKey(currentPossibleAction, goalAction);

                    if (m_ActionPath_To_PathCost.ContainsKey(key))
                    {
                        bool currentValueIsBetter = false;
                        switch (m_HowToChooseCurrentAction)
                        {
                            case HowToChooseCurrentAction.MinPathCost:

                                currentValue = m_ActionPath_To_PathCost[key];
                                currentValueIsBetter = currentValue < chosenValue;

                                break;
                            case HowToChooseCurrentAction.MaxScore:

                                currentValue = currentPossibleAction.CalculateScore(gameObj);
                                currentValueIsBetter = currentValue > chosenValue;

                                break;
                            case HowToChooseCurrentAction.MaxScoreMinusMinPathCost:

                                currentValue = currentPossibleAction.CalculateScore(gameObj) - m_ActionPath_To_PathCost[key];
                                currentValueIsBetter = currentValue > chosenValue;

                                break;
                            case HowToChooseCurrentAction.MaxScoreDividedByMinPathCost:

                                float minPathCost = m_ActionPath_To_PathCost[key];
                                minPathCost = minPathCost <= 0 ? 0.1f : minPathCost;
                                currentValue = currentPossibleAction.CalculateScore(gameObj) / minPathCost;
                                currentValueIsBetter = currentValue > chosenValue;

                                break;
                            default:
                                break;
                        }

                        if (currentValueIsBetter)
                        {
                            chosenAction = currentPossibleAction;
                            chosenValue = currentValue;
                        }
                    }
                }
            }

            return chosenAction;
        }
    }
}
