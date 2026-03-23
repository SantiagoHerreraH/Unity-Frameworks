using System;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class Distance_InteractionCondition : IInteractionCondition
    {
        [SerializeField]
        private FloatComparison.OperationType m_IfDistanceIs;
        [SerializeField, Min(0.001f)] private float m_ThanDistanceToCompare = 2.0f;
        
        private GameObject _self;

        public IInteractionCondition Clone()
        {
            return new Distance_InteractionCondition
            {
                m_ThanDistanceToCompare = this.m_ThanDistanceToCompare,
                _self = this._self
            };
        }

        public GameObject GetGameObject() => _self;

        public bool SetGameObject(GameObject self)
        {
            _self = self;
            return _self != null;
        }

        public bool IsFulfilled(GameObject target)
        {
            if (_self == null || target == null) return false;

            float distanceSqr = (_self.transform.position - target.transform.position).sqrMagnitude;
            return FloatComparison.Compare(distanceSqr, m_IfDistanceIs, m_ThanDistanceToCompare * m_ThanDistanceToCompare);
        }
    }
}
