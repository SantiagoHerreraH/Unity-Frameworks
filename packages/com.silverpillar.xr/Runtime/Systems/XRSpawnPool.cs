using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.ARFoundation;

namespace SilverPillar.Core
{
    public class XRSpawnPool : MonoBehaviour
    {
        public enum AlignmentSettings
        {
            AlignOnX,
            AlignOnY,
            AlignOnZ
        }

        public enum RaycastPositioning
        {
            PlaceInHitPosition,
            PositionInHitObjectPosition
        }

        public enum RaycastRotation
        {
            DontModifyRotation,
            AlignRotationToHitNormal,
            CopyRotationFromHitObject
        }

        [Serializable]
        public struct RaycastSpawningSettings
        {
            public bool ParentToHit;

            [Tooltip("If enabled, the spawned object will be attached to an AR Anchor.")]
            public bool ParentToARAnchor;

            public RaycastPositioning RaycastPositioning;

            public RaycastRotation RaycastRotation;

            [ShowIf(nameof(RaycastRotation), RaycastRotation.AlignRotationToHitNormal)]
            public AlignmentSettings RotationAlignmentSettings;
        }

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

        [Title("Spawn Data")]
        [SerializeField]
        private GameObject m_SpawnPrefab;

        [SerializeField, Min(1)]
        private int m_MaxSpawnCount = 10;

        [Title("Side Cases")]
        [SerializeField]
        private WhatToDoIfReachedMaxSpawnCount m_WhatToDoIfReachedMaxSpawnCount =
            WhatToDoIfReachedMaxSpawnCount.Nothing;

        [SerializeField, ShowIf(nameof(m_WhatToDoIfReachedMaxSpawnCount),
            WhatToDoIfReachedMaxSpawnCount.ReuseExisting)]
        private ReusingProtocol m_DeactivateBeforeActivatingAgain =
            ReusingProtocol.DeactivateAndActivate;

        [Title("Default Spawn Point")]
        [SerializeField, Tooltip("If this is null, it will be self")]
        private GameObject m_DefaultSpawnPoint;

        [SerializeField]
        private bool m_ParentToDefaultSpawnPoint;

        [Title("AR")]
        [SerializeField]
        private ARAnchorManager m_ARAnchorManager;

        [Title("Default Raycast Settings")]
        [SerializeField]
        private RaycastSpawningSettings m_DefaultRaycastSpawningSettings;

        [Title("Events")]
        [SerializeField]
        private UnityEvent<GameObject> m_OnSpawn;

        [SerializeField]
        private UnityEvent<GameObject> m_OnReuse;

        private readonly List<GameObject> m_Instances = new();

        private int m_NextReuseIndex = 0;

        private bool m_Initialized = false;

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (m_Initialized || m_SpawnPrefab == null)
                return;

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

            GameObject? obj = m_Instances.Find(x => !x.activeSelf);

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

                        m_NextReuseIndex =
                            (m_NextReuseIndex + 1) % m_Instances.Count;

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
                        }

                        m_OnSpawn?.Invoke(obj);

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

                m_OnReuse?.Invoke(obj);
            }

            return obj;
        }

        public GameObject? Spawn(
            Vector3 position,
            Quaternion rotation,
            bool parentToARAnchor)
        {
            GameObject? obj = Spawn(position, rotation);

            if (obj == null)
                return null;

            if (parentToARAnchor)
            {
                _ = AttachToARAnchor(obj);
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

        public void Spawn(RaycastHit hit, RaycastSpawningSettings raycastSpawningSettings)
        {
            Vector3 position;
            Quaternion rotation = Quaternion.identity;

            Transform? hitTransform =
                hit.collider != null ? hit.collider.transform : null;

            switch (raycastSpawningSettings.RaycastPositioning)
            {
                case RaycastPositioning.PlaceInHitPosition:
                    position = hit.point;
                    break;

                case RaycastPositioning.PositionInHitObjectPosition:
                    position = hitTransform != null
                        ? hitTransform.position
                        : hit.point;
                    break;

                default:
                    position = hit.point;
                    break;
            }

            switch (raycastSpawningSettings.RaycastRotation)
            {
                case RaycastRotation.DontModifyRotation:
                    rotation = Quaternion.identity;
                    break;

                case RaycastRotation.CopyRotationFromHitObject:
                    rotation = hitTransform != null
                        ? hitTransform.rotation
                        : Quaternion.identity;
                    break;

                case RaycastRotation.AlignRotationToHitNormal:
                    rotation = GetRotationAlignedToNormal(
                        hit.normal,
                        raycastSpawningSettings.RotationAlignmentSettings
                    );
                    break;
            }

            GameObject? obj = Spawn(
                position,
                rotation,
                raycastSpawningSettings.ParentToARAnchor
            );

            if (obj == null)
                return;

            if (raycastSpawningSettings.ParentToHit && hitTransform != null)
            {
                obj.transform.SetParent(hitTransform, true);
            }
        }

        private async Task AttachToARAnchor(GameObject obj)
        {
            if (obj == null)
                return;

            if (m_ARAnchorManager == null)
            {
                Debug.LogWarning(
                    $"{nameof(XRSpawnPool)}: No ARAnchorManager assigned."
                );

                return;
            }

            Pose pose = new Pose(
                obj.transform.position,
                obj.transform.rotation
            );

            var result = await m_ARAnchorManager.TryAddAnchorAsync(pose);

            if (!result.status.IsSuccess())
            {
                Debug.LogWarning(
                    $"{nameof(XRSpawnPool)}: Failed to create AR Anchor."
                );

                return;
            }

            ARAnchor anchor = result.value;

            obj.transform.SetParent(anchor.transform, true);
        }

        private Quaternion GetRotationAlignedToNormal(
            Vector3 normal,
            AlignmentSettings alignmentSettings)
        {
            if (normal.sqrMagnitude <= Mathf.Epsilon)
                return Quaternion.identity;

            normal.Normalize();

            return alignmentSettings switch
            {
                AlignmentSettings.AlignOnX =>
                    Quaternion.FromToRotation(Vector3.right, normal),

                AlignmentSettings.AlignOnY =>
                    Quaternion.FromToRotation(Vector3.up, normal),

                AlignmentSettings.AlignOnZ =>
                    Quaternion.FromToRotation(Vector3.forward, normal),

                _ => Quaternion.identity
            };
        }

        public void Spawn(RaycastHit hit)
        {
            Spawn(hit, m_DefaultRaycastSpawningSettings);
        }

        private GameObject CreateNewInstance(bool activate)
        {
            GameObject obj;

            if (m_ParentToDefaultSpawnPoint)
            {
                if (m_DefaultSpawnPoint == null)
                {
                    m_DefaultSpawnPoint = gameObject;
                }

                obj = Instantiate(
                    m_SpawnPrefab,
                    m_DefaultSpawnPoint.transform
                );
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