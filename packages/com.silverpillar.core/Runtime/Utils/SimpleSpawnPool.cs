using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

namespace SilverPillar.Core
{
    public class SimpleSpawnPool : MonoBehaviour
    {
        public enum WhatToDoIfReachedMaxSpawnCount
        {
            Nothing,
            InstantiateMore,
            ReuseExisting
        }

        [Header("Spawn Data")]
        [SerializeField] 
        private GameObject m_SpawnPrefab;
        [SerializeField, Min(1)] 
        private int m_MaxSpawnCount = 10;

        [Header("Side Cases")]
        [SerializeField] 
        private WhatToDoIfReachedMaxSpawnCount m_WhatToDoIfReachedMaxSpawnCount = WhatToDoIfReachedMaxSpawnCount.Nothing;
        [SerializeField, Tooltip("If this is null, it will be self")] 
        private GameObject m_DefaultSpawnPoint;

        private List<GameObject> m_Instances = new();
        private int m_NextReuseIndex = 0;
        private bool m_Initialized = false;

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (m_Initialized || m_SpawnPrefab == null) return;

            for (int i = 0; i < m_MaxSpawnCount; i++)
            {
                CreateNewInstance(false);
            }

            m_Initialized = true;
        }

#nullable enable
        public GameObject? Spawn(Vector3 position, Quaternion rotation)
        {
            GameObject? obj = null;

            obj = m_Instances.Find(x => !x.activeSelf);

            if (obj == null)
            {
                switch (m_WhatToDoIfReachedMaxSpawnCount)
                {
                    case WhatToDoIfReachedMaxSpawnCount.InstantiateMore:

                        for (int i = 0; i < m_MaxSpawnCount; i++)
                        {
                            CreateNewInstance(false);
                        }

                        obj = m_Instances.Last();

                        break;

                    case WhatToDoIfReachedMaxSpawnCount.ReuseExisting:
                        obj = m_Instances[m_NextReuseIndex];
                        m_NextReuseIndex = (m_NextReuseIndex + 1) % m_Instances.Count;
                        break;

                    case WhatToDoIfReachedMaxSpawnCount.Nothing:
                    default:
                        return null;
                }
            }

            if (obj != null)
            {
                obj.transform.SetPositionAndRotation(position, rotation);
                obj.SetActive(true);
            }

            return obj;
        }

        public GameObject? Spawn(Transform where)
        {
            return Spawn(where.position, where.rotation);
        }

        public void SpawnInDefault()
        {
            if (m_DefaultSpawnPoint == null)
            {
                m_DefaultSpawnPoint = gameObject;
            }
            Spawn(m_DefaultSpawnPoint.transform);
        }

        private GameObject CreateNewInstance(bool activate)
        {
            GameObject obj = Instantiate(m_SpawnPrefab, transform);
            obj.SetActive(activate);
            m_Instances.Add(obj);
            return obj;
        }
    }
}
