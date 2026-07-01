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
        [SerializeField]
        private SelfType m_WhoIsSelf;
        [SerializeField, ShowIf(nameof(m_WhoIsSelf), SelfType.CustomGameObject)]
        private GameObject? m_CustomSelf;
        [OdinSerialize, ShowInInspector]
        private List<IInteraction> m_InteractionsOnSelf = new();


        private GameObject? m_Self;

        public InteractionOnSelf_CachedGameAction() { }
        public InteractionOnSelf_CachedGameAction(InteractionOnSelf_CachedGameAction other)
        {
            m_WhoIsSelf = other.m_WhoIsSelf;
            m_CustomSelf = other.m_CustomSelf;
            m_Self = other.m_Self;

            if (other.m_InteractionsOnSelf != null)
            {
                m_InteractionsOnSelf ??= new();
                foreach (var item in other.m_InteractionsOnSelf)
                {
                    if (item != null)
                    {
                        m_InteractionsOnSelf.Add(item.Clone());
                    }
                }
            }
        }


        public bool SetGameObject(GameObject gameObj)
        {
            if (m_CustomSelf != null) return false;
            m_CustomSelf = gameObj;

            foreach (var interaction in m_InteractionsOnSelf)
            {
                interaction.SetSelf(gameObj);
            }
            return true;
        }

        public GameObject? GetGameObject() => m_CustomSelf;

        public void Execute()
        {
            if (m_CustomSelf == null) return;

            foreach (var interaction in m_InteractionsOnSelf)
            {
                interaction.Interact(m_CustomSelf);
            }
        }

        public ICachedGameAction Clone()
        {
            return new InteractionOnSelf_CachedGameAction(this);
        }
    }
}
