using SilverPillar.Core;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace SilverPillar.Senses
{
    [Serializable]
    public class IsSensing_InteractionCondition : IInteractionCondition
    {
        private enum SensingManner
        {
            IsSensing,
            IsInSenseGroup,
            IsInMemory
        }
        [SerializeField]
        private SensingManner m_SensingManner;
        [SerializeField]
        private SelfType m_WhichSenseController;
        [SerializeField, ShowIf(nameof(m_WhichSenseController), SelfType.CustomGameObject)]
        private SenseController m_SenseController;
        private GameObject m_Self;

        public IsSensing_InteractionCondition()
        {

        }

        public IsSensing_InteractionCondition(IsSensing_InteractionCondition other)
        {
            m_WhichSenseController = other.m_WhichSenseController;
            m_SenseController = other.m_SenseController;
            m_Self = other.m_Self;
        }

        public GameObject GetGameObject()
        {
            return m_Self;
        }

        public bool IsFulfilled(GameObject target)
        {
            if (m_SenseController != null && target.TryGetComponent(out SenseTarget senseTarget))
            {
                switch (m_SensingManner)
                {
                    case SensingManner.IsSensing:
                        return m_SenseController.IsSensing(senseTarget);
                    case SensingManner.IsInSenseGroup:
                        return m_SenseController.IsInSensingGroup(senseTarget);
                    case SensingManner.IsInMemory:
                        return
                            m_SenseController.IsInMemory(senseTarget);
                    default:
                        break;
                }
            }

            return false;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            m_Self = gameObj;
            if (m_WhichSenseController == SelfType.ThisGameObject)
            {
                m_SenseController = m_Self.GetComponent<SenseController>();
            }

            return m_SenseController != null && m_Self != null;
        }

        public IInteractionCondition Clone()
        {
            return new IsSensing_InteractionCondition(this);
        }
    }
}
