using SilverPillar.Core;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class VelocityDirection_CachedScore : ICachedScore
    {
#nullable enable
        [InfoBox(
            "Calculates the scalar projection of the current velocity onto the " +
            "custom axis relative to the object's forward orientation.")]
        [SerializeField] private Vector3 speedAxisVector = Vector3.forward;
        [SerializeField] private SelfType m_WhoToUseAsForwardVectorReference;
        [SerializeField, ShowIf(nameof(m_WhoToUseAsForwardVectorReference), SelfType.CustomGameObject)]
        private Rigidbody? m_Rigidbody;

        public ICachedScore Clone()
        {
            return new VelocityDirection_CachedScore
            {
                m_WhoToUseAsForwardVectorReference = this.m_WhoToUseAsForwardVectorReference,
                speedAxisVector = this.speedAxisVector,
                m_Rigidbody = this.m_Rigidbody
            };
        }

        public GameObject? GetGameObject() => m_Rigidbody ? m_Rigidbody.gameObject : null;

        public bool SetGameObject(GameObject self)
        {
            if (self == null) return false;

            if (m_WhoToUseAsForwardVectorReference != SelfType.CustomGameObject)
            {
                 self.TryGetComponent<Rigidbody>(out m_Rigidbody);
            }

            return m_Rigidbody != null;
        }

        /// <summary>
        /// Calcula cuánto de la velocidad actual coincide con el vector de eje 
        /// en relación con la orientación frontal (forward) del objeto.
        /// </summary>
        public float CalculateScore()
        {
            if (m_Rigidbody == null) return 0f;

            Vector3 worldAxis = m_Rigidbody.transform.TransformDirection(speedAxisVector.normalized);

            float projection = Vector3.Dot(m_Rigidbody.linearVelocity, worldAxis);

            return projection;
        }
    }
}
