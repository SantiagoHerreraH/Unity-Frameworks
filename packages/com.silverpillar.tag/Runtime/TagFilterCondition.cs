using SilverPillar.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Tag
{
    [Serializable]
    public class TagFilterCondition : ICondition
    {
        public enum ReturnTrueIf
        {
            HasAllTags,
            HasAnyTags,
            DoesntHaveTags
        }

        [Header("Follow Target if has Tags:")]
        [SerializeField]
        private ReturnTrueIf m_ReturnTrueIf;
        [SerializeField, Tooltip("If empty, all current targets are valid")]
        private List<Tag> m_Tags = new();

        public bool IsFulfilled(GameObject gameObj)
        {
            if (gameObj != null)
            {
                TagHolder tagHolder = gameObj.GetComponent<TagHolder>();

                if (tagHolder != null)
                {
                    switch (m_ReturnTrueIf)
                    {
                        case ReturnTrueIf.HasAllTags:

                            foreach (var tag in m_Tags)
                            {
                                if (!tagHolder.HasTag(tag))
                                {
                                    return false;
                                }
                            }

                            return true;

                        case ReturnTrueIf.HasAnyTags:

                            foreach (var tag in m_Tags)
                            {
                                if (tagHolder.HasTag(tag))
                                {
                                    return true;
                                }
                            }

                            return false;

                        case ReturnTrueIf.DoesntHaveTags:

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
            }

            return false;
        }
    }
}
