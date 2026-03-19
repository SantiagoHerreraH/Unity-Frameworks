using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using SilverPillar.Core;

namespace SilverPillar.Target
{
    public class TargetSystem : MonoBehaviour
    {
        [Serializable]
        public struct TargetAndScore
        {
            public GameObject Target;
            public float Score;
        }

        public enum WhenToChooseTarget
        {
            OnUpdate,
            OnRegisterPossibleTarget,
            OnUnregisterPossibleTarget,
            OnRegisterAndUnregisterPossibleTarget,
            OnEnable
        }

        [TabGroup("How to Choose Current Target")]

        [Title("Filter")]
        [SerializeField]
        private List<ConditionGroupData> m_ConditionsToChooseTheCurrentTarget = new();
        
        [Space(10)]

        [TabGroup("How to Choose Current Target")]
        [Title("Scoring")]
        [InfoBox("You always choose the possible target with the highest score as the current target.")]
        [SerializeField]
        private ScoreGroup.HowToCalculateScore m_HowToCalculateScore;

        [TabGroup("How to Choose Current Target")]
        [SerializeField]
        private List<SaveableScore> m_ScoringSystemToChooseTheCurrentTarget = new();
        private List<float> m_TempScores = new(); //  just for optimization

        [TabGroup("When to Choose Current Target")]
        [SerializeField]
        private WhenToChooseTarget m_WhenToChooseTarget;

        [TabGroup("When to Choose Current Target")]
        [SerializeField, Min(0), Tooltip("0 means every tick"), ShowIf(nameof(m_WhenToChooseTarget), WhenToChooseTarget.OnUpdate)]
        private float m_HowOftenToRecalculateCurrentTarget;
        private float m_TimeSinceLastCalculatedCurrentTarget = 0;

        [TabGroup("Events")]
        [SerializeField]
        private UnityEvent<GameObject> m_OnNewCurrentTarget = new();

        [PropertySpace(SpaceBefore = 20)]

        [FoldoutGroup("Debug")]
        [ShowInInspector, ReadOnly, SerializeField]
        private GameObject m_CurrentTarget;
        [FoldoutGroup("Debug")]
        [ShowInInspector, ReadOnly, SerializeField]
        private List<GameObject> m_PossibleTargets = new(); // for iteration
        private HashSet<GameObject> m_PossibleTargetsHashset = new(); // for queries
        [FoldoutGroup("Debug")]
        [ShowInInspector, ReadOnly, SerializeField]
        private List<TargetAndScore> m_QualifiedTargets = new();
        public GameObject CurrentTarget { get; }

        private void OnEnable()
        {
            if (m_WhenToChooseTarget == WhenToChooseTarget.OnEnable)
            {
                ChooseCurrentTarget();
            }
        }
        private void Update()
        {
            if (m_WhenToChooseTarget != WhenToChooseTarget.OnUpdate) return;

            m_TimeSinceLastCalculatedCurrentTarget += Time.deltaTime;
            if (m_HowOftenToRecalculateCurrentTarget <= m_TimeSinceLastCalculatedCurrentTarget)
            {
                ChooseCurrentTarget();
                m_TimeSinceLastCalculatedCurrentTarget = 0;
            }

        }

        public void RegisterPossibleTarget(GameObject possibleTarget)
        {
            if (possibleTarget == null) return;
            if (!m_PossibleTargetsHashset.Contains(possibleTarget))
            {
                m_PossibleTargetsHashset.Add(possibleTarget);
                m_PossibleTargets.Add(possibleTarget);

                if (m_WhenToChooseTarget == WhenToChooseTarget.OnRegisterPossibleTarget ||
                    m_WhenToChooseTarget == WhenToChooseTarget.OnRegisterAndUnregisterPossibleTarget)
                {
                    ChooseCurrentTarget();
                }
            }
        }

        public void UnregisterPossibleTarget(GameObject possibleTarget)
        {
            if (possibleTarget == null) return;
            if (m_PossibleTargetsHashset.Contains(possibleTarget))
            {
                m_PossibleTargetsHashset.Remove(possibleTarget);
                m_PossibleTargets.Remove(possibleTarget);

                if (m_WhenToChooseTarget == WhenToChooseTarget.OnUnregisterPossibleTarget ||
                    m_WhenToChooseTarget == WhenToChooseTarget.OnRegisterAndUnregisterPossibleTarget)
                {
                    ChooseCurrentTarget();
                }
            }
        }

        public bool HasPossibleTarget(GameObject possibleTarget)
        {
            return m_PossibleTargetsHashset.Contains(possibleTarget);
        }

        [FoldoutGroup("Debug")]
        [Button]
        private void ChooseCurrentTarget()
        {
            m_QualifiedTargets.Clear();

            foreach (GameObject possibleTarget in m_PossibleTargets)
            {
                bool passes = true;

                foreach (var condition in m_ConditionsToChooseTheCurrentTarget)
                {
                    if (!condition.IsFulfilled(possibleTarget))
                    {
                        passes = false;
                        break;
                    }
                }

                if (passes)
                {
                    if (m_TempScores.Count != m_ScoringSystemToChooseTheCurrentTarget.Count)
                    {
                        m_TempScores.Clear();
                        m_TempScores.AddRange(Enumerable.Repeat(0.0f, m_ScoringSystemToChooseTheCurrentTarget.Count));
                    }

                    int currentIdx = 0;
                    foreach (var scoringSystem in m_ScoringSystemToChooseTheCurrentTarget)
                    {
                        m_TempScores[currentIdx] = scoringSystem.CalculateScore(possibleTarget);
                        ++currentIdx;
                    }

                    float finalScore = ScoreGroup.CalculateScore(m_HowToCalculateScore, m_TempScores);

                    m_QualifiedTargets.Add(new TargetAndScore { Target = possibleTarget, Score = finalScore });
                }
            }

            if (m_QualifiedTargets.Count > 0)
            {
                var oldTarget = m_CurrentTarget;
                m_CurrentTarget = m_QualifiedTargets
                    .OrderByDescending(t => t.Score)
                    .FirstOrDefault().Target;

                if (oldTarget != m_CurrentTarget)
                {
                    m_OnNewCurrentTarget.Invoke(m_CurrentTarget);
                }
            }
        }
    }
}

