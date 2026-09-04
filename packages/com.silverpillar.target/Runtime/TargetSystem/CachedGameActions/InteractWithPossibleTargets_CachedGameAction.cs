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
    public class InteractWithPossibleTargets_CachedGameAction : ICachedGameAction
    {
        [OdinSerialize, ShowInInspector]
        private List<IInteraction> m_Interactions = new();

        private TargetSystem? m_TargetSystem;

        public InteractWithPossibleTargets_CachedGameAction() { }

        public InteractWithPossibleTargets_CachedGameAction(InteractWithPossibleTargets_CachedGameAction other)
        {
            m_Interactions = other.m_Interactions.Select(i => i.Clone()).ToList();
            m_TargetSystem = other.m_TargetSystem;
        }

        public ICachedGameAction Clone()
        {
            return new InteractWithPossibleTargets_CachedGameAction(this);
        }

        public void Execute()
        {
            if (m_TargetSystem == null) return;

            foreach (var possibleTarget in m_TargetSystem.PossibleTargets)
            {
                foreach (var interaction in m_Interactions)
                {
                    interaction.Interact(possibleTarget);
                }
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
