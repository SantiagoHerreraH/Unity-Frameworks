using SilverPillar.Core;
using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Tag
{
    public class TagOperation_CachedGameAction : ICachedGameAction
    {
        public enum Operation
        {
            AddTags,
            RemoveTags
        }
        [SerializeField]
        private Operation m_Operation;
        [SerializeField]
        List<Tag> m_Tags = new List<Tag>();

#nullable enable
        private TagHolder? m_TagHolder;

        public TagOperation_CachedGameAction() { }
        public TagOperation_CachedGameAction(TagOperation_CachedGameAction other)
        {
            m_Operation = other.m_Operation;
            m_Tags = new(other.m_Tags);
            m_TagHolder = other.m_TagHolder;
        }

        public ICachedGameAction Clone()
        {
            return new TagOperation_CachedGameAction(this);
        }

        public void Execute()
        {
            if (m_TagHolder == null)
            {
                return;
            }
            switch (m_Operation)
            {
                case Operation.AddTags:

                    foreach (var item in m_Tags)
                    {
                        m_TagHolder.AddTag(item);
                    }

                    break;
                case Operation.RemoveTags:

                    foreach (var item in m_Tags)
                    {
                        m_TagHolder.RemoveTag(item);
                    }

                    break;
                default:
                    break;
            }

            
        }

        public GameObject? GetGameObject()
        {
            return m_TagHolder?.gameObject;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            return gameObj.TryGetComponent(out m_TagHolder);
        }
    }
}
