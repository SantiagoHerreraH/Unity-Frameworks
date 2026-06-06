using SilverPillar.Core;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#nullable enable

namespace SilverPillar.Tag
{
    public class TagHolder : MonoBehaviour
    {
        private static Dictionary<Tag, List<GameObject>> m_Tags_To_GameObjects = new();

        [InfoBox("Only gameObjects that have been activated at least once in their lifetime will appear in the static tag list")]

        [SerializeField]
        private List<Tag> m_Tags = new List<Tag>();
        private HashSet<Tag> m_TagSet = new HashSet<Tag>();

        public List<Tag> Tags { get { return m_Tags; } }

        private void OnValidate()
        {
            m_Tags =  m_Tags.Distinct().ToList();
            m_TagSet = m_Tags.ToHashSet();
        }

        private void Awake()
        {
            m_Tags = m_Tags.Distinct().ToList();
            m_TagSet = m_Tags.ToHashSet();

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

        public GameObject? ChooseGameObjectWithTag(Tag tag, IChooseGameObject chooser)
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
            if (m_TagSet.Add(tag))
            {
                m_Tags.Add(tag);
                Register(tag);
            }
        }

        public void RemoveTag(Tag tag)
        {
            if (m_Tags.Remove(tag))
            {
                m_TagSet.Remove(tag);
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
