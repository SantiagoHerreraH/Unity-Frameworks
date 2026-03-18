using Sirenix.Serialization;
using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Core
{

    class GameAction_Action : IAction
    {
        public enum WhenToTriggerActions
        {
            Start,
            Update,
            End
        }

        [SerializeField]
        private bool m_CheckIfCanExecuteBeforeExecuting;
        [SerializeField]
        private WhenToTriggerActions m_WhenToTriggerActions;
        [OdinSerialize]
        private List<IGameAction> m_GameActions = new();

        private GameObject m_GameObject = null;


        public GameAction_Action()
        {

        }

        public GameAction_Action(GameAction_Action other)
        {
            m_CheckIfCanExecuteBeforeExecuting = other.m_CheckIfCanExecuteBeforeExecuting;
            m_WhenToTriggerActions = other.m_WhenToTriggerActions;
            m_GameActions = new(other.m_GameActions); 
            m_GameObject = other.m_GameObject;  
        }

        public bool SetGameObject(GameObject gameObj)
        {
            if (gameObj != null)
            {
                m_GameObject = gameObj;
                return true;
            }

            return false;
        }

        public GameObject GetGameObject()
        {
            return m_GameObject;
        }

        public void StartAction()
        {
            if (m_WhenToTriggerActions == WhenToTriggerActions.Start)
            {
                Execute();
            }
        }

        public void UpdateAction()
        {
            if (m_WhenToTriggerActions == WhenToTriggerActions.Update)
            {
                Execute();
            }
        }

        public void EndAction()
        {
            if (m_WhenToTriggerActions == WhenToTriggerActions.Update)
            {
                Execute();
            }
        }

        public IAction Clone()
        {
            return new GameAction_Action(this);
        }

        private void Execute()
        {
            foreach (var gameAction in m_GameActions)
            {
                if (m_CheckIfCanExecuteBeforeExecuting)
                {
                    if (gameAction.CanExecute(m_GameObject))
                    {
                        gameAction.Execute(m_GameObject);
                    }
                }
                else
                {
                    gameAction.Execute(m_GameObject);
                }
            }
        }
    }


    class CachedGameAction_Action : IAction
    {
        public enum WhenToTriggerActions
        {
            Start,
            Update,
            End
        }

        [SerializeField]
        private bool m_CheckIfCanExecuteBeforeExecuting;
        [SerializeField]
        private WhenToTriggerActions m_WhenToTriggerActions;
        [OdinSerialize]
        private List<ICachedGameAction> m_GameActions = new();

        private GameObject m_GameObject = null;


        public CachedGameAction_Action()
        {

        }

        public CachedGameAction_Action(CachedGameAction_Action other)
        {
            m_CheckIfCanExecuteBeforeExecuting = other.m_CheckIfCanExecuteBeforeExecuting;
            m_WhenToTriggerActions = other.m_WhenToTriggerActions;

            m_GameObject = other.m_GameObject;

            foreach (var gameAction in other.m_GameActions)
            {
                m_GameActions.Add(gameAction.Clone());
            }
        }

        public bool SetGameObject(GameObject gameObj)
        {
            if (gameObj != null)
            {
                m_GameObject = gameObj;

                foreach (var item in m_GameActions)
                {
                    item.SetGameObject(gameObj);
                }

                return true;
            }

            return false;
        }

        public GameObject GetGameObject()
        {
            return m_GameObject;
        }

        public void StartAction()
        {
            if (m_WhenToTriggerActions == WhenToTriggerActions.Start)
            {
                Execute();
            }
        }

        public void UpdateAction()
        {
            if (m_WhenToTriggerActions == WhenToTriggerActions.Update)
            {
                Execute();
            }
        }

        public void EndAction()
        {
            if (m_WhenToTriggerActions == WhenToTriggerActions.Update)
            {
                Execute();
            }
        }

        public IAction Clone()
        {
            return new CachedGameAction_Action(this);
        }

        private void Execute()
        {
            foreach (var gameAction in m_GameActions)
            {
                gameAction.Execute();
            }
        }
    }
}

