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
        [SerializeField]
        private SelfType m_FromWhoToGetTargetSystem;
        [SerializeField, ShowIf(nameof(m_FromWhoToGetTargetSystem), SelfType.CustomGameObject)]
        private TargetSystem? m_TargetSystem;
        [OdinSerialize, ShowInInspector]
        private List<IInteraction> m_Interactions = new();

        private GameObject? m_Self;

        public InteractWithCurrentTarget_CachedGameAction() { }

        public InteractWithCurrentTarget_CachedGameAction(InteractWithCurrentTarget_CachedGameAction other)
        {
            m_Interactions = other.m_Interactions.Select(i => i.Clone()).ToList();
            m_TargetSystem = other.m_TargetSystem;
            m_Self = other.m_Self;
            m_FromWhoToGetTargetSystem = other.m_FromWhoToGetTargetSystem;
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
            return m_Self;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            m_Self = gameObj;

            if (m_FromWhoToGetTargetSystem == SelfType.ThisGameObject)
            {
                gameObj.TryGetComponent(out m_TargetSystem);
            }

            if (m_TargetSystem != null)
            {
                foreach (var interaction in m_Interactions)
                {
                    interaction.SetSelf(m_TargetSystem.gameObject);
                }

                return m_Self != null;
            }
            
            return false;
        }
    }
}
