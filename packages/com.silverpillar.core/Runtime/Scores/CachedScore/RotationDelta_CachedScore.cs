using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class RotationDelta_CachedScore : ICachedScore
    {
        public enum MultiplicationType
        {
            None,
            DeltaTime,
            FixedDeltaTime
        };
        public enum RotationCalculationType
        {
            LeftIsNegative,
            LeftIsPositive
        }

        public enum WhatValueToGet
        {
            AngleDelta,
            AngleDeltaDividedBy180
        }

        [SerializeField]
        private SelfType m_WhoIsSelf;
        [SerializeField, ShowIf(nameof(m_WhoIsSelf), SelfType.CustomGameObject)]
        private GameObject m_TargetGameObject;

        [SerializeField]
        private RotationCalculationType m_RotationCalculationType;

        [SerializeField]
        private MultiplicationType m_MultiplicationType;

        [SerializeField]
        private WhatValueToGet m_WhatValueToGet;

        private float m_LastYAngle;
        private bool m_IsInitialized;

        public float CalculateScore()
        {
            if (m_TargetGameObject == null) return 0f;

            float currentYAngle = m_TargetGameObject.transform.eulerAngles.y;

            if (!m_IsInitialized)
            {
                m_LastYAngle = currentYAngle;
                m_IsInitialized = true;
                return 0f;
            }

            // DeltaAngle returns degrees moved: positive is CCW (Left), negative is CW (Right)
            float angleDiff = Mathf.DeltaAngle(m_LastYAngle, currentYAngle);
            m_LastYAngle = currentYAngle;

            switch (m_WhatValueToGet)
            {
                case WhatValueToGet.AngleDelta:
                    break;
                case WhatValueToGet.AngleDeltaDividedBy180:
                    angleDiff /= 180;
                    break;
                default:
                    break;
            }

            // Apply direction multiplier based on the enum
            // If LeftIsNegative, we invert the standard Unity sign
            float directionMultiplier = (m_RotationCalculationType == RotationCalculationType.LeftIsNegative) ? -1f : 1f;

            switch (m_MultiplicationType)
            {
                case MultiplicationType.None:
                    break;
                case MultiplicationType.DeltaTime:
                    angleDiff *= Time.deltaTime;
                    break;
                case MultiplicationType.FixedDeltaTime:
                    angleDiff *= Time.fixedDeltaTime;
                    break;
                default:
                    break;
            }

            return (angleDiff * directionMultiplier);
        }

        public ICachedScore Clone()
        {
            return new RotationDelta_CachedScore
            {
                m_RotationCalculationType = this.m_RotationCalculationType,
                m_TargetGameObject = this.m_TargetGameObject,
                m_LastYAngle = this.m_LastYAngle,
                m_IsInitialized = this.m_IsInitialized
            };
        }

        public GameObject GetGameObject() => m_TargetGameObject;

        public bool SetGameObject(GameObject self)
        {
            if (m_WhoIsSelf == SelfType.ThisGameObject)
            {
                m_TargetGameObject = self;
            }

            m_IsInitialized = false;
            return m_TargetGameObject != null;
        }
    }
}
