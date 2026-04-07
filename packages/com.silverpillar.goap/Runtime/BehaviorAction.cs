using SilverPillar.Core;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SilverPillar.GOAP
{
    [CreateAssetMenu(fileName = "BehaviorAction", menuName = "SilverPillar/GOAP/BehaviorAction")]
    public class BehaviorAction : SaveableScriptableObject, IScore
    {
        [TabGroup("Graph Connections")]
        [Title("Preconditions")]
        [SerializeField]
        private PositiveConditionType m_PreconditionsType;
        public PositiveConditionType PreconditionsType { get { return m_PreconditionsType; } }

        [TabGroup("Graph Connections")]
        [SerializeField, Tooltip("If these are and conditions you have to make sure the children in conjunction lead to all conditions being true")]
        private List<CachedCondition> m_Preconditions = new(); 
        public List<CachedCondition> Preconditions {  get { return m_Preconditions; } }

        [TabGroup("Graph Connections")]
        [Title("Effects On World")]
        [SerializeField]
        private List<CachedCondition> m_EffectOnWorld = new(); 
        public List<CachedCondition> EffectOnWorld { get { return m_EffectOnWorld; } }

        [TabGroup("Action Choosing Criteria")]
        [Title("Action Cost")]
        [Min(0.1f), SerializeField]
        private float m_Cost = 1;
        public float Cost { get { return m_Cost; } }


        [TabGroup("Action Choosing Criteria")]
        [Title("How to Score Action")]
        [OdinSerialize, ShowInInspector]
        private IScore m_Score;

        [TabGroup("Action Data")]
        [OdinSerialize, ShowInInspector]
        private List<IAction> m_Actions = new();
        public List<IAction> Actions {  get { return m_Actions; } }

        public float CalculateScore(GameObject gameObject)
        {
            if (m_Score == null)
            {
                return 0f;
            }
            return m_Score.CalculateScore(gameObject);
        }

        public BehaviorActionInstance CreateInstance(GameObject gameObject)
        {
            return new BehaviorActionInstance(this, gameObject);
        }
        public bool IsChildrenActionOfOther(BehaviorAction otherAction)
        {
            foreach (var otherEffectOnWorld in otherAction.m_EffectOnWorld)
                foreach (var selfPreCondition in m_Preconditions)
                    if (otherEffectOnWorld == selfPreCondition) return true;

            return false;
        }

        public bool IsParentActionOfOther(BehaviorAction otherAction)
        {
            foreach (var selfEffectOnWorld in m_EffectOnWorld)
                foreach (var otherPreCondition in otherAction.m_Preconditions)
                    if (selfEffectOnWorld == otherPreCondition) return true;

            return false;
        }

        public bool HasPrecondition(CachedCondition condition)
        {
            return m_Preconditions.Contains(condition);
        }

        public bool HasEffectOnWorld(CachedCondition condition)
        {
            return m_EffectOnWorld.Contains(condition);
        }
    }


    public class BehaviorActionInstance : IAction
    {
        private List<IAction> m_ExecutableActions = new();
        private List<ICachedCondition> m_Preconditions = new();
        private PositiveConditionType m_PreconditionsType;

        private BehaviorAction m_Action;
        public BehaviorAction Action { get { return m_Action; } }

        public BehaviorActionInstance(BehaviorAction action, GameObject gameObject)
        {
            m_Action = action;
            var actions = action.Actions;

            foreach (var possibleAction in actions)
            {
                var clone = possibleAction.Clone();
                clone.SetGameObject(gameObject);

                m_ExecutableActions.Add(clone);
            }


            var preconditions = action.Preconditions;

            foreach (var condition in preconditions)
            {
                var clone = condition.Clone();
                clone.SetGameObject(gameObject);

                m_Preconditions.Add(clone);
            }

            m_PreconditionsType = action.PreconditionsType;
        }

        public BehaviorActionInstance()
        {

        }

        public BehaviorActionInstance(BehaviorActionInstance other)
        {
            m_PreconditionsType = other.m_PreconditionsType;

            foreach (var action in other.m_ExecutableActions)
            {
                m_ExecutableActions.Add(action.Clone());
            }

            foreach (var condition in other.m_Preconditions)
            {
                m_Preconditions.Add(condition.Clone());
            }
        }

        public IAction Clone()
        {
            return new BehaviorActionInstance(this);
        }
        public GameObject GetGameObject()
        {
            if (m_ExecutableActions.Count > 0)
            {
                return m_ExecutableActions.First().GetGameObject();
            }

            return null;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            bool allGood = true;
            foreach (var action in m_ExecutableActions)
            {
                if (action.SetGameObject(gameObj))
                {
                    allGood = false;
                }
            }

            return allGood;
        }

        public bool PreconditionsAreFulfilled()
        {
            if (m_Preconditions.Count == 0)
            {
                return true;
            }

            return CachedConditions.IsFulfilled(m_PreconditionsType, m_Preconditions);
        }

        public void EndAction()
        {
            foreach (var action in m_ExecutableActions)
            {
                action.EndAction();
            }
        }

        public void StartAction()
        {
            foreach (var action in m_ExecutableActions)
            {
                action.StartAction();
            }
        }

        public void UpdateAction()
        {
            foreach (var action in m_ExecutableActions)
            {
                action.UpdateAction();
            }
        }
    }

    public class ActionNode
    {
        public BehaviorAction Action;
        public List<BehaviorAction> Parents = new();
        public List<BehaviorAction> Children = new();
    }
}
