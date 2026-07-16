using SilverPillar.Core;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SilverPillar.GOAP
{
    public class UndoableBrainInstance
    {
        private UndoableBehaviorActionListInstance m_ActionListInstance = null;
        private UndoableGoalPlanInstance   m_GoalPlanInstance   = null;
        private UndoableBrain m_Brain = null;
        private GameObject m_GameObject = null;
        private BrainInstanceDebugSettings m_DebugSettings;
        public GameObject GameObject { get { return m_GameObject; } }
        public UndoableBrainInstance(UndoableBrain brain, GameObject gameObject, BrainInstanceDebugSettings debugSettings) 
        {
            m_DebugSettings = debugSettings;
            m_Brain = brain;
            m_GameObject = gameObject;
            m_ActionListInstance = brain.ActionList.CreateInstance(gameObject);
            m_GoalPlanInstance = brain.GoalPlan.CreateInstance(gameObject);
        }

        public bool SetGameObject(GameObject gameObj)
        {
            m_GameObject = gameObj;
            bool allGood = m_ActionListInstance.SetGameObject(gameObj);
            allGood &= m_GoalPlanInstance.SetGameObject(gameObj);

            return allGood;
        }


        public UndoableBehaviorActionInstance GetActionInstance()
        {
            UndoableBehaviorActionInstance actionInstance = null;
            CachedCondition chosenGoal = m_GoalPlanInstance.GetGoal();

            if (chosenGoal != null)
            {
                if (m_DebugSettings.PrintCurrentGoal)
                {
                    Debug.Log($"{m_GameObject}'s GOAP BrainHolder's Current Goal is {chosenGoal.name}");
                }


                List<UndoableBehaviorAction> currentPossibleActions = m_ActionListInstance.GetCurrentPossibleActions();
                List<UndoableBehaviorAction> actionsThatLeadToGoal = m_ActionListInstance.GetActionsThatLeadToGoal(chosenGoal);
                UndoableBehaviorAction chosenAction = m_Brain.GetAction(m_GameObject, currentPossibleActions, actionsThatLeadToGoal);
                
                if (chosenAction != null)
                {
                    actionInstance = m_ActionListInstance.GetInstance(chosenAction);
                }
                else
                {
                    actionInstance = m_ActionListInstance.GetFirstInstance();
                }
            }
            else
            {
                if (m_DebugSettings.PrintCurrentGoal)
                {
                    Debug.Log($"{m_GameObject}'s GOAP BrainHolder has NO CURRENT GOAL.");
                }

                actionInstance = m_ActionListInstance.GetFirstInstance();
            }

            return actionInstance;

        }
    }

    [Serializable]
    public struct UndoableActionPath : IEquatable<UndoableActionPath>
    {
        [SerializeField]
        private UndoableBehaviorAction m_Start;
        [SerializeField]
        private UndoableBehaviorAction m_Target;

        public UndoableActionPath(UndoableBehaviorAction start, UndoableBehaviorAction target)
        {
            m_Start = start;
            m_Target = target;
        }

        public override bool Equals(object obj)
        {
            return obj is ActionPath other && Equals(other);
        }

        public bool Equals(UndoableActionPath other)
        {
            return m_Start == other.m_Start && m_Target == other.m_Target;
        }

        public override int GetHashCode()
        {
            int firstId = m_Start == null ? 0 : m_Start.GetHashCode();
            int secondId = m_Target == null ? 0 : m_Target.GetHashCode();
            return HashCodeCombiner.Combine(firstId, secondId).GetHashCode();
        }
    }

    [CreateAssetMenu(fileName = "Brain", menuName = "SilverPillar/GOAP/Brain")]
    public class UndoableBrain : SaveableScriptableObject
    {
        [Title("Creation Data")]
        [SerializeField] private HowToChooseCurrentAction m_HowToChooseCurrentAction;
        [SerializeField] private UndoableGoalPlan m_GoalPlan;
        [SerializeField] private UndoableBehaviorActionList m_ActionList;

        public HowToChooseCurrentAction HowToChooseCurrentAction { get { return m_HowToChooseCurrentAction; } }
        public UndoableGoalPlan GoalPlan {  get { return m_GoalPlan; } }
        public UndoableBehaviorActionList ActionList { get { return m_ActionList; }}

        [Title("When To Recreate")]
        [SerializeField]
        private bool m_RecreateGraphOnValidate = true;
        private bool m_IsDirty = false;

        [OdinSerialize, SerializeField, HideInInspector]
        private Dictionary<UndoableBehaviorAction, UndoableActionNode> m_ActionToNode = new();

        // cache: (start,target) -> cost/path
        [OdinSerialize, SerializeField, HideInInspector]
        private Dictionary<UndoableActionPath, float> m_ActionPath_To_PathCost = new();

        [OdinSerialize, SerializeField, HideInInspector]
        private Dictionary<UndoableActionPath, List<UndoableBehaviorAction>> m_ActionPath_To_Path = new();

        // -------- Priority Queue Item (no tuples) --------
        private readonly struct PQItem
        {
            public readonly float Cost;
            public readonly int Tie;
            public readonly UndoableBehaviorAction Action;

            public PQItem(float cost, int tie, UndoableBehaviorAction action)
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

        public UndoableBrainInstance CreateInstance(GameObject gameObject, BrainInstanceDebugSettings debugSettings)
        {
            return new UndoableBrainInstance(this, gameObject, debugSettings);
        }

        

        private void EnsureGraphIsBuilt()
        {
            if (m_IsDirty)
            {
                if (m_ActionToNode != null)
                {
                    m_ActionToNode.Clear();
                }
                if (m_ActionPath_To_PathCost == null)
                {
                    m_ActionPath_To_PathCost = new();
                }
                if (m_ActionPath_To_Path == null)
                {
                    m_ActionPath_To_Path = new();
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
                var node = new UndoableActionNode { Action = action };
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
                for (int j = 0; j < possibleActions.Count; j++)
                {
                    CalculateShortestPath(possibleActions[i], possibleActions[j]);
                }
            }
        }

        /// <summary>
        /// Dijkstra shortest-path (action sequence) from startAction to targetAction.
        /// Returns empty list if unreachable.
        /// </summary>
        private void CalculateShortestPath(UndoableBehaviorAction startAction, UndoableBehaviorAction targetAction)
        {
            if (startAction == null || targetAction == null) return;

            var pathKey = new UndoableActionPath(startAction, targetAction);

            Dictionary<UndoableBehaviorAction, float> distances = new();
            Dictionary<UndoableBehaviorAction, UndoableBehaviorAction> predecessors = new();
            SortedSet<PQItem> priorityQueue = new(new PQItemComparer());

            distances[startAction] = 0;
            priorityQueue.Add(new PQItem(0, 0, startAction));

            bool found = false;

            while (priorityQueue.Count > 0)
            {
                var current = priorityQueue.Min;
                priorityQueue.Remove(current);

                if (current.Action == targetAction)
                {
                    found = true;
                    break;
                }

                if (!m_ActionToNode.TryGetValue(current.Action, out var node)) continue;

                foreach (var child in node.Children)
                {
                    float newDist = distances[current.Action] + child.Cost; // Asumiendo que BehaviorAction tiene una propiedad Cost
                    if (!distances.ContainsKey(child) || newDist < distances[child])
                    {
                        priorityQueue.Remove(new PQItem(distances.ContainsKey(child) ? distances[child] : 0, 0, child));
                        distances[child] = newDist;
                        predecessors[child] = current.Action;
                        priorityQueue.Add(new PQItem(newDist, 0, child));
                    }
                }
            }

            if (found)
            {
                m_ActionPath_To_PathCost[pathKey] = distances[targetAction];

                List<UndoableBehaviorAction> path = new();
                UndoableBehaviorAction curr = targetAction;
                while (curr != null)
                {
                    path.Add(curr);
                    predecessors.TryGetValue(curr, out curr);
                }
                path.Reverse();
                m_ActionPath_To_Path[pathKey] = path;
            }
        }

#nullable enable

        public UndoableBehaviorAction? GetAction(GameObject gameObj, List<UndoableBehaviorAction> currentPossibleActions, List<UndoableBehaviorAction> actionsThatLeadToGoal)
        {
            EnsureGraphIsBuilt();

            UndoableBehaviorAction? chosenAction = null;

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
                    var key = new UndoableActionPath(currentPossibleAction, goalAction);

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
