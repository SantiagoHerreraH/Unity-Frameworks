using SilverPillar.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SilverPillar.Tag
{
    public enum TagFilterType
    {
        HasAllTags,
        HasAnyTags,
        DoesntHaveTags
    }

    [Serializable]
    public class TagFilter_CachedCondition : ICachedCondition
    {
        [SerializeField] private TagFilterType m_ReturnTrueIf;
        [SerializeField]
        private List<Tag> m_Tags = new();

        private GameObject _cachedGameObject;
        private TagHolder _cachedTagHolder;

        public bool SetGameObject(GameObject gameObj)
        {
            _cachedGameObject = gameObj;
            _cachedTagHolder = gameObj != null ? gameObj.GetComponent<TagHolder>() : null;
            return _cachedTagHolder != null;
        }

        public GameObject GetGameObject() => _cachedGameObject;

        public bool IsFulfilled()
        {
            if (_cachedTagHolder == null) return false;

            return m_ReturnTrueIf switch
            {
                TagFilterType.HasAllTags => m_Tags.All(t => _cachedTagHolder.HasTag(t)),
                TagFilterType.HasAnyTags => m_Tags.Any(t => _cachedTagHolder.HasTag(t)),
                TagFilterType.DoesntHaveTags => !m_Tags.Any(t => _cachedTagHolder.HasTag(t)),
                _ => false
            };
        }

        public ICachedCondition Clone()
        {
            var clone = new TagFilter_CachedCondition
            {
                m_ReturnTrueIf = this.m_ReturnTrueIf,
                m_Tags = new List<Tag>(this.m_Tags)
            };
            clone.SetGameObject(_cachedGameObject);
            return clone;
        }
    }
}
