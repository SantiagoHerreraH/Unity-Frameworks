using SilverPillar.Core;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace SilverPillar.Target
{
    [Serializable]
    public class ChangeCurrentTarget_CachedGameAction : ICachedGameAction
    {
        [SerializeField]
        private SelfType m_WhichTargetSystemToUse;
        [SerializeField, ShowIf(nameof(m_WhichTargetSystemToUse), SelfType.CustomGameObject)] 
        private TargetSystem m_TargetSystem;
        [SerializeField]
        private GameObject m_Target;

        private GameObject m_Self;

        public ChangeCurrentTarget_CachedGameAction()
        {

        }

        public ChangeCurrentTarget_CachedGameAction(ChangeCurrentTarget_CachedGameAction other)
        {
            m_WhichTargetSystemToUse = other.m_WhichTargetSystemToUse;
            m_Target = other.m_Target;
            m_Self = other.m_Self;
            m_TargetSystem = other.m_TargetSystem;
        }

        public ICachedGameAction Clone()
        {
            return new ChangeCurrentTarget_CachedGameAction(this);
        }

        public void Execute()
        {
            if (m_TargetSystem != null)
            {
                m_TargetSystem.ChangeCurrentTarget(m_Target);
            }
        }

        public GameObject GetGameObject()
        {
            return m_Self;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            m_Self = gameObj;
            if (gameObj != null && m_WhichTargetSystemToUse == SelfType.ThisGameObject)
            {
                m_TargetSystem = gameObj.GetComponent<TargetSystem>();
            }

            return m_Self != null && m_TargetSystem != null;
        }
    }
}
