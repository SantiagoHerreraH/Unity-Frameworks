using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace SilverPillar.Core
{
    public class SimpleSpawnPool : MonoBehaviour
    {
        public enum AlignmentSettings
        {
            AlignOnX,
            AlignOnY,
            AlignOnZ
        }

        public enum HitPositioning
        {
            PlaceInHitPosition,
            PositionInHitObjectPosition
        }

        public enum HitRotation
        {
            DontModifyRotation,
            AlignRotationToHitNormal,
            CopyRotationFromHitObject
        }


        [Serializable]
        public struct RaycastSpawningSettings
        {
            public bool ParentToHit;
            public HitPositioning RaycastPositioning;
            public HitRotation RaycastRotation;
            [ShowIf(nameof(RaycastRotation), HitRotation.AlignRotationToHitNormal)]
            public AlignmentSettings RotationAlignmentSettings;
            
        }

        [Serializable]
        public struct CollisionSpawningSettings
        {
            public bool ParentToHit;

            public HitPositioning HitPositioning;

            public HitRotation HitRotation;

            [ShowIf(nameof(HitRotation), HitRotation.AlignRotationToHitNormal)]
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
        private WhatToDoIfReachedMaxSpawnCount m_WhatToDoIfReachedMaxSpawnCount = WhatToDoIfReachedMaxSpawnCount.Nothing;
        [SerializeField, ShowIf(nameof(m_WhatToDoIfReachedMaxSpawnCount), WhatToDoIfReachedMaxSpawnCount.ReuseExisting)]
        private ReusingProtocol m_DeactivateBeforeActivatingAgain = ReusingProtocol.DeactivateAndActivate;


        [Title("Default Spawn Point")]
        [SerializeField, Tooltip("If this is null, it will be self")] 
        private GameObject m_DefaultSpawnPoint;
        [SerializeField]
        private bool m_ParentToDefaultSpawnPoint;


        [Title("Default Raycast Settings")]
        [SerializeField]
        private RaycastSpawningSettings m_DefaultRaycastSpawningSettings;

        [Title("Default Collision Settings")]
        [SerializeField]
        private bool m_CallOnSelfCollisionEnter;
        [SerializeField]
        private bool m_CallOnSelfCollisionExit;
        [SerializeField]
        private CollisionSpawningSettings m_CollisionSpawningSettings;


        [Title("Default Trigger Settings")]
        [SerializeField]
        private bool m_CallOnSelfTriggerEnter;
        [SerializeField]
        private bool m_CallOnSelfTriggerExit;
        [SerializeField]
        private CollisionSpawningSettings m_TriggerSpawningSettings;

        [Title("Events")]
        [SerializeField]
        private UnityEvent<GameObject> m_OnSpawn;
        [SerializeField]
        private UnityEvent<GameObject> m_OnReuse;

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

            Transform? hitTransform = hit.collider != null ? hit.collider.transform : null;

            switch (raycastSpawningSettings.RaycastPositioning)
            {
                case HitPositioning.PlaceInHitPosition:
                    position = hit.point;
                    break;

                case HitPositioning.PositionInHitObjectPosition:
                    position = hitTransform != null ? hitTransform.position : hit.point;
                    break;

                default:
                    position = hit.point;
                    break;
            }

            switch (raycastSpawningSettings.RaycastRotation)
            {
                case HitRotation.DontModifyRotation:
                    rotation = Quaternion.identity;
                    break;

                case HitRotation.CopyRotationFromHitObject:
                    rotation = hitTransform != null ? hitTransform.rotation : Quaternion.identity;
                    break;

                case HitRotation.AlignRotationToHitNormal:
                    rotation = GetRotationAlignedToNormal(
                        hit.normal,
                        raycastSpawningSettings.RotationAlignmentSettings
                    );
                    break;
            }

            GameObject? obj = Spawn(position, rotation);

            if (obj == null)
                return;

            if (raycastSpawningSettings.ParentToHit && hitTransform != null)
                obj.transform.SetParent(hitTransform, true);
        }

        private Quaternion GetRotationAlignedToNormal(Vector3 normal, AlignmentSettings alignmentSettings)
        {
            if (normal.sqrMagnitude <= Mathf.Epsilon)
                return Quaternion.identity;

            normal.Normalize();

            return alignmentSettings switch
            {
                AlignmentSettings.AlignOnX => Quaternion.FromToRotation(Vector3.right, normal),
                AlignmentSettings.AlignOnY => Quaternion.FromToRotation(Vector3.up, normal),
                AlignmentSettings.AlignOnZ => Quaternion.FromToRotation(Vector3.forward, normal),
                _ => Quaternion.identity
            };
        }

        public void Spawn(RaycastHit hit)
        {
            Spawn(hit, m_DefaultRaycastSpawningSettings);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!m_CallOnSelfCollisionEnter)
                return;

            Spawn(collision);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!m_CallOnSelfTriggerEnter)
                return;

            Spawn(other);
        }

        private void OnCollisionExit(Collision collision)
        {
            if (!m_CallOnSelfCollisionExit)
                return;

            Spawn(collision);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!m_CallOnSelfTriggerExit)
                return;

            Spawn(other);
        }

        public void Spawn(Collider collider)
        {
            Spawn(collider, m_TriggerSpawningSettings);
        }
        public void Spawn(Collider collider, CollisionSpawningSettings settings)
        {
            Vector3 closestPoint = collider.ClosestPoint(transform.position);
            Vector3 normal =
                (transform.position - closestPoint).normalized;

            if (normal.sqrMagnitude <= Mathf.Epsilon)
            {
                normal = Vector3.up;
            }

            HandleCollisionSpawn(
                collider.transform,
                closestPoint,
                normal,
                settings
            );
        }

        public void Spawn(Collision collision)
        {
            Spawn(collision, m_CollisionSpawningSettings);
        }

        public void Spawn(Collision collision, CollisionSpawningSettings settings)
        {
            HandleCollisionSpawn(
               collision.transform,
               collision.contacts.Length > 0
                   ? collision.contacts[0].point
                   : collision.transform.position,
               collision.contacts.Length > 0
                   ? collision.contacts[0].normal
                   : Vector3.up,
               settings
           );
        }




        private void HandleCollisionSpawn(
            Transform hitTransform,
            Vector3 hitPoint,
            Vector3 hitNormal,
            CollisionSpawningSettings settings)
        {
            Vector3 position;
            Quaternion rotation;

            switch (settings.HitPositioning)
            {
                case HitPositioning.PlaceInHitPosition:

                    position = hitPoint;

                    break;

                case HitPositioning.PositionInHitObjectPosition:

                    position = hitTransform != null
                        ? hitTransform.position
                        : hitPoint;

                    break;

                default:

                    position = hitPoint;

                    break;
            }

            switch (settings.HitRotation)
            {
                case HitRotation.DontModifyRotation:

                    rotation = Quaternion.identity;

                    break;

                case HitRotation.CopyRotationFromHitObject:

                    rotation = hitTransform != null
                        ? hitTransform.rotation
                        : Quaternion.identity;

                    break;

                case HitRotation.AlignRotationToHitNormal:

                    rotation = GetRotationAlignedToNormal(
                        hitNormal,
                        settings.RotationAlignmentSettings
                    );

                    break;

                default:

                    rotation = Quaternion.identity;

                    break;
            }

            GameObject? obj = Spawn(
                position,
                rotation
            );

            if (obj == null)
                return;

            if (settings.ParentToHit && hitTransform != null)
            {
                obj.transform.SetParent(hitTransform, true);
            }
        }


        private GameObject CreateNewInstance(bool activate)
        {
            GameObject? obj = null;

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
