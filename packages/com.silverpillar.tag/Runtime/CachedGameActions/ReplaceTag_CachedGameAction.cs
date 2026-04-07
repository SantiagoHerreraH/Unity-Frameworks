using SilverPillar.Core;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SilverPillar.Tag
{
    [Serializable]
    public class ReplaceTags_CachedGameAction : ICachedGameAction
    {
        public enum WhenToAddNewTags
        {
            Always,
            IfHasAllOldTags,
            IfHasAnyOldTag
        }

        [SerializeField] private WhenToAddNewTags m_Condition = WhenToAddNewTags.Always;
        [SerializeField] private List<Tag> m_OldTags = new();
        [SerializeField] private List<Tag> m_NewTags = new();

#nullable enable
        private TagHolder? m_TagHolder;

        public ReplaceTags_CachedGameAction() { }

        public ReplaceTags_CachedGameAction(ReplaceTags_CachedGameAction other)
        {
            m_Condition = other.m_Condition;
            m_OldTags = new List<Tag>(other.m_OldTags);
            m_NewTags = new List<Tag>(other.m_NewTags);
            m_TagHolder = other.m_TagHolder;
        }

        public ICachedGameAction Clone() => new ReplaceTags_CachedGameAction(this);

        public void Execute()
        {
            if (m_TagHolder == null) return;

            bool shouldAdd = m_Condition switch
            {
                WhenToAddNewTags.Always => true,
                WhenToAddNewTags.IfHasAllOldTags => m_OldTags.All(t => m_TagHolder.HasTag(t)),
                WhenToAddNewTags.IfHasAnyOldTag => m_OldTags.Any(t => m_TagHolder.HasTag(t)),
                _ => false
            };

            foreach (var oldTag in m_OldTags)
            {
                m_TagHolder.RemoveTag(oldTag);
            }

            if (shouldAdd)
            {
                foreach (var newTag in m_NewTags)
                {
                    m_TagHolder.AddTag(newTag);
                }
            }
        }

        public GameObject? GetGameObject() => m_TagHolder?.gameObject;

        public bool SetGameObject(GameObject gameObj) => gameObj.TryGetComponent(out m_TagHolder);
    }
}
