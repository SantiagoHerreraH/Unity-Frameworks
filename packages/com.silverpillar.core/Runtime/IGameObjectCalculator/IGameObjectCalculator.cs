using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Core
{
    public interface IGameObjectCalculator
    {
        public bool SetGameObject(GameObject gameObject);
        public GameObject GetGameObject();
        public GameObject CalculateGameObject();
        public IGameObjectCalculator Clone();
    }

    [Serializable]
    public struct GameObjectCalculatorData
    {
        [OdinSerialize, ShowInInspector]
        private IGameObjectCalculator m_GameObjectCalculator;
        [SerializeField]
        private SelfType m_WhereToGetCalculatorGameObjectInputFrom;
        [SerializeField, ShowIf(nameof(m_WhereToGetCalculatorGameObjectInputFrom))]
        private GameObject m_InputGameObject;

        public bool IsValid()
        {
            return m_GameObjectCalculator != null && m_InputGameObject != null;
        }

        public GameObjectCalculatorData CloneData()
        {
            return new GameObjectCalculatorData { 
                m_GameObjectCalculator = m_GameObjectCalculator.Clone(), 
                m_WhereToGetCalculatorGameObjectInputFrom  = m_WhereToGetCalculatorGameObjectInputFrom,
                m_InputGameObject = m_InputGameObject
            };
        }


        public bool SetGameObject(GameObject gameObject)
        {
            switch (m_WhereToGetCalculatorGameObjectInputFrom)
            {
                case SelfType.ThisGameObject:
                    m_InputGameObject = gameObject;
                    return m_GameObjectCalculator.SetGameObject(gameObject);
                case SelfType.CustomGameObject:
                    return m_GameObjectCalculator.SetGameObject(m_InputGameObject);
                default:
                    break;
            }

            return m_GameObjectCalculator.SetGameObject(gameObject);

        }
        public GameObject GetGameObject()
        {
            return m_GameObjectCalculator.GetGameObject();
        }
        public GameObject CalculateGameObject()
        {
            return m_GameObjectCalculator.CalculateGameObject();
        }
        public IGameObjectCalculator Clone()
        {
            return m_GameObjectCalculator.Clone();    
        }
    }
}
