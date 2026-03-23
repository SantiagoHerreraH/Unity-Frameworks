using SilverPillar.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SilverPillar.Tag
{
    public enum ReturnTrueIf
    {
        HasAllTags,
        HasAnyTags,
        DoesntHaveTags
    }

    [Serializable]
    public class CachedTagFilterCondition : ICachedCondition
    {
        [Header("Follow Target if has Tags:")]
        [SerializeField] private ReturnTrueIf m_ReturnTrueIf;
        [SerializeField, Tooltip("If empty, all current targets are valid")]
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
                ReturnTrueIf.HasAllTags => m_Tags.All(t => _cachedTagHolder.HasTag(t)),
                ReturnTrueIf.HasAnyTags => m_Tags.Any(t => _cachedTagHolder.HasTag(t)),
                ReturnTrueIf.DoesntHaveTags => !m_Tags.Any(t => _cachedTagHolder.HasTag(t)),
                _ => false
            };
        }

        public ICachedCondition Clone()
        {
            var clone = new CachedTagFilterCondition
            {
                m_ReturnTrueIf = this.m_ReturnTrueIf,
                m_Tags = new List<Tag>(this.m_Tags) // Copia de la lista
            };
            clone.SetGameObject(_cachedGameObject);
            return clone;
        }
    }
}
