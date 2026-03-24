using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using SilverPillar.Core;
using Sirenix.Serialization;

namespace SilverPillar.Target
{
    public class TargetSystem : SerializedMonoBehaviour
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

        public enum WhatToDoIfPossibleTargetDoesntPassFilter
        {
            KeepInThePossibleTargetsArray,
            RemoveFromThePossibleTargetsArray
        }

        [TabGroup("How to Choose Current Target")]

        [Title("Filter")]
        [OdinSerialize, ShowInInspector]
        private IInteractionCondition m_ConditionToChooseTheCurrentTarget = null;
        [SerializeField]
        private WhatToDoIfPossibleTargetDoesntPassFilter m_WhatToDoIfPossibleTargetDoesntPassFilter;

        [Space(10)]

        [TabGroup("How to Choose Current Target")]
        [Title("Scoring")]
        [InfoBox("You always choose the possible target with the highest score as the current target.")]
        [SerializeField]
        private HowToCalculateScore m_HowToCalculateScore;

        [TabGroup("How to Choose Current Target")]
        [SerializeField]
        private List<CachedInteractionScore> m_ScoringSystemToChooseTheCurrentTarget = new();
        private List<ICachedInteractionScore> m_ScoringSystemInstances = new();
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
        
        private bool m_Initialized = false;

        private void Awake()
        {
            Initialize();
        }

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
            Initialize();

            m_QualifiedTargets.Clear();

            List<GameObject> targetsToRemove = null;

            foreach (GameObject possibleTarget in m_PossibleTargets)
            {
                if (m_ConditionToChooseTheCurrentTarget.IsFulfilled(possibleTarget))
                {
                    if (m_TempScores.Count != m_ScoringSystemInstances.Count)
                    {
                        m_TempScores.Clear();
                        m_TempScores.AddRange(Enumerable.Repeat(0.0f, m_ScoringSystemInstances.Count));
                    }

                    int currentIdx = 0;
                    foreach (var scoringSystem in m_ScoringSystemInstances)
                    {
                        m_TempScores[currentIdx] = scoringSystem.CalculateScore(possibleTarget);
                        ++currentIdx;
                    }

                    float finalScore = ScoreTools.CalculateScore(m_HowToCalculateScore, m_TempScores);

                    m_QualifiedTargets.Add(new TargetAndScore { Target = possibleTarget, Score = finalScore });
                }
                else if(
                    m_WhatToDoIfPossibleTargetDoesntPassFilter ==
                    WhatToDoIfPossibleTargetDoesntPassFilter.RemoveFromThePossibleTargetsArray)
                {
                    if(targetsToRemove == null)
                    {
                        targetsToRemove = new List<GameObject>();
                    }

                    targetsToRemove.Add(possibleTarget);
                }
            }

            if(targetsToRemove != null)
            {
                foreach (var target in targetsToRemove)
                {
                    m_PossibleTargets.Remove(target);
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

        private void Initialize()
        {
            if (!m_Initialized)
            {
                foreach (var item in m_ScoringSystemToChooseTheCurrentTarget)
                {
                    m_ScoringSystemInstances.Add(item.Clone(gameObject));
                }
                m_ConditionToChooseTheCurrentTarget.SetGameObject(gameObject);
                m_Initialized = true;

            }
        }
    }
}

