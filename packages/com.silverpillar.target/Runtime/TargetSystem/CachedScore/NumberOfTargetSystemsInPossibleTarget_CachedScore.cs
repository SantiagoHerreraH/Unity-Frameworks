using SilverPillar.Core;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace SilverPillar.Target
{
    [Serializable]
    public class NumberOfTargetSystemsInPossibleTarget_CachedScore : ICachedScore
    {
        [SerializeField]
        private SelfType m_GetPossibleTargetFrom;

        [SerializeField, ShowIf(nameof(m_GetPossibleTargetFrom), SelfType.CustomGameObject)]
        private PossibleTarget m_CustomPossibleTarget;

        private GameObject m_GameObject;
        private PossibleTarget m_PossibleTarget;

        public float CalculateScore()
        {
            PossibleTarget possibleTarget = GetPossibleTarget();

            if (possibleTarget == null)
            {
                Debug.LogError($"{nameof(NumberOfTargetSystemsInPossibleTarget_CachedScore)} could not calculate score because no {nameof(PossibleTarget)} was found.");
                return 0f;
            }

            if (possibleTarget.TargetSystemsThatChoseThisAsTarget == null)
            {
                return 0f;
            }

            return possibleTarget.TargetSystemsThatChoseThisAsTarget.Count;
        }

        public ICachedScore Clone()
        {
            return new NumberOfTargetSystemsInPossibleTarget_CachedScore
            {
                m_GetPossibleTargetFrom = m_GetPossibleTargetFrom,
                m_CustomPossibleTarget = m_CustomPossibleTarget,
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

            if (m_GetPossibleTargetFrom == SelfType.CustomGameObject)
            {
                m_PossibleTarget = m_CustomPossibleTarget;
                return m_PossibleTarget != null;
            }

            if (m_GameObject == null)
            {
                m_PossibleTarget = null;
                return false;
            }

            if (!m_GameObject.TryGetComponent(out m_PossibleTarget))
            {
                Debug.LogError($"{nameof(NumberOfTargetSystemsInPossibleTarget_CachedScore)} could not find a {nameof(PossibleTarget)} on {m_GameObject.name}.");
                return false;
            }

            return true;
        }

        private PossibleTarget GetPossibleTarget()
        {
            if (m_GetPossibleTargetFrom == SelfType.CustomGameObject)
            {
                return m_CustomPossibleTarget;
            }

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
    }
}