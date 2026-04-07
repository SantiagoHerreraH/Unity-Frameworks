using Sirenix.Serialization;
using System.Collections.Generic;
using UnityEngine;
using SilverPillar.Core;
using Sirenix.OdinInspector;
using System.Linq;

namespace SilverPillar.GOAP
{
    [CreateAssetMenu(fileName = "BehaviorActionList", menuName = "SilverPillar/GOAP/BehaviorActionList")]
    public class BehaviorActionList : SaveableScriptableObject
    {
        [OdinSerialize, ShowInInspector]
        private List<BehaviorAction> m_PossibleActions = new();
        public List<BehaviorAction> PossibleActions { get { return m_PossibleActions; } }

        private void OnValidate()
        {
            m_PossibleActions = m_PossibleActions.Distinct().ToList();
        }

        public BehaviorActionListInstance CreateInstance(GameObject gameObj)
        {
            return new BehaviorActionListInstance(this, gameObj);
        }

#if UNITY_EDITOR
        [Button("Retrieve All Behaviour Actions", ButtonSizes.Medium)]
        private void RetrieveAllEditorOnly()
        {
            ScriptableObjectRegistry.Instance.RefreshCacheEditorOnly();
            m_PossibleActions.Clear();
            m_PossibleActions = ScriptableObjectRegistry.Instance.GetAllOfType<BehaviorAction>();
        }
#endif
    }

    public class BehaviorActionListInstance
    {
        private List<BehaviorActionInstance> m_Instances = new();
        private Dictionary<BehaviorAction, BehaviorActionInstance> m_Action_To_Instance = new();

        private List<BehaviorAction> m_CurrentPossibleActions = new();
        private List<BehaviorAction> m_ActionsThatLeadToGoal = new();
        public BehaviorActionListInstance(BehaviorActionList actionList, GameObject gameObject)
        {
            foreach (var action in actionList.PossibleActions)
            {
                var instance = action.CreateInstance(gameObject);
                m_Instances.Add(instance);
                m_Action_To_Instance.Add(action, instance);
            }
        }

        public BehaviorActionInstance GetInstance(BehaviorAction action)
        {
            return m_Action_To_Instance[action];
        }

        public BehaviorActionInstance GetRandomInstance()
        {
            return m_Instances.FirstOrDefault();
        }

        public List<BehaviorAction> GetCurrentPossibleActions()
        {
            m_CurrentPossibleActions.Clear();

            foreach (var actionInstance in m_Instances)
            {
                if (actionInstance.PreconditionsAreFulfilled())
                {
                    m_CurrentPossibleActions.Add(actionInstance.Action);
                }
            }

            return m_CurrentPossibleActions;
        }

        public List<BehaviorAction> GetActionsThatLeadToGoal(CachedCondition chosenGoal)
        {
            m_ActionsThatLeadToGoal.Clear();

            foreach (var actionInstance in m_Instances)
            {
                if (actionInstance.Action.HasEffectOnWorld(chosenGoal))
                {
                    m_ActionsThatLeadToGoal.Add(actionInstance.Action);
                }
            }

            return m_ActionsThatLeadToGoal;
        }
    }

}
