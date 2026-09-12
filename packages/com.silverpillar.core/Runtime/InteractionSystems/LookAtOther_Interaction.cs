using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class LookAtOther_Interaction : IInteraction
    {
        [SerializeField]
        private SelfType m_WhoToRotate;
        [SerializeField, ShowIf(nameof(m_WhoToRotate), SelfType.CustomGameObject)]
        private GameObject m_RotationTarget;

        [SerializeField] 
        private AxisFilter m_RotationAxes = AxisFilter.xyz;
        [SerializeField] 
        private bool m_InstantRotation = false;
        [SerializeField, HideIf(nameof(m_InstantRotation))] 
        private float m_RotationSpeed = 5f;

        private GameObject m_Self;

        public IInteraction Clone()
        {
            return new LookAtOther_Interaction
            {
                m_Self = this.m_Self,
                m_RotationAxes = this.m_RotationAxes,
                m_InstantRotation = this.m_InstantRotation,
                m_RotationSpeed = this.m_RotationSpeed,
                m_RotationTarget = this.m_RotationTarget
            };
        }

        public GameObject GetSelf() => m_Self;

        public bool SetSelf(GameObject self)
        {
            if (self == null) return false;
            m_Self = self;
            if (m_WhoToRotate == SelfType.ThisGameObject)
            {
                m_RotationTarget = m_Self;
            }
            return true;
        }

        public void Interact(GameObject target)
        {
            if (m_RotationTarget == null || target == null) return;

            Vector3 direction = target.transform.position - m_RotationTarget.transform.position;

            if (direction.sqrMagnitude < 0.001f) return;


            if (direction == Vector3.zero) return;

            Quaternion targetRotation = Quaternion.LookRotation(direction);

            direction = AxisFilterTools.GetFilteredVector(targetRotation.eulerAngles, m_RotationAxes);

            targetRotation = Quaternion.Euler(direction);

            if (!m_InstantRotation)
            {
                m_RotationTarget.transform.rotation = Quaternion.Slerp(m_RotationTarget.transform.rotation, targetRotation, m_RotationSpeed * Time.deltaTime);
            }
            else
            {
                m_RotationTarget.transform.rotation = targetRotation;
            }
        }
        
    }
}
