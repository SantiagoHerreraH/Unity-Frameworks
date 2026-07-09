using Sirenix.OdinInspector;
using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace SilverPillar.Core
{
    [Serializable]
    public class CopySavedTransform_CachedGameAction : ICachedGameAction
    {
        public enum WhenToSaveData
        {
            OnlyOnceOnExecute,
            EveryTimeOnExecute,
            OnceOnSetGameObject,
            EveryTimeOnSetGameObject
        }

        [Header("Copy From Settings")]
        [SerializeField]
        private WhenToSaveData m_WhenToSaveData;

        [SerializeField]
        private SelfType m_CopyFrom;

        [SerializeField, ShowIf(nameof(m_CopyFrom), SelfType.CustomGameObject)]
        private Transform m_ReferenceTransform;

        [SerializeField]
        private Space m_WhatSpaceToCopy;

        [Header("Copy To")]
        [SerializeField]
        private SelfType m_CopyTo;

        [SerializeField, ShowIf(nameof(m_CopyTo), SelfType.CustomGameObject)]
        private Transform m_Transform;

        [Header("Position")]
        [SerializeField]
        private bool m_CopyPositionX;

        private float m_SavedPositionX;

        [SerializeField]
        private bool m_CopyPositionY;

        private float m_SavedPositionY;

        [SerializeField]
        private bool m_CopyPositionZ;

        private float m_SavedPositionZ;

        [Header("Rotation")]
        [SerializeField]
        private bool m_CopyRotationX;

        private float m_SavedRotationX;

        [SerializeField]
        private bool m_CopyRotationY;

        private float m_SavedRotationY;

        [SerializeField, FormerlySerializedAs("m_SetRotationZ")]
        private bool m_CopyRotationZ;

        private float m_SavedRotationZ;

        [Header("Scale")]
        [SerializeField]
        private bool m_CopyScaleX;

        private float m_SavedScaleX;

        [SerializeField]
        private bool m_CopyScaleY;

        private float m_SavedScaleY;

        [SerializeField]
        private bool m_CopyScaleZ;

        private float m_SavedScaleZ;

        private GameObject m_GameObject;
        private bool m_HasSavedTransformData;

        public ICachedGameAction Clone()
        {
            return new CopySavedTransform_CachedGameAction
            {
                m_WhenToSaveData = m_WhenToSaveData,

                m_CopyFrom = m_CopyFrom,
                m_ReferenceTransform = m_ReferenceTransform,
                m_WhatSpaceToCopy = m_WhatSpaceToCopy,

                m_CopyTo = m_CopyTo,
                m_Transform = m_Transform,

                m_CopyPositionX = m_CopyPositionX,
                m_SavedPositionX = m_SavedPositionX,
                m_CopyPositionY = m_CopyPositionY,
                m_SavedPositionY = m_SavedPositionY,
                m_CopyPositionZ = m_CopyPositionZ,
                m_SavedPositionZ = m_SavedPositionZ,

                m_CopyRotationX = m_CopyRotationX,
                m_SavedRotationX = m_SavedRotationX,
                m_CopyRotationY = m_CopyRotationY,
                m_SavedRotationY = m_SavedRotationY,
                m_CopyRotationZ = m_CopyRotationZ,
                m_SavedRotationZ = m_SavedRotationZ,

                m_CopyScaleX = m_CopyScaleX,
                m_SavedScaleX = m_SavedScaleX,
                m_CopyScaleY = m_CopyScaleY,
                m_SavedScaleY = m_SavedScaleY,
                m_CopyScaleZ = m_CopyScaleZ,
                m_SavedScaleZ = m_SavedScaleZ,

                m_GameObject = m_GameObject,
                m_HasSavedTransformData = m_HasSavedTransformData
            };
        }

        public void Execute()
        {
            TrySaveDataOnExecute();

            if (!m_HasSavedTransformData)
                return;

            Transform target = GetTargetTransform();

            if (target == null)
                return;

            SetPositionIfNeeded(target);
            SetRotationIfNeeded(target);
            SetScaleIfNeeded(target);
        }

        public GameObject GetGameObject()
        {
            return m_GameObject;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            m_GameObject = gameObj;

            if (m_GameObject == null)
                return false;

            TrySaveDataOnSetGameObject();

            return true;
        }

        private void TrySaveDataOnExecute()
        {
            switch (m_WhenToSaveData)
            {
                case WhenToSaveData.OnlyOnceOnExecute:
                    if (!m_HasSavedTransformData)
                        SaveTransformData();
                    break;

                case WhenToSaveData.EveryTimeOnExecute:
                    SaveTransformData();
                    break;
            }
        }

        private void TrySaveDataOnSetGameObject()
        {
            switch (m_WhenToSaveData)
            {
                case WhenToSaveData.OnceOnSetGameObject:
                    if (!m_HasSavedTransformData)
                        SaveTransformData();
                    break;

                case WhenToSaveData.EveryTimeOnSetGameObject:
                    SaveTransformData();
                    break;
            }
        }

        private void SaveTransformData()
        {
            Transform source = GetSourceTransform();

            if (source == null)
                return;

            SavePosition(source);
            SaveRotation(source);
            SaveScale(source);

            m_HasSavedTransformData = true;
        }

        private void SavePosition(Transform source)
        {
            if (!m_CopyPositionX && !m_CopyPositionY && !m_CopyPositionZ)
                return;

            Vector3 position = m_WhatSpaceToCopy == Space.World
                ? source.position
                : source.localPosition;

            if (m_CopyPositionX)
                m_SavedPositionX = position.x;

            if (m_CopyPositionY)
                m_SavedPositionY = position.y;

            if (m_CopyPositionZ)
                m_SavedPositionZ = position.z;
        }

        private void SaveRotation(Transform source)
        {
            if (!m_CopyRotationX && !m_CopyRotationY && !m_CopyRotationZ)
                return;

            Vector3 eulerRotation = m_WhatSpaceToCopy == Space.World
                ? source.rotation.eulerAngles
                : source.localRotation.eulerAngles;

            if (m_CopyRotationX)
                m_SavedRotationX = eulerRotation.x;

            if (m_CopyRotationY)
                m_SavedRotationY = eulerRotation.y;

            if (m_CopyRotationZ)
                m_SavedRotationZ = eulerRotation.z;
        }

        private void SaveScale(Transform source)
        {
            if (!m_CopyScaleX && !m_CopyScaleY && !m_CopyScaleZ)
                return;

            Vector3 scale = source.localScale;

            if (m_CopyScaleX)
                m_SavedScaleX = scale.x;

            if (m_CopyScaleY)
                m_SavedScaleY = scale.y;

            if (m_CopyScaleZ)
                m_SavedScaleZ = scale.z;
        }

        private Transform GetSourceTransform()
        {
            switch (m_CopyFrom)
            {
                case SelfType.ThisGameObject:
                    return m_GameObject != null ? m_GameObject.transform : null;

                case SelfType.CustomGameObject:
                    return m_ReferenceTransform;

                default:
                    return null;
            }
        }

        private Transform GetTargetTransform()
        {
            switch (m_CopyTo)
            {
                case SelfType.ThisGameObject:
                    return m_GameObject != null ? m_GameObject.transform : null;

                case SelfType.CustomGameObject:
                    return m_Transform;

                default:
                    return null;
            }
        }

        private void SetPositionIfNeeded(Transform target)
        {
            if (!m_CopyPositionX && !m_CopyPositionY && !m_CopyPositionZ)
                return;

            Vector3 position = m_WhatSpaceToCopy == Space.World
                ? target.position
                : target.localPosition;

            if (m_CopyPositionX)
                position.x = m_SavedPositionX;

            if (m_CopyPositionY)
                position.y = m_SavedPositionY;

            if (m_CopyPositionZ)
                position.z = m_SavedPositionZ;

            if (m_WhatSpaceToCopy == Space.World)
                target.position = position;
            else
                target.localPosition = position;
        }

        private void SetRotationIfNeeded(Transform target)
        {
            if (!m_CopyRotationX && !m_CopyRotationY && !m_CopyRotationZ)
                return;

            Vector3 eulerRotation = m_WhatSpaceToCopy == Space.World
                ? target.rotation.eulerAngles
                : target.localRotation.eulerAngles;

            if (m_CopyRotationX)
                eulerRotation.x = m_SavedRotationX;

            if (m_CopyRotationY)
                eulerRotation.y = m_SavedRotationY;

            if (m_CopyRotationZ)
                eulerRotation.z = m_SavedRotationZ;

            Quaternion rotation = Quaternion.Euler(eulerRotation);

            if (m_WhatSpaceToCopy == Space.World)
                target.rotation = rotation;
            else
                target.localRotation = rotation;
        }

        private void SetScaleIfNeeded(Transform target)
        {
            if (!m_CopyScaleX && !m_CopyScaleY && !m_CopyScaleZ)
                return;

            Vector3 scale = target.localScale;

            if (m_CopyScaleX)
                scale.x = m_SavedScaleX;

            if (m_CopyScaleY)
                scale.y = m_SavedScaleY;

            if (m_CopyScaleZ)
                scale.z = m_SavedScaleZ;

            target.localScale = scale;
        }
    }
}