using SilverPillar.Core;
using System;
using UnityEngine;

namespace SilverPillar.Target
{
    [Serializable]
    public class NumberOfTargetSystemsInPossibleTarget_CachedInteractionScore : ICachedInteractionScore
    {
        [SerializeField]
        private TargetType m_GetPossibleTargetFrom;
        [SerializeField]
        private bool m_LogErrorIfNoPossibleTarget;

        private GameObject m_GameObject;
        private PossibleTarget m_PossibleTarget;

        public float CalculateScore(GameObject target)
        {
            PossibleTarget possibleTarget = GetPossibleTarget(target);

            if (possibleTarget == null)
            {
                if (m_LogErrorIfNoPossibleTarget)
                {
                    Debug.LogError($"{nameof(NumberOfTargetSystemsInPossibleTarget_CachedInteractionScore)} could not calculate score because no {nameof(PossibleTarget)} was found.");
                }

                return 0f;
            }

            if (possibleTarget.TargetSystemsThatChoseThisAsTarget == null)
            {
                return 0f;
            }

            return possibleTarget.TargetSystemsThatChoseThisAsTarget.Count;
        }

        public ICachedInteractionScore Clone()
        {
            return new NumberOfTargetSystemsInPossibleTarget_CachedInteractionScore
            {
                m_GetPossibleTargetFrom = m_GetPossibleTargetFrom,
                m_GameObject = m_GameObject,
                m_PossibleTarget = m_PossibleTarget
            };
        }

        public GameObject GetGameObject()
        {
            return m_GameObject;
        }

        public bool SetGameObject(GameObject self)
        {
            m_GameObject = self;

            if (m_GetPossibleTargetFrom == TargetType.Other)
            {
                return true;
            }

            if (m_GameObject == null)
            {
                m_PossibleTarget = null;
                return false;
            }

            if (!m_GameObject.TryGetComponent(out m_PossibleTarget))
            {
                Debug.LogError($"{nameof(NumberOfTargetSystemsInPossibleTarget_CachedInteractionScore)} could not find a {nameof(PossibleTarget)} on {m_GameObject.name}.");
                return false;
            }

            return true;
        }

        private PossibleTarget GetPossibleTarget(GameObject target)
        {
            switch (m_GetPossibleTargetFrom)
            {
                case TargetType.Self:
                    return GetSelfPossibleTarget();

                case TargetType.Other:
                    return GetOtherPossibleTarget(target);

                default:
                    return null;
            }
        }

        private PossibleTarget GetSelfPossibleTarget()
        {
            if (m_PossibleTarget != null)
            {
                return m_PossibleTarget;
            }

            if (m_GameObject == null)
            {
                return null;
            }

            m_GameObject.TryGetComponent(out m_PossibleTarget);
            return m_PossibleTarget;
        }

        private PossibleTarget GetOtherPossibleTarget(GameObject target)
        {
            if (target == null)
            {
                return null;
            }

            if (!target.TryGetComponent(out PossibleTarget possibleTarget))
            {
                Debug.LogError($"{nameof(NumberOfTargetSystemsInPossibleTarget_CachedInteractionScore)} could not find a {nameof(PossibleTarget)} on {target.name}.");
                return null;
            }

            return possibleTarget;
        }
    }
}