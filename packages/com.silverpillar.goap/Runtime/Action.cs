using Pillar;
using Sirenix.Serialization;
using System.Collections.Generic;
using UnityEngine;

namespace Pillar
{

    [CreateAssetMenu(fileName = "Action", menuName = "GOAP/Action")]
    public class Action : SaveableScriptableObject, IScore
    {
        [Tooltip("These are Or Conditions, if one is fulfilled, then ")]
        public List<ConditionGroup> Preconditions = new();
        public List<ConditionGroup> EffectOnWorld = new();

        [Min(0.1f)]
        public float Cost = 1;

        [OdinSerialize]
        public List<IScore> Score = new();

        [OdinSerialize]
        public List<IAction> Actions = new();

        private void OnEnable()
        {
            foreach (var action in Actions)
            {
                action.Initialize();
            }
        }

        public float CalculateScore(GameObject gameObject)
        {
            float finalValue = 0;
            foreach (var score in Score)
                finalValue += score.CalculateScore(gameObject);
            return finalValue;
        }

        public void Start(GameObject gameObj)
        {
            foreach (var action in Actions) action.Start(gameObj);
        }

        public void Update(GameObject gameObj)
        {
            foreach (var action in Actions) action.Update(gameObj);
        }

        public void End(GameObject gameObj)
        {
            foreach (var action in Actions) action.End(gameObj);
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
            return ConditionGroup.IsFulfilled(gameObj, ConditionType.OneHasToBeTrue, Preconditions);
        }

        public bool EffectOnWorldIsFulfilled(GameObject gameObj)
        {
            return ConditionGroup.IsFulfilled(gameObj, ConditionType.OneHasToBeTrue, EffectOnWorld);
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
