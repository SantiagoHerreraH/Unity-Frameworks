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
        private bool m_SetPosition;

        [SerializeField, ShowIf(nameof(m_SetPosition))]
        private Vector3 m_Position;

        [Header("Rotation")]
        [SerializeField]
        private bool m_SetRotation;

        [SerializeField, ShowIf(nameof(m_SetRotation))]
        private Vector3 m_Rotation;

        [Header("Scale")]
        [SerializeField]
        private bool m_SetScale;

        [SerializeField, ShowIf(nameof(m_SetScale))]
        private Vector3 m_Scale = Vector3.one;

        private GameObject m_GameObject;

        public ICachedGameAction Clone()
        {
            return new SetTransform_CachedGameAction
            {
                m_SetTransformOnWho = m_SetTransformOnWho,
                m_Transform = m_Transform,
                m_Space = m_Space,
                m_SetPosition = m_SetPosition,
                m_Position = m_Position,
                m_SetRotation = m_SetRotation,
                m_Rotation = m_Rotation,
                m_SetScale = m_SetScale,
                m_Scale = m_Scale,
                m_GameObject = m_GameObject
            };
        }

        public void Execute()
        {
            Transform target = GetTargetTransform();

            if (target == null)
                return;

            if (m_SetPosition)
            {
                if (m_Space == Space.World)
                    target.position = m_Position;
                else
                    target.localPosition = m_Position;
            }

            if (m_SetRotation)
            {
                Quaternion rotation = Quaternion.Euler(m_Rotation);

                if (m_Space == Space.World)
                    target.rotation = rotation;
                else
                    target.localRotation = rotation;
            }

            if (m_SetScale)
            {
                target.localScale = m_Scale;
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
    }
}