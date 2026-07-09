using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class SetTransform_CachedGameAction : ICachedGameAction
    {
        [Header("Target")]
        [SerializeField]
        private SelfType m_SetTransformOnWho;

        [SerializeField, ShowIf(nameof(m_SetTransformOnWho), SelfType.CustomGameObject)]
        private Transform m_Transform;

        [Header("Transformation")]
        [SerializeField]
        private Space m_Space;

        [Header("Position")]
        [SerializeField]
        private bool m_SetPositionX;

        [SerializeField, ShowIf(nameof(m_SetPositionX))]
        private float m_PositionX;

        [SerializeField]
        private bool m_SetPositionY;

        [SerializeField, ShowIf(nameof(m_SetPositionY))]
        private float m_PositionY;

        [SerializeField]
        private bool m_SetPositionZ;

        [SerializeField, ShowIf(nameof(m_SetPositionZ))]
        private float m_PositionZ;

        [Header("Rotation")]
        [SerializeField]
        private bool m_SetRotationX;

        [SerializeField, ShowIf(nameof(m_SetRotationX))]
        private float m_RotationX;

        [SerializeField]
        private bool m_SetRotationY;

        [SerializeField, ShowIf(nameof(m_SetRotationY))]
        private float m_RotationY;

        [SerializeField]
        private bool m_SetRotationZ;

        [SerializeField, ShowIf(nameof(m_SetRotationZ))]
        private float m_RotationZ;

        [Header("Scale")]
        [SerializeField]
        private bool m_SetScaleX;

        [SerializeField, ShowIf(nameof(m_SetScaleX))]
        private float m_ScaleX;

        [SerializeField]
        private bool m_SetScaleY;

        [SerializeField, ShowIf(nameof(m_SetScaleY))]
        private float m_ScaleY;

        [SerializeField]
        private bool m_SetScaleZ;

        [SerializeField, ShowIf(nameof(m_SetScaleZ))]
        private float m_ScaleZ;

        private GameObject m_GameObject;

        public ICachedGameAction Clone()
        {
            return new SetTransform_CachedGameAction
            {
                m_SetTransformOnWho = m_SetTransformOnWho,
                m_Transform = m_Transform,
                m_Space = m_Space,

                m_SetPositionX = m_SetPositionX,
                m_PositionX = m_PositionX,
                m_SetPositionY = m_SetPositionY,
                m_PositionY = m_PositionY,
                m_SetPositionZ = m_SetPositionZ,
                m_PositionZ = m_PositionZ,

                m_SetRotationX = m_SetRotationX,
                m_RotationX = m_RotationX,
                m_SetRotationY = m_SetRotationY,
                m_RotationY = m_RotationY,
                m_SetRotationZ = m_SetRotationZ,
                m_RotationZ = m_RotationZ,

                m_SetScaleX = m_SetScaleX,
                m_ScaleX = m_ScaleX,
                m_SetScaleY = m_SetScaleY,
                m_ScaleY = m_ScaleY,
                m_SetScaleZ = m_SetScaleZ,
                m_ScaleZ = m_ScaleZ,

                m_GameObject = m_GameObject
            };
        }

        public void Execute()
        {
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
            return m_GameObject != null;
        }

        private Transform GetTargetTransform()
        {
            switch (m_SetTransformOnWho)
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
            if (!m_SetPositionX && !m_SetPositionY && !m_SetPositionZ)
                return;

            Vector3 position = m_Space == Space.World
                ? target.position
                : target.localPosition;

            if (m_SetPositionX)
                position.x = m_PositionX;

            if (m_SetPositionY)
                position.y = m_PositionY;

            if (m_SetPositionZ)
                position.z = m_PositionZ;

            if (m_Space == Space.World)
                target.position = position;
            else
                target.localPosition = position;
        }

        private void SetRotationIfNeeded(Transform target)
        {
            if (!m_SetRotationX && !m_SetRotationY && !m_SetRotationZ)
                return;

            Vector3 eulerRotation = m_Space == Space.World
                ? target.rotation.eulerAngles
                : target.localRotation.eulerAngles;

            if (m_SetRotationX)
                eulerRotation.x = m_RotationX;

            if (m_SetRotationY)
                eulerRotation.y = m_RotationY;

            if (m_SetRotationZ)
                eulerRotation.z = m_RotationZ;

            Quaternion rotation = Quaternion.Euler(eulerRotation);

            if (m_Space == Space.World)
                target.rotation = rotation;
            else
                target.localRotation = rotation;
        }

        private void SetScaleIfNeeded(Transform target)
        {
            if (!m_SetScaleX && !m_SetScaleY && !m_SetScaleZ)
                return;

            Vector3 scale = target.localScale;

            if (m_SetScaleX)
                scale.x = m_ScaleX;

            if (m_SetScaleY)
                scale.y = m_ScaleY;

            if (m_SetScaleZ)
                scale.z = m_ScaleZ;

            target.localScale = scale;
        }
    }
}