
using SilverPillar.Core;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace SilverPillar.Target
{
    [Serializable]
    public class RegisterPossibleTargetFromCurrentTarget_CachedGameAction : ICachedGameAction
    {
        [SerializeField]
        private SelfType m_FromWhichTargetSystem;

        [SerializeField, ShowIf(nameof(m_FromWhichTargetSystem), SelfType.CustomGameObject)]
        private TargetSystem m_From;

        [SerializeField]
        private SelfType m_ToWhichTargetSystem;

        [SerializeField, ShowIf(nameof(m_ToWhichTargetSystem), SelfType.CustomGameObject)]
        private TargetSystem m_To;

        [NonSerialized]
        private GameObject m_Self;

        public ICachedGameAction Clone()
        {
            return new RegisterPossibleTargetFromCurrentTarget_CachedGameAction
            {
                m_FromWhichTargetSystem = m_FromWhichTargetSystem,
                m_ToWhichTargetSystem = m_ToWhichTargetSystem,

                m_From = m_FromWhichTargetSystem == SelfType.CustomGameObject
                    ? m_From
                    : null,

                m_To = m_ToWhichTargetSystem == SelfType.CustomGameObject
                    ? m_To
                    : null
            };
        }

        public void Execute()
        {
            if (m_From == null || m_To == null)
            {
                return;
            }

            GameObject currentTarget = m_From.CurrentTarget;

            if (currentTarget != null)
            {
                m_To.RegisterPossibleTarget(currentTarget);
            }
        }

        public GameObject GetGameObject()
        {
            return m_Self;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            m_Self = gameObj;

            if (m_FromWhichTargetSystem != SelfType.CustomGameObject)
            {
                m_From = gameObj != null
                    ? gameObj.GetComponent<TargetSystem>()
                    : null;
            }

            if (m_ToWhichTargetSystem != SelfType.CustomGameObject)
            {
                m_To = gameObj != null
                    ? gameObj.GetComponent<TargetSystem>()
                    : null;
            }

            return m_From != null && m_To != null;
        }
    }
}
