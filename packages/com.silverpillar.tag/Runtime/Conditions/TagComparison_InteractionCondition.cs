using SilverPillar.Core;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace SilverPillar.Tag
{
    [Serializable]
    public class TagComparison_InteractionCondition : IInteractionCondition
    {
        [SerializeField]
        private TagFilterType m_TagFilterTypeForOther;

        [SerializeField]
        private SelfType m_WhichTagHolderToTakeAsReferenceForComparison;

        [SerializeField, ShowIf(nameof(m_WhichTagHolderToTakeAsReferenceForComparison), SelfType.CustomGameObject)]
        private TagHolder m_TagHolder;

        [SerializeField]
        private bool m_WhatToReturnIfTargetDoesntHaveTagHolder;

        private GameObject m_Self;

        public IInteractionCondition Clone()
        {
            return new TagComparison_InteractionCondition
            {
                m_TagFilterTypeForOther = m_TagFilterTypeForOther,
                m_WhichTagHolderToTakeAsReferenceForComparison = m_WhichTagHolderToTakeAsReferenceForComparison,
                m_TagHolder = m_TagHolder,
                m_WhatToReturnIfTargetDoesntHaveTagHolder = m_WhatToReturnIfTargetDoesntHaveTagHolder,
                m_Self = m_Self
            };
        }

        public GameObject GetGameObject()
        {
            return m_Self;
        }

        public bool SetGameObject(GameObject self)
        {
            m_Self = self;

            if (m_WhichTagHolderToTakeAsReferenceForComparison == SelfType.CustomGameObject)
                return m_TagHolder != null;

            if (m_Self == null)
                return false;

            return m_Self.TryGetComponent(out m_TagHolder);
        }

        public bool IsFulfilled(GameObject target)
        {
            if (m_TagHolder == null)
                return false;

            if (target == null || !target.TryGetComponent(out TagHolder targetTagHolder))
                return m_WhatToReturnIfTargetDoesntHaveTagHolder;

            var tags = m_TagHolder.Tags;

            if (tags == null)
                return false;

            switch (m_TagFilterTypeForOther)
            {
                case TagFilterType.HasAllTags:
                    foreach (var tag in tags)
                    {
                        if (!targetTagHolder.HasTag(tag))
                            return false;
                    }

                    return true;

                case TagFilterType.HasAnyTags:
                    foreach (var tag in tags)
                    {
                        if (targetTagHolder.HasTag(tag))
                            return true;
                    }

                    return false;

                case TagFilterType.DoesntHaveTags:
                    foreach (var tag in tags)
                    {
                        if (targetTagHolder.HasTag(tag))
                            return false;
                    }

                    return true;

                default:
                    return false;
            }
        }
    }
}