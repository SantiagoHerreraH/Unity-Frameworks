using SilverPillar.Core;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Target
{
    [Serializable]
    public class DoTargetsFulfillInteractionConditions_CachedCondition : ICachedCondition
    {
        public enum WhichTargets
        {
            AllPossibleTargets,
            AnyPossibleTarget
        }

        [SerializeField]
        private WhichTargets m_WhichTargets;

        [SerializeField]
        private bool m_WhatToReturnIfThereAreNoPossibleTargets;

        [OdinSerialize, ShowInInspector]
        private IInteractionCondition m_ConditionToFulfill = null;

        private TargetSystem m_TargetSystem;

        public DoTargetsFulfillInteractionConditions_CachedCondition() { }

        public DoTargetsFulfillInteractionConditions_CachedCondition(DoTargetsFulfillInteractionConditions_CachedCondition other)
        {
            m_ConditionToFulfill = other.m_ConditionToFulfill.Clone();
            m_TargetSystem = other.m_TargetSystem;
            m_WhichTargets = other.m_WhichTargets;
        }

        public ICachedCondition Clone()
        {
            return new DoTargetsFulfillInteractionConditions_CachedCondition(this);
        }

        public GameObject GetGameObject()
        {
            return m_TargetSystem ? m_TargetSystem.gameObject : null;
        }

        public bool IsFulfilled()
        {
            if (m_TargetSystem != null )
            {
                if (m_TargetSystem.PossibleTargets.Count == 0)
                {
                    return m_WhatToReturnIfThereAreNoPossibleTargets;
                }

                switch (m_WhichTargets)
                {
                    case WhichTargets.AllPossibleTargets:

                        foreach (var possibleTarget in m_TargetSystem.PossibleTargets)
                        {
                            if (!m_ConditionToFulfill.IsFulfilled(m_TargetSystem.CurrentTarget))
                            {
                                return false;
                            }
                        }
                        return true;
                    case WhichTargets.AnyPossibleTarget:

                        foreach (var possibleTarget in m_TargetSystem.PossibleTargets)
                        {
                            if (m_ConditionToFulfill.IsFulfilled(m_TargetSystem.CurrentTarget))
                            {
                                return true;
                            }
                        }
                        return false;

                    default:
                        break;
                }
            }

            return false;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            if (gameObj.TryGetComponent(out m_TargetSystem))
            {
                return m_ConditionToFulfill.SetGameObject(gameObj);
            }

            return false;
        }
    }
}

