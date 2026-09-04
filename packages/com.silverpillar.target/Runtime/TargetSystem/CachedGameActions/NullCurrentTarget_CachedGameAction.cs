using UnityEngine;
using SilverPillar.Core;
using System;

namespace SilverPillar.Target
{
    [Serializable]
    public class NullCurrentTarget_CachedGameAction : ICachedGameAction
    {
        private TargetSystem m_TargetSystem;

        public NullCurrentTarget_CachedGameAction() { }
        public NullCurrentTarget_CachedGameAction(NullCurrentTarget_CachedGameAction other)
        {
            m_TargetSystem = other.m_TargetSystem;
        }

        public ICachedGameAction Clone()
        {
            throw new System.NotImplementedException();
        }

        public void Execute()
        {
            if (m_TargetSystem != null)
            {
                m_TargetSystem.NullCurrentTarget();
            }
        }

#nullable enable
        public GameObject? GetGameObject()
        {
            return m_TargetSystem != null ? m_TargetSystem.gameObject : null;
        }
#nullable disable

        public bool SetGameObject(GameObject gameObj)
        {
            return gameObj.TryGetComponent(out m_TargetSystem);
        }
    }
}
