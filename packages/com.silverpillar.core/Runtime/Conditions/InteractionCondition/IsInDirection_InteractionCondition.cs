using System;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class IsInDirection_InteractionCondition : IInteractionCondition
    {
        [SerializeField]
        private Vector3 m_SelfForwardVector = Vector3.forward;

        [SerializeField, Range(0f, 180f)]
        [Tooltip("If Target is outside this angle from self forward vector, it is not in direction.")]
        private float m_ForwardAngle = 45f;

        private GameObject m_Self;

        public IInteractionCondition Clone()
        {
            return new IsInDirection_InteractionCondition
            {
                m_SelfForwardVector = m_SelfForwardVector,
                m_ForwardAngle = m_ForwardAngle,
                m_Self = m_Self
            };
        }

        public GameObject GetGameObject()
        {
            return m_Self;
        }

        public bool IsFulfilled(GameObject target)
        {
            if (m_Self == null || target == null)
            {
                return false;
            }

            if (m_SelfForwardVector.sqrMagnitude <= Mathf.Epsilon)
            {
                return false;
            }

            // Convert the configured local direction into world space.
            Vector3 forwardDirection =
                m_Self.transform.TransformDirection(m_SelfForwardVector);

            // Direction from self to target.
            Vector3 targetDirection =
                target.transform.position - m_Self.transform.position;

            if (targetDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                return false;
            }

            // Calculate the angle between both directions.
            float angle = Vector3.Angle(forwardDirection, targetDirection);

            return angle <= m_ForwardAngle;
        }

        public bool SetGameObject(GameObject self)
        {
            m_Self = self;
            return m_Self != null;
        }
    }
}