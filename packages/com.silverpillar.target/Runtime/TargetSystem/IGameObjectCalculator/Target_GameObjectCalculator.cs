using SilverPillar.Core;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace SilverPillar.Target
{
    [Serializable]
    public class Target_GameObjectCalculator : IGameObjectCalculator
    {
        [Title("Data")]
        [SerializeField]
        private SelfType m_WhereToGetTargetSystemFrom;

        [SerializeField, ShowIf(nameof(m_WhereToGetTargetSystemFrom), SelfType.CustomGameObject)]
        private TargetSystem m_TargetSystem;


        private GameObject m_Self;


        public GameObject CalculateGameObject()
        {
            if (m_TargetSystem == null)
            {
                return null;
            }

            return m_TargetSystem.CurrentTarget;
        }


        public IGameObjectCalculator Clone()
        {
            return new Target_GameObjectCalculator
            {
                m_WhereToGetTargetSystemFrom = m_WhereToGetTargetSystemFrom,
                m_TargetSystem = m_TargetSystem,
                m_Self = m_Self
            };
        }


        public GameObject GetGameObject()
        {
            return m_Self;
        }


        public bool SetGameObject(GameObject gameObject)
        {
            m_Self = gameObject;

            if (m_Self != null && m_WhereToGetTargetSystemFrom == SelfType.ThisGameObject)
            {
                return m_Self.TryGetComponent(out m_TargetSystem);
            }
            return m_Self != null;
        }
    }
}