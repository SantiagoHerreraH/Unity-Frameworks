
using MoreMountains.TopDownEngine;
using SilverPillar.Core;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace SilverPillar.Integrations.MMTopDown
{
    [Serializable]
    public class SetMovement_CachedGameAction : ICachedGameAction
    {
        [SerializeField]
        private SelfType m_WhichController;

        [SerializeField, ShowIf(nameof(m_WhichController), SelfType.CustomGameObject)]
        private TopDownController m_TopDownController;

        [SerializeField]
        private Vector3 m_Movement = Vector3.zero;

        private GameObject m_Self;
        private TopDownController m_CachedTopDownController;

        public ICachedGameAction Clone()
        {
            return new SetMovement_CachedGameAction
            {
                m_WhichController = m_WhichController,
                m_TopDownController = m_TopDownController,
                m_Movement = m_Movement
            };
        }

        public void Execute()
        {
            if (m_CachedTopDownController == null)
            {
                Debug.LogError(
                    $"{nameof(SetMovement_CachedGameAction)}: " +
                    "No TopDownController is cached. " +
                    "Make sure SetGameObject was called successfully."
                );
                return;
            }

            m_CachedTopDownController.SetMovement(m_Movement);
        }

        public GameObject GetGameObject()
        {
            return m_Self;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            m_Self = gameObj;
            m_CachedTopDownController = null;

            if (m_WhichController == SelfType.CustomGameObject)
            {
                m_CachedTopDownController = m_TopDownController;
            }
            else if (m_Self != null)
            {
                m_Self.TryGetComponent(
                    out m_CachedTopDownController
                );
            }

            if (m_CachedTopDownController == null)
            {
                Debug.LogError(
                    $"{nameof(SetMovement_CachedGameAction)}: " +
                    "Could not find a TopDownController component."
                );
                return false;
            }

            return true;
        }
    }
}
