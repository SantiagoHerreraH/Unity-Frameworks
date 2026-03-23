using SilverPillar.Core;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

namespace SilverPillar.Tag
{
    public class TagHolder : SerializedMonoBehaviour
    {
        private static Dictionary<Tag, List<GameObject>> m_Tags_To_GameObjects = new();

        [InfoBox("Only gameObjects that have been activated at least once in their lifetime will appear in the static tag list")]

        [OdinSerialize, ShowInInspector]
        private HashSet<Tag> m_Tags = new HashSet<Tag>();

        private void Awake()
        {
            foreach (Tag tag in m_Tags)
            {
                Register(tag);
            }
        }

        public static List<GameObject>? GetAllGameObjectsWithTag(Tag tag)
        {
            if (m_Tags_To_GameObjects.ContainsKey(tag))
            {
                return m_Tags_To_GameObjects[tag];
            }

            return null;
        }

        public GameObject? ChooseGameObjectWithTag(Tag tag, IChoose chooser)
        {
            if (m_Tags_To_GameObjects.ContainsKey(tag))
            {
                var list = m_Tags_To_GameObjects[tag];

                return chooser.Choose(gameObject, list);
            }

            return null;
        }

        public bool HasTag(Tag tag)
        {
            return m_Tags.Contains(tag);
        }

        public void AddTag(Tag tag)
        {
            if (m_Tags.Add(tag))
            {
                Register(tag);
            }
        }

        public void RemoveTag(Tag tag)
        {
            if (m_Tags.Remove(tag))
            {
                Unregister(tag);
            }
        }

        private void Register(Tag tag)
        {
            if (!m_Tags_To_GameObjects.ContainsKey(tag))
            {
                m_Tags_To_GameObjects.Add(tag, new());
            }

            m_Tags_To_GameObjects[tag].Add(gameObject);
        }

        private void Unregister(Tag tag)
        {
            if (m_Tags_To_GameObjects.ContainsKey(tag))
            {
                m_Tags_To_GameObjects[tag].Remove(gameObject);
            }
        }
    }
}
