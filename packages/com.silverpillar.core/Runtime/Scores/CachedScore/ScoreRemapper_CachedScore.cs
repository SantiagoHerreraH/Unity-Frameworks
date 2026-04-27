using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class ScoreRemapper_CachedScore : ICachedScore
    {
        [Title("Input Settings")]
        [OdinSerialize, ShowInInspector]
        private ICachedScore m_InputValue;

        [OdinSerialize, ShowInInspector]
        private ICachedScore m_InputMin;

        [OdinSerialize, ShowInInspector]
        private ICachedScore m_InputMax;

        [Title("Output Settings")]
        [OdinSerialize, ShowInInspector]
        private ICachedScore m_OutputMin;

        [OdinSerialize, ShowInInspector]
        private ICachedScore m_OutputMax;

        [Title("Clamp Settings")]
        [SerializeField]
        private bool m_ClampAtMin;
        [SerializeField]
        private bool m_ClampAtMax;

#nullable enable
        private GameObject? m_Target;

        /// <summary>
        /// Maps the input value from the input range to the output range.
        /// Formula: OutputMin + (NormalizedInput * (OutputMax - OutputMin))
        /// </summary>
        public float CalculateScore()
        {
            if (m_InputValue == null || m_InputMin == null || m_InputMax == null ||
                m_OutputMin == null || m_OutputMax == null) return 0f;

            float val = m_InputValue.CalculateScore();
            float inMin = m_InputMin.CalculateScore();
            float inMax = m_InputMax.CalculateScore();

            // Apply Clamping to the input value
            if (m_ClampAtMin && val < inMin) val = inMin;
            if (m_ClampAtMax && val > inMax) val = inMax;

            // Prevent division by zero
            if (Mathf.Approximately(inMin, inMax)) return m_OutputMin.CalculateScore();

            // 1. Get normalized 0-1 value from input range (Inverse Lerp)
            float t = (val - inMin) / (inMax - inMin);

            // 2. Map that 't' to the output range (Lerp)
            float outMin = m_OutputMin.CalculateScore();
            float outMax = m_OutputMax.CalculateScore();

            return outMin + t * (outMax - outMin);
        }

        /// <summary>
        /// Creates a deep copy of the remapper and all internal score providers.
        /// </summary>
        public ICachedScore Clone()
        {
            return new ScoreRemapper_CachedScore
            {
                m_InputValue = this.m_InputValue?.Clone(),
                m_InputMin = this.m_InputMin?.Clone(),
                m_InputMax = this.m_InputMax?.Clone(),
                m_OutputMin = this.m_OutputMin?.Clone(),
                m_OutputMax = this.m_OutputMax?.Clone(),
                m_ClampAtMin = this.m_ClampAtMin,
                m_ClampAtMax = this.m_ClampAtMax,
                m_Target = this.m_Target
            };
        }

        public GameObject? GetGameObject() => m_Target;

        /// <summary>
        /// Sets the GameObject and propagates it to all five internal score providers.
        /// </summary>
        public bool SetGameObject(GameObject self)
        {
            if (self == null) return false;
            m_Target = self;

            bool success = true;

            if (m_InputValue != null) success &= m_InputValue.SetGameObject(self);
            if (m_InputMin != null) success &= m_InputMin.SetGameObject(self);
            if (m_InputMax != null) success &= m_InputMax.SetGameObject(self);
            if (m_OutputMin != null) success &= m_OutputMin.SetGameObject(self);
            if (m_OutputMax != null) success &= m_OutputMax.SetGameObject(self);

            return success;
        }
    }
}
