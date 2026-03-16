using Sirenix.Serialization;
using System.Collections.Generic;
using UnityEngine;
using SilverPillar.Core;

namespace SilverPillar.GOAP
{
    public interface IAction
    {
        public bool Initialize();
        public bool Start(GameObject gameObject);
        public bool Update(GameObject gameObject);
        public bool End(GameObject gameObject);
    }

    class GameAction : IAction
    {
        [OdinSerialize]
        private List<IGameAction> m_GameActionData = new();

        private Dictionary<GameObject, List<Coroutine>> m_GameObject_To_Coroutine;

        public bool Initialize()
        {
            m_GameObject_To_Coroutine.Clear();
            return true;
        }

        public bool End(GameObject gameObject)
        {
            if (m_GameObject_To_Coroutine.ContainsKey(gameObject))
            {
                var coroutineList = m_GameObject_To_Coroutine[gameObject];

                var gameActionMachine = gameObject.GetComponent<GameActionMachine>();

                if (gameActionMachine == null)
                {
                    gameActionMachine = gameObject.AddComponent<GameActionMachine>();
                }

                foreach (var coroutine in coroutineList)
                {
                    gameActionMachine.StopAction(coroutine);
                }

                m_GameObject_To_Coroutine.Remove(gameObject);
            }

            return true;
        }

        

        public bool Start(GameObject gameObject)
        {
            if (!m_GameObject_To_Coroutine.ContainsKey(gameObject))
            {
                m_GameObject_To_Coroutine.Add(gameObject, new());
            }

            var coroutineList = m_GameObject_To_Coroutine[gameObject];

            var gameActionMachine = gameObject.GetComponent<GameActionMachine>();

            if (gameActionMachine == null)
            {
                gameActionMachine = gameObject.AddComponent<GameActionMachine>();
            }

            foreach (var action in m_GameActionData)
            {
                coroutineList.Add(gameActionMachine.StartAction(action));
            }

            return true;
        }

        public bool Update(GameObject gameObject)
        {
            return true;
        }
    }
}

