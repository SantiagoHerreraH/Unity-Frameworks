using SilverPillar.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Tag
{
    [Serializable]
    public class TagFilter_Condition : ICondition
    {
        [Header("Follow Target if has Tags:")]
        [SerializeField]
        private TagFilterType m_ReturnTrueIf;
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
            }

            return false;
        }
    }
}
