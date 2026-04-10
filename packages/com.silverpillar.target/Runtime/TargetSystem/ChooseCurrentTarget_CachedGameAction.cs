using SilverPillar.Core;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SilverPillar.Target
{
    [Serializable]
    public class ChooseCurrentTarget_CachedGameAction : ICachedGameAction
    {
        [Title("Filter")]
        [OdinSerialize, ShowInInspector]
        private IInteractionCondition m_ConditionToChooseTheCurrentTarget = null;

        [Title("How to Choose Current Target")]
        [SerializeField]
        private WhichScoreToChoose m_WhichScoreToChoose;
        [OdinSerialize, ShowInInspector]
        private ICachedInteractionScore m_HowToCalculateScore = null;

        private TargetSystem m_TargetSystem;

        private List<TargetAndScore> m_QualifiedTargets = new();
        public ChooseCurrentTarget_CachedGameAction() { }
        public ChooseCurrentTarget_CachedGameAction(ChooseCurrentTarget_CachedGameAction other)
        {
            m_ConditionToChooseTheCurrentTarget = other.m_ConditionToChooseTheCurrentTarget.Clone();
            m_HowToCalculateScore = other.m_HowToCalculateScore.Clone();
            m_TargetSystem = other.m_TargetSystem;
            m_QualifiedTargets = new(other.m_QualifiedTargets);
        }

        public ICachedGameAction Clone()
        {
            return new ChooseCurrentTarget_CachedGameAction(this);
        }

        public void Execute()
        {
            if (m_TargetSystem == null)
            {
                return;
            }

            m_QualifiedTargets.Clear();

            foreach (GameObject possibleTarget in m_TargetSystem.PossibleTargets)
            {
                if (m_ConditionToChooseTheCurrentTarget.IsFulfilled(possibleTarget))
                {
                    float finalScore = m_HowToCalculateScore != null ? m_HowToCalculateScore.CalculateScore(possibleTarget) : 0f;

                    m_QualifiedTargets.Add(new TargetAndScore { Target = possibleTarget, Score = finalScore });
                }
            }


            if (m_QualifiedTargets.Count > 0)
            {
                GameObject newTarget = null;
                switch (m_WhichScoreToChoose)
                {
                    case WhichScoreToChoose.Highest:

                        newTarget = m_QualifiedTargets
                            .OrderByDescending(t => t.Score)
                            .FirstOrDefault().Target;

                        break;
                    case WhichScoreToChoose.Lowest:

                        newTarget = m_QualifiedTargets
                            .OrderByDescending(t => t.Score)
                            .LastOrDefault().Target;

                        break;
                    default:
                        break;
                }

                m_TargetSystem.ChangeCurrentTarget(newTarget);
            }
        }

#nullable enable

        public GameObject? GetGameObject()
        {
            return m_TargetSystem != null ? m_TargetSystem.gameObject : null;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            return gameObj.TryGetComponent(out m_TargetSystem);
        }
    }
}
