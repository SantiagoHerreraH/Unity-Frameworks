using Sirenix.OdinInspector;
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

        public enum ReusingProtocol
        {
            DeactivateAndActivate,
            OnlyActivate,
            OnlyChangePosition
        }

        [Header("Spawn Data")]
        [SerializeField] 
        private GameObject m_SpawnPrefab;
        [SerializeField, Min(1)] 
        private int m_MaxSpawnCount = 10;

        [Header("Side Cases")]
        [SerializeField] 
        private WhatToDoIfReachedMaxSpawnCount m_WhatToDoIfReachedMaxSpawnCount = WhatToDoIfReachedMaxSpawnCount.Nothing;
        [SerializeField, ShowIf(nameof(m_WhatToDoIfReachedMaxSpawnCount), WhatToDoIfReachedMaxSpawnCount.ReuseExisting)]
        private ReusingProtocol m_DeactivateBeforeActivatingAgain = ReusingProtocol.DeactivateAndActivate;
        [SerializeField, Tooltip("If this is null, it will be self")] 
        private GameObject m_DefaultSpawnPoint;
        [SerializeField]
        private bool m_ParentToDefaultSpawnPoint;

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
            Initialize();

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


                        switch (m_DeactivateBeforeActivatingAgain)
                        {
                            case ReusingProtocol.DeactivateAndActivate:
                               
                                obj.SetActive(false);
                                obj.transform.SetPositionAndRotation(position, rotation);
                                obj.SetActive(true);

                                break;
                            case ReusingProtocol.OnlyActivate:

                                obj.transform.SetPositionAndRotation(position, rotation);
                                obj.SetActive(true);

                                break;
                            case ReusingProtocol.OnlyChangePosition:


                                obj.transform.SetPositionAndRotation(position, rotation);

                                break;
                            default:
                                break;
                        }

                        return obj;

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
            GameObject obj = null;

            if (m_ParentToDefaultSpawnPoint)
            {
                if (m_DefaultSpawnPoint == null)
                {
                    m_DefaultSpawnPoint = gameObject;
                }

                obj = Instantiate(m_SpawnPrefab, m_DefaultSpawnPoint.transform);
            }
            else
            {
                obj = Instantiate(m_SpawnPrefab);
            }

            obj.SetActive(activate);
            m_Instances.Add(obj);
            return obj;
        }
    }
}
