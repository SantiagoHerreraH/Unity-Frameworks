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
    [Serializable]
    public struct TargetAndScore
    {
        public GameObject Target;
        public float Score;
    }
    public class TargetSystem : SerializedMonoBehaviour
    {
        private static List<TargetSystem> m_AllTargetSystems = new();
        private static List<PossibleTarget> m_StaticPossibleTargets = new List<PossibleTarget>();
        public static void RegisterStaticPossibleTarget(PossibleTarget possibleTarget)
        {
            if (!m_StaticPossibleTargets.Contains(possibleTarget))
            {
                m_StaticPossibleTargets.Add(possibleTarget);

                foreach (var item in m_AllTargetSystems)
                {
                    item.m_StaticTargetsAppended = false;
                }
            }
        }
        public static void UnregisterStaticPossibleTarget(PossibleTarget possibleTarget)
        {
            m_StaticPossibleTargets.Remove(possibleTarget);

            foreach (var item in m_AllTargetSystems)
            {
                item.m_StaticTargetsAppended = false;
            }
        }

        public enum WhenToChooseTarget
        {
            DontChooseAutomatically,
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

        [FoldoutGroup("How to Choose Current Target")]

        [Title("Filter")]
        [OdinSerialize, ShowInInspector]
        private IInteractionCondition m_ConditionToChooseTheCurrentTarget = null;
        [FoldoutGroup("How to Choose Current Target")]
        [SerializeField]
        private WhatToDoIfPossibleTargetDoesntPassFilter m_WhatToDoIfPossibleTargetDoesntPassFilter;

        [Space(10)]

        [FoldoutGroup("How to Choose Current Target")]
        [Title("Scoring")]
        [SerializeField]
        private WhichScoreToChoose m_WhichScoreToChoose;
        [FoldoutGroup("How to Choose Current Target")]
        [SerializeField]
        private bool m_ChooseFromStaticPossibleTargetsAsWell = true;
        private bool m_StaticTargetsAppended = false;

        [FoldoutGroup("How to Choose Current Target")]
        [OdinSerialize, ShowInInspector]
        private ICachedInteractionScore m_HowToCalculateScore;

        [FoldoutGroup("When to Choose Current Target")]
        [SerializeField]
        private WhenToChooseTarget m_WhenToChooseTarget = WhenToChooseTarget.DontChooseAutomatically;

        [FoldoutGroup("When to Choose Current Target")]
        [SerializeField, Min(0), Tooltip("0 means every tick"), ShowIf(nameof(m_WhenToChooseTarget), WhenToChooseTarget.OnUpdate)]
        private float m_HowOftenToRecalculateCurrentTarget;
        private float m_TimeSinceLastCalculatedCurrentTarget = 0;

        [FoldoutGroup("Events")]
        [SerializeField]
        private UnityEvent<GameObject> m_OnNewCurrentTarget = new();

        [FoldoutGroup("Targets")]
        [SerializeField]
        private GameObject m_CurrentTarget;
        [FoldoutGroup("Targets")]
        [SerializeField]
        private List<GameObject> m_PossibleTargets = new(); // for iteration
        private HashSet<GameObject> m_PossibleTargetsHashset = new(); // for queries

        [FoldoutGroup("Debug")]
        [SerializeField]
        private bool m_PrintOnChangeTarget;

        [FoldoutGroup("Debug")]
        [SerializeField]
        private bool m_PrintOnRegisterPossibleTarget;
        [FoldoutGroup("Debug")]
        [SerializeField]
        private bool m_PrintOnUnregisterPossibleTarget;

        [FoldoutGroup("Debug")]
        [ShowInInspector, ReadOnly, SerializeField]
        private List<TargetAndScore> m_QualifiedTargets = new();
        public GameObject CurrentTarget { get { return m_CurrentTarget; } }
        public List<GameObject> PossibleTargets { 
            get {

                AppendStatic();
                return m_PossibleTargets; 
            } }
        
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

                if (m_PrintOnRegisterPossibleTarget)
                {
                    Debug.Log($"{gameObject.name}'s Target System <b><color=#F44336>[Registered Target]</color></b> {possibleTarget.name}");
                }

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

                if (m_PrintOnUnregisterPossibleTarget)
                {
                    Debug.Log($"{gameObject.name}'s Target System <b><color=#F44336>[Unregistered Target]</color></b> {possibleTarget.name}");
                }

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

        public void ChangeCurrentTarget(GameObject newTarget)
        {
            var oldTarget = m_CurrentTarget;
            m_CurrentTarget = newTarget;

            if (newTarget != oldTarget)
            {
                m_OnNewCurrentTarget.Invoke(newTarget);
            }

            if (m_PrintOnChangeTarget)
            {
                Debug.Log($"{gameObject}'s Target System Current Target is {newTarget}.");
            }
        }

        [FoldoutGroup("Debug")]
        [Button]
        private void ChooseCurrentTarget()
        {
            AppendStatic();
            Initialize();

            m_QualifiedTargets.Clear();

            List<GameObject> targetsToRemove = null;

            foreach (GameObject possibleTarget in m_PossibleTargets)
            {
                if (m_ConditionToChooseTheCurrentTarget == null || 
                    m_ConditionToChooseTheCurrentTarget.IsFulfilled(possibleTarget))
                {
                    float finalScore = m_HowToCalculateScore != null ? m_HowToCalculateScore.CalculateScore(possibleTarget) : 0f;

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

                ChangeCurrentTarget(newTarget);

            }
        }

        private void Initialize()
        {
            if (!m_Initialized)
            {
                m_AllTargetSystems.Add(this);
                AppendStatic();
                m_HowToCalculateScore?.SetGameObject(gameObject);
                m_ConditionToChooseTheCurrentTarget?.SetGameObject(gameObject);
                m_Initialized = true;

            }
        }

        private void AppendStatic()
        {
            if (m_ChooseFromStaticPossibleTargetsAsWell && !m_StaticTargetsAppended)
            {
                foreach (var item in m_StaticPossibleTargets)
                {
                    m_PossibleTargets.Add(item.gameObject);
                }

                m_StaticTargetsAppended = true;
            }
        }
    }
}

