using Sirenix.Serialization;
using System.Collections.Generic;
using UnityEngine;
using SilverPillar.Core;
using Sirenix.OdinInspector;
using System.Linq;

namespace SilverPillar.GOAP
{
    [CreateAssetMenu(fileName = "BehaviorActionList", menuName = "SilverPillar/GOAP/BehaviorActionList")]
    public class UndoableBehaviorActionList : SaveableScriptableObject
    {
        [OdinSerialize, ShowInInspector]
        private List<UndoableBehaviorAction> m_PossibleActions = new();
        public List<UndoableBehaviorAction> PossibleActions { get { return m_PossibleActions; } }

        private void OnValidate()
        {
            m_PossibleActions = m_PossibleActions.Distinct().ToList();
        }

        public UndoableBehaviorActionListInstance CreateInstance(GameObject gameObj)
        {
            return new UndoableBehaviorActionListInstance(this, gameObj);
        }

#if UNITY_EDITOR
        [Button("Retrieve All Behaviour Actions", ButtonSizes.Medium)]
        private void RetrieveAllEditorOnly()
        {
            ScriptableObjectRegistry.Instance.RefreshCacheEditorOnly();
            m_PossibleActions.Clear();
            m_PossibleActions = ScriptableObjectRegistry.Instance.GetAllOfType<UndoableBehaviorAction>();
        }
#endif
    }

    public class UndoableBehaviorActionListInstance
    {
        private List<UndoableBehaviorActionInstance> m_Instances = new();
        private Dictionary<UndoableBehaviorAction, UndoableBehaviorActionInstance> m_Action_To_Instance = new();

        private List<UndoableBehaviorAction> m_CurrentPossibleActions = new();
        private List<UndoableBehaviorAction> m_ActionsThatLeadToGoal = new();
        public UndoableBehaviorActionListInstance(UndoableBehaviorActionList actionList, GameObject gameObject)
        {
            foreach (var action in actionList.PossibleActions)
            {
                var instance = action.CreateInstance(gameObject);
                m_Instances.Add(instance);
                m_Action_To_Instance.Add(action, instance);
            }
        }

        public UndoableBehaviorActionInstance GetInstance(UndoableBehaviorAction action)
        {
            return m_Action_To_Instance[action];
        }

        public UndoableBehaviorActionInstance GetFirstInstance()
        {
            return m_Instances.FirstOrDefault();
        }

        public bool SetGameObject(GameObject gameObj)
        {
            bool allGood = true;
            foreach (var action in m_Instances)
            {
                allGood &= action.SetGameObject(gameObj);
            }

            return allGood;
        }

        public List<UndoableBehaviorAction> GetCurrentPossibleActions()
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

        public List<UndoableBehaviorAction> GetActionsThatLeadToGoal(CachedCondition chosenGoal)
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
