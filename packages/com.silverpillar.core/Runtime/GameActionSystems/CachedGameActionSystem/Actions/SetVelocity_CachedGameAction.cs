using Sirenix.OdinInspector;
using UnityEngine;

namespace SilverPillar.Core
{
    public class SetVelocity_CachedGameAction : ICachedGameAction
    {

        [SerializeField]
        private SelfType m_WhichRigidbody;

        [SerializeField, ShowIf(nameof(m_WhichRigidbody), SelfType.CustomGameObject)]
        private Rigidbody m_Rigidbody;

        [SerializeField]
        private Vector3 m_Velocity = Vector3.zero;

        private GameObject m_Self;
        private Rigidbody m_CachedRigidbody;

        public ICachedGameAction Clone()
        {
            return new SetVelocity_CachedGameAction
            {
                m_WhichRigidbody = m_WhichRigidbody,
                m_Rigidbody = m_Rigidbody,
                m_Velocity = m_Velocity,
                m_Self = m_Self,
                m_CachedRigidbody = m_CachedRigidbody
            };
        }

        public void Execute()
        {
            if (m_CachedRigidbody == null)
            {
                Debug.LogError(
                    $"{nameof(SetVelocity_CachedGameAction)}: " +
                    "No TopDownController is cached. " +
                    "Make sure SetGameObject was called successfully."
                );
                return;
            }

            m_CachedRigidbody.linearVelocity = m_Velocity;
        }

        public GameObject GetGameObject()
        {
            return m_Self;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            m_Self = gameObj;
            m_CachedRigidbody = null;

            if (m_WhichRigidbody == SelfType.CustomGameObject)
            {
                m_CachedRigidbody = m_Rigidbody;
            }
            else if (m_Self != null)
            {
                m_Self.TryGetComponent(
                    out m_CachedRigidbody
                );
            }

            if (m_CachedRigidbody == null)
            {
                Debug.LogError(
                    $"{nameof(SetVelocity_CachedGameAction)}: " +
                    "Could not find a Rigidbody component."
                );
                return false;
            }

            return true;
        }
    }
}
