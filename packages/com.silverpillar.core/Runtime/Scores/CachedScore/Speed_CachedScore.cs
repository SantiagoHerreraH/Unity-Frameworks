using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class Speed_CachedScore : ICachedScore
    {
#nullable enable
        [SerializeField]
        private SelfType m_SpeedFromWho;

        [SerializeField, ShowIf(nameof(m_SpeedFromWho), SelfType.CustomGameObject)]
        private Rigidbody? m_Rigidbody;

        /// <summary>
        /// Returns the magnitude of the Rigidbody's velocity.
        /// </summary>
        public float CalculateScore()
        {
            if (m_Rigidbody == null) return 0f;

            // Magnitude represents the scalar speed (always positive)
            return m_Rigidbody.linearVelocity.magnitude;
        }

        /// <summary>
        /// Creates a copy of this score provider.
        /// </summary>
        public ICachedScore Clone()
        {
            return new Speed_CachedScore
            {
                m_Rigidbody = this.m_Rigidbody
            };
        }

        public GameObject? GetGameObject() => m_Rigidbody ? m_Rigidbody.gameObject : null;

        /// <summary>
        /// Stores the GameObject and caches the Rigidbody component for efficiency.
        /// </summary>
        public bool SetGameObject(GameObject self)
        {
            if (self == null) return false;

            if (m_SpeedFromWho == SelfType.CustomGameObject )
            {
                return m_Rigidbody != null;
            }

            return self.TryGetComponent<Rigidbody>(out m_Rigidbody); ;
        }
    }
}
