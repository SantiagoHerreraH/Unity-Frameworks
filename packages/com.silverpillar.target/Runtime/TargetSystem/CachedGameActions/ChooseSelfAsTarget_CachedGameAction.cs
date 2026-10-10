using SilverPillar.Core;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace SilverPillar.Target
{
    [Serializable]
    public class ChooseSelfAsTarget_CachedGameAction : ICachedGameAction
    {
        [SerializeField]
        private SelfType m_TargetSystemFrom;
        [SerializeField, ShowIf(nameof(m_TargetSystemFrom), SelfType.CustomGameObject)]
        private TargetSystem m_TargetSystem;
        private GameObject m_Self;

        public ChooseSelfAsTarget_CachedGameAction()
        {

        }

        public ChooseSelfAsTarget_CachedGameAction(ChooseSelfAsTarget_CachedGameAction other)
        {
            m_TargetSystemFrom = other.m_TargetSystemFrom;
            m_TargetSystem = other.m_TargetSystem;
            m_Self = other.m_Self;
        }

        public ICachedGameAction Clone()
        {
            return new ChooseSelfAsTarget_CachedGameAction(this);
        }

        public void Execute()
        {
            m_TargetSystem?.ChangeCurrentTarget(m_Self);
        }

        public GameObject GetGameObject()
        {
            return m_Self;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            m_Self = gameObj;
            if (m_TargetSystemFrom == SelfType.ThisGameObject)
            {
                gameObj.TryGetComponent(out m_TargetSystem);
            }

            return m_Self != null && m_TargetSystem != null;
        }
    }
}
