using SilverPillar.Core;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.GOAP
{
    [CreateAssetMenu(fileName = "Action", menuName = "SilverPillar/GOAP/Action")]
    public class Action : SaveableScriptableObject, IScore
    {
        [TabGroup("Graph Connections")]
        [Title("Preconditions")]
        public ReducedConditionType PreconditionsType;
        [Tooltip("If these are and conditions you have to make sure the children in conjunction lead to all conditions being true")]
        public List<ICondition> Preconditions = new();

        [TabGroup("Graph Connections")]
        [Title("Effects On World")]
        public List<ICondition> EffectOnWorld = new();


        [TabGroup("Action Choosing Criteria")]
        [Title("Action Cost")]
        [Min(0.1f)]
        public float Cost = 1;


        [TabGroup("Action Choosing Criteria")]
        [Title("How to Score Action")]
        [SerializeField]
        private ScoreGroup m_ScoreGroup;

        [TabGroup("Action Data")]
        [OdinSerialize, ShowInInspector]
        public List<IAction> Actions = new();

        public float CalculateScore(GameObject gameObject)
        {
            return m_ScoreGroup.CalculateScore(gameObject);
        }

        public ActionExecutionData GetActionExecutionData(GameObject gameObject)
        {
            return new ActionExecutionData(Actions, gameObject);
        }
        public bool IsChildrenActionOfOther(Action otherAction)
        {
            foreach (var otherEffectOnWorld in otherAction.EffectOnWorld)
                foreach (var selfPreCondition in Preconditions)
                    if (otherEffectOnWorld == selfPreCondition) return true;

            return false;
        }

        public bool IsParentActionOfOther(Action otherAction)
        {
            foreach (var selfEffectOnWorld in EffectOnWorld)
                foreach (var otherPreCondition in otherAction.Preconditions)
                    if (selfEffectOnWorld == otherPreCondition) return true;

            return false;
        }

        public bool PreconditionsAreFulfilled(GameObject gameObj)
        {
            return ConditionGroupData.IsFulfilled(gameObj, PreconditionsType, Preconditions);
        }

        public bool EffectOnWorldIsFulfilled(GameObject gameObj)
        {
            return ConditionGroupData.IsFulfilled(gameObj, ConditionType.OneHasToBeTrue, EffectOnWorld);
        }

        public bool HasPrecondition(ConditionGroup condition)
        {
            return Preconditions.Contains(condition);
        }

        public bool HasEffectOnWorld(ConditionGroup condition)
        {
            return EffectOnWorld.Contains(condition);
        }
    }

    public class ActionNode
    {
        public Action Action;
        public List<Action> Parents = new();
        public List<Action> Children = new();
    }
}
