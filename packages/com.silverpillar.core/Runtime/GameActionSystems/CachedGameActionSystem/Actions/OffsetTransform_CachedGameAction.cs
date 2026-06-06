using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class OffsetTransform_CachedGameAction : ICachedGameAction
    {
        [Header("Target")]
        [SerializeField]
        private SelfType m_OffsetTransformOnWho;

        [SerializeField, ShowIf(nameof(m_OffsetTransformOnWho), SelfType.CustomGameObject)]
        private Transform m_Transform;

        [Header("Transformation")]
        [SerializeField]
        private Space m_Space;

        [Header("Position")]
        [SerializeField]
        private bool m_OffsetPosition;

        [SerializeField, ShowIf(nameof(m_OffsetPosition))]
        private Vector3 m_PositionOffset;

        [Header("Rotation")]
        [SerializeField]
        private bool m_OffsetRotation;

        [SerializeField, ShowIf(nameof(m_OffsetRotation))]
        private Vector3 m_RotationOffset;

        [Header("Scale")]
        [SerializeField]
        private bool m_OffsetScale;

        [SerializeField, ShowIf(nameof(m_OffsetScale))]
        private Vector3 m_ScaleOffset;

        private GameObject m_GameObject;

        public ICachedGameAction Clone()
        {
            return new OffsetTransform_CachedGameAction
            {
                m_OffsetTransformOnWho = m_OffsetTransformOnWho,
                m_Transform = m_Transform,
                m_Space = m_Space,
                m_OffsetPosition = m_OffsetPosition,
                m_PositionOffset = m_PositionOffset,
                m_OffsetRotation = m_OffsetRotation,
                m_RotationOffset = m_RotationOffset,
                m_OffsetScale = m_OffsetScale,
                m_ScaleOffset = m_ScaleOffset,
                m_GameObject = m_GameObject
            };
        }

        public void Execute()
        {
            Transform target = GetTargetTransform();

            if (target == null)
                return;

            if (m_OffsetPosition)
            {
                if (m_Space == Space.World)
                    target.position += m_PositionOffset;
                else
                    target.localPosition += m_PositionOffset;
            }

            if (m_OffsetRotation)
            {
                Quaternion rotationOffset = Quaternion.Euler(m_RotationOffset);

                if (m_Space == Space.World)
                    target.rotation = rotationOffset * target.rotation;
                else
                    target.localRotation *= rotationOffset;
            }

            if (m_OffsetScale)
            {
                target.localScale += m_ScaleOffset;
            }
        }

        public GameObject GetGameObject()
        {
            return m_GameObject;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            m_GameObject = gameObj;
            return m_GameObject != null;
        }

        private Transform GetTargetTransform()
        {
            switch (m_OffsetTransformOnWho)
            {
                case SelfType.ThisGameObject:
                    return m_GameObject != null ? m_GameObject.transform : null;

                case SelfType.CustomGameObject:
                    return m_Transform;

                default:
                    return null;
            }
        }
    }
}