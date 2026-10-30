using SilverPillar.Core;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace SilverPillar.Integrations.MMTopDown
{
    public class CombatRangeType : SerializedMonoBehaviour
    {
        private enum RangeOptions { ShortRange, LongRange, Both }
        private enum ThresholdType { SetValue, AddValueFromShortRange, SubtractValueFromLongRange }

        [SerializeField] private RangeOptions m_RangeOptions;

        private bool m_AllowShortRange => m_RangeOptions != RangeOptions.LongRange;
        private bool m_AllowLongRange => m_RangeOptions != RangeOptions.ShortRange;

        [OdinSerialize, ShowIf(nameof(m_AllowShortRange)), Tooltip("If distance from target is equal or less than this, allow short range")]
        private ICachedScore m_ShortRange;
        private float m_ShortRangeValue;

        [SerializeField, ShowIf(nameof(m_RangeOptions), RangeOptions.Both), Tooltip("How the threshold is calculated.")]
        private ThresholdType m_ThresholdType;

        [OdinSerialize, ShowIf(nameof(m_RangeOptions), RangeOptions.Both), Tooltip("If distance from target is equal or less than this, prefer short range, else, prefer long range.")]
        private ICachedScore m_Threshold;
        private float m_ThresholdValue;

        [OdinSerialize, ShowIf(nameof(m_AllowLongRange))]
        private ICachedScore m_IdealLongRange;
        private float m_IdealLongRangeValue;

        private GameObject m_Target;
        private bool m_Initialized = false;

        private void Initialize()
        {
            if (m_Initialized) { return; }

            if (m_ShortRange != null)
            {
                if (m_ShortRange.SetGameObject(gameObject))
                {
                    m_ShortRangeValue = m_ShortRange.CalculateScore();
                }
                else
                {
                    Debug.LogError($"[{nameof(CombatRangeType)}] Failed to set GameObject for {nameof(m_ShortRange)} on {gameObject.name}");
                }
            }

            if (m_Threshold != null)
            {
                if (m_Threshold.SetGameObject(gameObject))
                {
                    m_ThresholdValue = m_Threshold.CalculateScore();
                }
                else
                {
                    Debug.LogError($"[{nameof(CombatRangeType)}] Failed to set GameObject for {nameof(m_Threshold)} on {gameObject.name}");
                }
            }

            if (m_IdealLongRange != null)
            {
                if (m_IdealLongRange.SetGameObject(gameObject))
                {
                    m_IdealLongRangeValue = m_IdealLongRange.CalculateScore();
                }
                else
                {
                    Debug.LogError($"[{nameof(CombatRangeType)}] Failed to set GameObject for {nameof(m_IdealLongRange)} on {gameObject.name}");
                }
            }

            m_Initialized = true;
        }

        public void RecalculateValues()
        {
            Initialize(); // Ensure initialized before calling scores to avoid empty setups

            if (m_ShortRange != null)
            {
                m_ShortRangeValue = m_ShortRange.CalculateScore();
            }
            if (m_Threshold != null)
            {
                m_ThresholdValue = m_Threshold.CalculateScore();
            }
            if (m_IdealLongRange != null)
            {
                m_IdealLongRangeValue = m_IdealLongRange.CalculateScore();
            }
        }

        public GameObject GetTarget()
        {
            return m_Target;
        }

        public void NullTarget()
        {
            m_Target = null;
        }

        public void SetTarget(GameObject target)
        {
            m_Target = target;
        }

        public bool IsPermittedToAttackShortRange()
        {
            return m_AllowShortRange;
        }

        public bool IsPermittedToAttackLongRange()
        {

            return m_AllowLongRange;
        }

        public bool CanAttackLongRange()
        {
            if (!m_AllowLongRange || m_Target == null) return false;
            Initialize();

            float distance = Vector3.Distance(transform.position, m_Target.transform.position);
            return distance <= m_IdealLongRangeValue;
        }

        public bool CanAttackShortRange()
        {
            if (!m_AllowShortRange || m_Target == null) return false;
            Initialize();

            float distance = Vector3.Distance(transform.position, m_Target.transform.position);
            return distance <= m_ShortRangeValue;
        }

        public bool PreferLongRange()
        {
            if (!m_AllowLongRange) return false;
            if (!m_AllowShortRange) return true; // Only LongRange is allowed
            if (m_Target == null) return false;

            return !PreferShortRange();
        }

        public bool PreferShortRange()
        {
            if (!m_AllowShortRange) return false;
            if (!m_AllowLongRange) return true; // Only ShortRange is allowed
            if (m_Target == null) return false;
            Initialize();

            float distance = Vector3.Distance(transform.position, m_Target.transform.position);
            float calculatedThreshold = GetCalculatedThreshold();

            return distance <= calculatedThreshold;
        }

        private float GetCalculatedThreshold()
        {
            switch (m_ThresholdType)
            {
                case ThresholdType.AddValueFromShortRange:
                    return m_ShortRangeValue + m_ThresholdValue;
                case ThresholdType.SubtractValueFromLongRange:
                    return m_IdealLongRangeValue - m_ThresholdValue;
                case ThresholdType.SetValue:
                default:
                    return m_ThresholdValue;
            }
        }
    }
}
