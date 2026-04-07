using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SilverPillar.Tag
{
    public class TagCategoryDefiner : MonoBehaviour
    {
        [Serializable]
        public class TagCategory
        {
            [SerializeField]
            private TagFilterType m_ReturnTrueIf;

            [SerializeField]
            private List<Tag> m_Tags = new List<Tag>();

            public bool IsPartOfTagCategory(List<Tag> otherTags)
            {
                switch (m_ReturnTrueIf)
                {
                    case TagFilterType.HasAllTags:
                        return m_Tags.All(t => otherTags.Contains(t));

                    case TagFilterType.HasAnyTags:
                        return m_Tags.Any(t => otherTags.Contains(t));

                    case TagFilterType.DoesntHaveTags:
                        return !m_Tags.Any(t => otherTags.Contains(t));

                    default:
                        return false;
                }
            }

        }

        [Serializable]
        public class TagCategoryDefinition
        {
            [SerializeField]
            public Tag CategoryTag;
            [SerializeField]
            public TagCategory IsDefinedAs = new();
        }

        [SerializeField]
        private List<TagCategoryDefinition> m_TagDefinitions = new();
        private Dictionary<Tag, TagCategory> m_Tags_To_TagGroup = new();

        private bool m_Initialized = false;

        private void Awake()
        {
            CheckInitialization();
        }

        private void CheckInitialization()
        {
            if (!m_Initialized)
            {
                Initialize();
                m_Initialized = true;
            }
        }

        [Button("Correct If Duplicated Tag Definitions", ButtonSizes.Medium)]
        private void Initialize()
        {
             m_TagDefinitions = m_TagDefinitions
                .Where(d => d.CategoryTag != null) // filter null
                .GroupBy(d => d.CategoryTag)       // group by category tag
                .Select(g => g.First())            // select first (Distinct)
                .ToList();

            m_Tags_To_TagGroup = m_TagDefinitions
                .ToDictionary(
                    d => d.CategoryTag,
                    d => d.IsDefinedAs
                );
        }

        public bool TagHolderIsInCategory(TagHolder tagHolder, Tag categoryTag)
        {
            CheckInitialization();

            if (m_Tags_To_TagGroup.ContainsKey(categoryTag))
            {
                return m_Tags_To_TagGroup[categoryTag].IsPartOfTagCategory(tagHolder.Tags);
            }

            return false;
        }
    }
}
