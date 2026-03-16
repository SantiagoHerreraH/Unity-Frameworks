using Sirenix.Serialization;
using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Tag
{
    public class TagHolder : MonoBehaviour
    {
        [OdinSerialize]
        private HashSet<Tag> m_Tags = new HashSet<Tag>();

        public bool HasTag(Tag tag)
        {
            return m_Tags.Contains(tag);
        }

        public void AddTag(Tag tag)
        {
            m_Tags.Add(tag);
        }

        public void RemoveTag(Tag tag)
        {
            m_Tags.Remove(tag);
        }
    }
}
