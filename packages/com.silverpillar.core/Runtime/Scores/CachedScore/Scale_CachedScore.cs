using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class Scale_CachedScore : ICachedScore
    {
        public enum ScaleValueType
        {
            Average,
            Highest,
            Lowest,
            ChooseOneAxis
        }

        public enum Vector3Axis
        {
            X,
            Y,
            Z
        }

        [SerializeField]
        private ScaleValueType m_ScaleValueType;

        [SerializeField, HideIf(nameof(m_ScaleValueType), ScaleValueType.ChooseOneAxis)]
        private AxisFilter m_FromWhatScaleAxis;

        [SerializeField, ShowIf(nameof(m_ScaleValueType), ScaleValueType.ChooseOneAxis)]
        private Vector3Axis m_FromWhatSingleScaleAxis;

        [SerializeField]
        private SelfType m_ScaleFromWho;

        [SerializeField, ShowIf(nameof(m_ScaleFromWho), SelfType.CustomGameObject)]
        private GameObject m_Target;

        public float CalculateScore()
        {
            if (m_Target == null) return 0f;

            Vector3 scale = m_Target.transform.localScale;

            // Handle single axis selection immediately
            if (m_ScaleValueType == ScaleValueType.ChooseOneAxis)
            {
                return m_FromWhatSingleScaleAxis switch
                {
                    Vector3Axis.X => scale.x,
                    Vector3Axis.Y => scale.y,
                    Vector3Axis.Z => scale.z,
                    _ => 0f
                };
            }

            // Get the filtered list of axis values
            List<float> values = GetFilteredAxes(scale);
            if (values.Count == 0) return 0f;

            // Calculate based on the selected type
            return m_ScaleValueType switch
            {
                ScaleValueType.Average => values.Average(),
                ScaleValueType.Highest => values.Max(),
                ScaleValueType.Lowest => values.Min(),
                _ => 0f
            };
        }

        private List<float> GetFilteredAxes(Vector3 scale)
        {
            List<float> result = new List<float>();

            // Check flags based on the AxisFilter enum
            bool includeX = m_FromWhatScaleAxis is AxisFilter.xyz or AxisFilter.xz or AxisFilter.xy or AxisFilter.x;
            bool includeY = m_FromWhatScaleAxis is AxisFilter.xyz or AxisFilter.yz or AxisFilter.xy or AxisFilter.y;
            bool includeZ = m_FromWhatScaleAxis is AxisFilter.xyz or AxisFilter.xz or AxisFilter.yz or AxisFilter.z;

            if (includeX) result.Add(scale.x);
            if (includeY) result.Add(scale.y);
            if (includeZ) result.Add(scale.z);

            return result;
        }

        public bool SetGameObject(GameObject self)
        {
            if (m_ScaleFromWho == SelfType.ThisGameObject)
            {
                m_Target = self;
            }

            return m_Target != null;
        }

        public GameObject GetGameObject()
        {
            return m_Target;
        }

        public ICachedScore Clone()
        {
            return new Scale_CachedScore
            {
                m_ScaleValueType = this.m_ScaleValueType,
                m_FromWhatScaleAxis = this.m_FromWhatScaleAxis,
                m_FromWhatSingleScaleAxis = this.m_FromWhatSingleScaleAxis,
                m_Target = this.m_Target
            };
        }
    }
}
