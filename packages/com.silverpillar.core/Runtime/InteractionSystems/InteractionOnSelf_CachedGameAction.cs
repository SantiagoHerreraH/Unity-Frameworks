using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Core
{
#nullable enable

    [Serializable]
    public class Interaction_CachedGameAction : ICachedGameAction
    {
        [SerializeField]
        private SelfType m_WhoIsSelf;
        [SerializeField, ShowIf(nameof(m_WhoIsSelf), SelfType.CustomGameObject)]
        private GameObject? m_CustomSelf;
        [SerializeField]
        private SelfType m_InteractionTarget;
        [SerializeField, ShowIf(nameof(m_InteractionTarget), SelfType.CustomGameObject)]
        private GameObject? m_Target;
        [OdinSerialize, ShowInInspector]
        private List<IInteraction> m_Interactions = new();


        private GameObject? m_Self;

        public Interaction_CachedGameAction() { }
        public Interaction_CachedGameAction(Interaction_CachedGameAction other)
        {
            m_InteractionTarget = other.m_InteractionTarget;
            m_Target = other.m_Target;
            m_Self = other.m_Self;
            m_WhoIsSelf = other.m_WhoIsSelf;
            m_CustomSelf = other.m_CustomSelf;

            if (other.m_Interactions != null)
            {
                m_Interactions ??= new();
                foreach (var item in other.m_Interactions)
                {
                    if (item != null)
                    {
                        m_Interactions.Add(item.Clone());
                    }
                }
            }
        }


        public bool SetGameObject(GameObject gameObj)
        {
            m_Self = gameObj;
            if (m_WhoIsSelf == SelfType.CustomGameObject)
            {
                if (m_CustomSelf == null)
                {
                    Debug.LogError($"{nameof(m_CustomSelf)} is null in {nameof(Interaction_CachedGameAction)}");
                    return false;
                }
            }
            else
            {
                m_CustomSelf = gameObj;
            }

            if (m_InteractionTarget == SelfType.CustomGameObject)
            {
                if (m_Target == null)
                {
                    Debug.LogError($"{nameof(m_CustomSelf)} is null in {nameof(Interaction_CachedGameAction)}");
                    return false;
                }
            }
            else
            {
                m_Target = gameObj;
            }

            bool allGood = true;

            foreach (var interaction in m_Interactions)
            {
                allGood &= interaction.SetSelf(m_CustomSelf);
            }
            return allGood;
        }

        public GameObject? GetGameObject() => m_Self;

        public void Execute()
        {
            if (m_Target == null) return;

            foreach (var interaction in m_Interactions)
            {
                interaction.Interact(m_Target);
            }
        }

        public ICachedGameAction Clone()
        {
            return new Interaction_CachedGameAction(this);
        }
    }
}
