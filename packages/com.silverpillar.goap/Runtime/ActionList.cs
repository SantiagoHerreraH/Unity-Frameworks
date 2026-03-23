using Sirenix.Serialization;
using System.Collections.Generic;
using UnityEngine;
using SilverPillar.Core;
using Sirenix.OdinInspector;
using System.Linq;

namespace SilverPillar.GOAP
{
    [CreateAssetMenu(fileName = "ActionList", menuName = "SilverPillar/GOAP/ActionList")]
    public class ActionList : SaveableScriptableObject
    {
        [OdinSerialize, ShowInInspector]
        private List<Action> m_PossibleActions = new();
        public List<Action> PossibleActions { get { return m_PossibleActions; } }

        private void OnValidate()
        {
            m_PossibleActions = m_PossibleActions.Distinct().ToList();
        }

        public ActionListInstance CreateInstance(GameObject gameObj)
        {
            return new ActionListInstance(this, gameObj);
        }
    }

    public class ActionListInstance
    {
        private List<ActionInstance> m_Instances = new();
        private Dictionary<Action, ActionInstance> m_Action_To_Instance = new();

        private List<Action> m_CurrentPossibleActions = new();
        private List<Action> m_ActionsThatLeadToGoal = new();
        public ActionListInstance(ActionList actionList, GameObject gameObject)
        {
            foreach (var action in actionList.PossibleActions)
            {
                var instance = action.CreateInstance(gameObject);
                m_Instances.Add(instance);
                m_Action_To_Instance.Add(action, instance);
            }
        }

        public ActionInstance GetInstance(Action action)
        {
            return m_Action_To_Instance[action];
        }

        public ActionInstance GetRandomInstance()
        {
            return m_Instances.FirstOrDefault();
        }

        public List<Action> GetCurrentPossibleActions()
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

        public List<Action> GetActionsThatLeadToGoal(CachedCondition chosenGoal)
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
