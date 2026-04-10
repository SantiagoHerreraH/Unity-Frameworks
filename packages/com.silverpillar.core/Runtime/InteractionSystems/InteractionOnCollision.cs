using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Core
{
#nullable enable

    [Serializable]
    public class Interaction_Collision : ICollision
    {
        [OdinSerialize, ShowInInspector]
        private List<IInteraction> m_Interactions = new();
        private GameObject? m_Self;

        public bool SetSelf(GameObject gameObject)
        {
            if (m_Self != null) return false;
            m_Self = gameObject;
            foreach (var interaction in m_Interactions) interaction.SetSelf(gameObject);
            return true;
        }

        public GameObject? GetSelf() => m_Self;

        public void Collide(Collision collision)
        {
            foreach (var interaction in m_Interactions) interaction.Interact(collision.gameObject);
        }
    }

    [Serializable]
    public class Interaction_Collision2D : ICollision2D
    {
        [OdinSerialize, ShowInInspector]
        private List<IInteraction> m_Interactions = new();
        private GameObject? m_Self;

        public bool SetSelf(GameObject gameObject)
        {
            if (m_Self != null) return false;
            m_Self = gameObject;
            foreach (var interaction in m_Interactions) interaction.SetSelf(gameObject);
            return true;
        }

        public GameObject? GetSelf() => m_Self;

        public void Collide(Collision2D collision)
        {
            foreach (var interaction in m_Interactions) interaction.Interact(collision.gameObject);
        }
    }

    [Serializable]
    public class Interaction_Trigger : ITriggerCollision
    {
        [OdinSerialize, ShowInInspector]
        private List<IInteraction> m_Interactions = new();
        private GameObject? m_Self;

        public bool SetSelf(GameObject gameObject)
        {
            if (m_Self != null) return false;
            m_Self = gameObject;
            foreach (var interaction in m_Interactions) interaction.SetSelf(gameObject);
            return true;
        }

        public GameObject? GetSelf() => m_Self;

        public void Collide(Collider collider)
        {
            foreach (var interaction in m_Interactions) interaction.Interact(collider.gameObject);
        }
    }

    [Serializable]
    public class Interaction_Trigger2D : ITriggerCollision2D
    {
        [OdinSerialize, ShowInInspector]
        private List<IInteraction> m_Interactions = new();
        private GameObject? m_Self;

        public bool SetSelf(GameObject gameObject)
        {
            if (m_Self != null) return false;
            m_Self = gameObject;
            foreach (var interaction in m_Interactions) interaction.SetSelf(gameObject);
            return true;
        }

        public GameObject? GetSelf() => m_Self;

        public void Collide(Collider2D collider)
        {
            foreach (var interaction in m_Interactions) interaction.Interact(collider.gameObject);
        }
    }
}
