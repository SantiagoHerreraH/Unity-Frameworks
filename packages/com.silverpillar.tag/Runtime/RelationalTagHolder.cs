using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SilverPillar.Tag
{
    public class RelationalTagHolder : SerializedMonoBehaviour
    {
        [Header("Tags to Instances")]
        [OdinSerialize, ShowInInspector]
        private Dictionary<Tag, HashSet<GameObject>> m_Tag_To_GameObjectInstance = new();
        private Dictionary<GameObject, HashSet<Tag>> m_GameObjectInstance_To_Tags = new();
        private bool m_Initialized = false;

        private void Awake()
        {
            Initialize();
        }

        public bool HasRelation(GameObject gameObj, Tag tag)
        {
            if (!m_Tag_To_GameObjectInstance.ContainsKey(tag))
            {
                return false;
            }

            return m_Tag_To_GameObjectInstance[tag].Contains(gameObject);
        }

        public List<Tag> GetRelationalTags(GameObject gameObj)
        {
            Initialize();
            if (!m_GameObjectInstance_To_Tags.ContainsKey(gameObj))
            {
                return new();
            }

            return m_GameObjectInstance_To_Tags[gameObj].ToList();
        }

        public List<GameObject> GetGameObjectsWithRelationalTag(Tag tag)
        {
            if (!m_Tag_To_GameObjectInstance.ContainsKey(tag))
            {
                return new();
            }

            return m_Tag_To_GameObjectInstance[tag].ToList();
        }

        public void AddRelationalTag(GameObject gameObj, Tag tag)
        {
            Initialize();

            if (!m_Tag_To_GameObjectInstance.ContainsKey(tag))
            {
                m_Tag_To_GameObjectInstance.Add(tag, new());
            }
            if (!m_GameObjectInstance_To_Tags.ContainsKey(gameObj))
            {
                m_GameObjectInstance_To_Tags.Add(gameObj, new HashSet<Tag>());
            }

            m_Tag_To_GameObjectInstance[tag].Add(gameObj);
            m_GameObjectInstance_To_Tags[gameObj].Add(tag);
        }

        public void RemoveRelationalTag(GameObject gameObj, Tag tag)
        {
            Initialize();

            if (!m_Tag_To_GameObjectInstance.ContainsKey(tag) ||
                !m_GameObjectInstance_To_Tags.ContainsKey(gameObj))
            {
                return;
            }

            m_Tag_To_GameObjectInstance[tag].Remove(gameObj);
            m_GameObjectInstance_To_Tags[gameObj].Remove(tag);
        }

        private void Initialize()
        {
            if (!m_Initialized)
            {
                foreach (var item in m_Tag_To_GameObjectInstance)
                {
                    var gameObjects = item.Value;
                    foreach (var gameObj in gameObjects)
                    {
                        if (!m_GameObjectInstance_To_Tags.ContainsKey(gameObj))
                        {
                            m_GameObjectInstance_To_Tags.Add(gameObj, new());
                        }

                        m_GameObjectInstance_To_Tags[gameObj].Add(item.Key);
                    }
                }
                m_Initialized = true;
            }
        }
    }
}
