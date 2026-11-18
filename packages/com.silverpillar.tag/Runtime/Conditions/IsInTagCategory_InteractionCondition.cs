using SilverPillar.Core;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace SilverPillar.Tag
{
    [Serializable]
    public class IsInTagCategory_InteractionCondition : IInteractionCondition
    {
        [SerializeField]
        private SelfType m_WhichTagCatgeoryDefiner;
        [SerializeField, ShowIf(nameof(m_WhichTagCatgeoryDefiner), SelfType.CustomGameObject)]
        private TagCategoryDefiner m_TagCategoryDefiner;
        [SerializeField]
        private Tag m_CategoryTag;


        public IsInTagCategory_InteractionCondition() { }
        public IsInTagCategory_InteractionCondition(IsInTagCategory_InteractionCondition other)
        {
            m_CategoryTag = other.m_CategoryTag;
            m_TagCategoryDefiner = other.m_TagCategoryDefiner;
        }

        public IInteractionCondition Clone()
        {
            return new IsInTagCategory_InteractionCondition(this);
        }


        public bool IsFulfilled(GameObject target)
        {
            if (m_TagCategoryDefiner == null || m_CategoryTag == null)
            {
                return false;
            }

            TagHolder otherTagHolder = null;

            if (!target.TryGetComponent(out otherTagHolder))
            {
                return false;
            }

            return m_TagCategoryDefiner.TagHolderIsInCategory(otherTagHolder, m_CategoryTag);
        }

        public bool SetGameObject(GameObject self)
        {
            if (m_WhichTagCatgeoryDefiner == SelfType.ThisGameObject)
            {
                return self.TryGetComponent(out m_TagCategoryDefiner);
            }

            return m_TagCategoryDefiner != null;
        }

#nullable enable
        public GameObject? GetGameObject()
        {
            return m_TagCategoryDefiner != null ? m_TagCategoryDefiner.gameObject : null;
        }
    }
}
