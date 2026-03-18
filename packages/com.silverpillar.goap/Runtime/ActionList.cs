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
        public HashSet<Action> PossibleActions = new();
    }

    public class ActionExecutionData : IAction
    {
        private List<IAction> m_ExecutableActions = new();
        public ActionExecutionData(List<IAction> actionList, GameObject gameObject)
        {
            foreach (var action in actionList)
            {
                var clone = action.Clone();
                clone.SetGameObject(gameObject);

                m_ExecutableActions.Add(clone);
            }
        }

        public ActionExecutionData()
        {

        }

        public ActionExecutionData(ActionExecutionData other)
        {
            foreach (var action in other.m_ExecutableActions)
            {
                m_ExecutableActions.Add(action.Clone());
            }
        }

        public IAction Clone()
        {
            return new ActionExecutionData(this);
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
}
