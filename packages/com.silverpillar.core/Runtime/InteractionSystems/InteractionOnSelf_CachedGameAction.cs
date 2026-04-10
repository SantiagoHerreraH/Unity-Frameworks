using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Core
{
#nullable enable

    [Serializable]
    public class InteractionOnSelf_CachedGameAction : ICachedGameAction
    {
        [OdinSerialize, ShowInInspector]
        private List<IInteraction> m_InteractionsOnSelf = new();

        private GameObject? m_Self;

        public bool SetGameObject(GameObject gameObj)
        {
            if (m_Self != null) return false;
            m_Self = gameObj;

            foreach (var interaction in m_InteractionsOnSelf)
            {
                interaction.SetSelf(gameObj);
            }
            return true;
        }

        public GameObject? GetGameObject() => m_Self;

        public void Execute()
        {
            if (m_Self == null) return;

            foreach (var interaction in m_InteractionsOnSelf)
            {
                interaction.Interact(m_Self);
            }
        }

        public ICachedGameAction Clone()
        {
            var clone = new InteractionOnSelf_CachedGameAction
            {
                // Copiamos la lista de interacciones (referencias)
                m_InteractionsOnSelf = new List<IInteraction>(m_InteractionsOnSelf)
            };
            return clone;
        }
    }
}
