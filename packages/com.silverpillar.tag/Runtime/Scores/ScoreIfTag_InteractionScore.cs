using UnityEngine;
using SilverPillar.Core;
using System;
using Sirenix.Serialization;
using Sirenix.OdinInspector;
using System.Collections.Generic;

namespace SilverPillar.Tag
{
    [Serializable]
    public class ScoreIfTag_InteractionScore : ICachedInteractionScore
    {
        [Title("Tag Filter")]
        [SerializeField] 
        private TagFilterType m_ReturnTrueIf;
        [SerializeField]
        private List<Tag> m_Tags = new();
        [SerializeField]
        private TargetType m_TagFilterOnWho;


        [Title("Values")]
        [SerializeField]
        private float m_ValueIfTrue;
        [SerializeField]
        private float m_ValueIfFalse;

        private GameObject m_Self;
        private TagHolder m_TagHolder;

        public ScoreIfTag_InteractionScore() { }
        public ScoreIfTag_InteractionScore(ScoreIfTag_InteractionScore other)
        {
            m_ReturnTrueIf = other.m_ReturnTrueIf;
            m_Tags = new(other.m_Tags);
            m_TagFilterOnWho = other.m_TagFilterOnWho;

            m_ValueIfTrue = other.m_ValueIfTrue;
            m_ValueIfFalse = other.m_ValueIfFalse;
            m_Self = other.m_Self;
            m_TagHolder = other.m_TagHolder;
        }

        public float CalculateScore(GameObject target)
        {
            if ((m_TagFilterOnWho == TargetType.Self  && m_TagHolder != null) || 
                target.TryGetComponent(out m_TagHolder))
            {
                if (IsFulfilled(m_TagHolder))
                {
                    return m_ValueIfTrue;
                }
            }

            return m_ValueIfFalse;
        }

        public ICachedInteractionScore Clone()
        {
            return new ScoreIfTag_InteractionScore(this);
        }

        public GameObject GetGameObject()
        {
            return m_Self;
        }

        public bool SetGameObject(GameObject self)
        {
            if (self == null)
            {
                return false;
            }
            m_Self = self;
            if (m_TagFilterOnWho == TargetType.Self)
            {
                return self.TryGetComponent(out m_TagHolder);
            }

            return true;
        }

        private bool IsFulfilled(TagHolder tagHolder)
        {

            if (tagHolder != null)
            {
                switch (m_ReturnTrueIf)
                {
                    case TagFilterType.HasAllTags:

                        foreach (var tag in m_Tags)
                        {
                            if (!tagHolder.HasTag(tag))
                            {
                                return false;
                            }
                        }

                        return true;

                    case TagFilterType.HasAnyTags:

                        foreach (var tag in m_Tags)
                        {
                            if (tagHolder.HasTag(tag))
                            {
                                return true;
                            }
                        }

                        return false;

                    case TagFilterType.DoesntHaveTags:

                        foreach (var tag in m_Tags)
                        {
                            if (tagHolder.HasTag(tag))
                            {
                                return false;
                            }
                        }

                        return true;
                    default:
                        break;
                }
            }

            return false;
        }
    }
}
