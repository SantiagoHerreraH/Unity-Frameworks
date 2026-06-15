using SilverPillar.Core;
using System;
using UnityEngine;

namespace SilverPillar.Tag
{
    [Serializable]
    public class TagAsString : IString
    {
        [SerializeField]
        private Tag m_Tag;
        private GameObject m_GameObject;

        public TagAsString() { }
        public TagAsString(TagAsString other)
        {
            m_GameObject = other.m_GameObject;
            m_Tag = other.m_Tag;
        }

        public string CalculateString()
        {
            return m_Tag.name;
        }

        public IString Clone()
        {
            return new TagAsString(this);
        }

#nullable enable

        public GameObject? GetGameObject()
        {
            return m_GameObject;
        }

        public bool SetGameObject(GameObject gameObject)
        {
            if (gameObject == null) return false;
            m_GameObject = gameObject;
            return true;
        }
    }
}
