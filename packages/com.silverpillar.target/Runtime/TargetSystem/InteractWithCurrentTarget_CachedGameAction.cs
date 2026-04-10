using SilverPillar.Core;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SilverPillar.Target
{

#nullable enable

    [Serializable]
    public class InteractWithCurrentTarget_CachedGameAction : ICachedGameAction
    {
        [OdinSerialize, ShowInInspector]
        private List<IInteraction> m_Interactions = new();

        private TargetSystem? m_TargetSystem;

        public InteractWithCurrentTarget_CachedGameAction() { }

        public InteractWithCurrentTarget_CachedGameAction(InteractWithCurrentTarget_CachedGameAction other)
        {
            m_Interactions = other.m_Interactions.Select(i => i.Clone()).ToList();
            m_TargetSystem = other.m_TargetSystem;
        }

        public ICachedGameAction Clone()
        {
            return new InteractWithCurrentTarget_CachedGameAction(this);
        }

        public void Execute()
        {
            if (m_TargetSystem == null || m_TargetSystem.CurrentTarget == null) return;

            foreach (var interaction in m_Interactions)
            {
                interaction.Interact(m_TargetSystem.CurrentTarget);
            }
        }

        public GameObject? GetGameObject()
        {
            return m_TargetSystem != null ? m_TargetSystem.gameObject : null;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            if (gameObj.TryGetComponent(out m_TargetSystem))
            {
                foreach (var interaction in m_Interactions)
                {
                    interaction.SetSelf(gameObj);
                }
                return true;
            }
            return false;
        }
    }
}
