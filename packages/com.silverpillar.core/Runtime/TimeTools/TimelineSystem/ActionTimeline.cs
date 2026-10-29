using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SilverPillar.Core
{
    public enum WhenToCalculateTimelineValues
    {
        OnceWhenSettingTheGameObject,
        EverytimeOnStartingTimeline
    }

    public enum HowToCalculateTimelineDuration
    {
        SetDurationInTimeline, //the duration is explicitly set by the Timeline's m_Duration variable
        CalculateDurationFromTimelineSequenceData, // the duration is calculated by summing all of the durations of the sequence data
        CalculateDurationFromFreeActionData // the duration is calculated by the latest end time of the free action data
    }

    [Serializable]
    public class ActionTimeline : IAction
    {
        // =========================================================
        // ACTION SEQUENCE DATA
        // =========================================================

        [Serializable]
        public class ActionSequenceData
        {
            [Title("Duration")]
            [OdinSerialize, ShowInInspector]
            private ICachedScore m_DurationCalculator;

            [SerializeField]
            private WhenToCalculateTimelineValues m_WhenToCalculateDuration;


            [FoldoutGroup("Duration Runtime Debug")]
            [ReadOnly]
            private float m_StartTime;

            [FoldoutGroup("Duration Runtime Debug")]
            [ReadOnly]
            private float m_EndTime;

            [FoldoutGroup("Duration Runtime Debug")]
            [ReadOnly]
            private float m_Duration;

            // Unclamped calculated duration. Sequence layout must not overwrite this
            // because the timeline duration may change on the next StartAction().
            [NonSerialized]
            private float m_RequestedDuration;


            [Title("Actions")]
            [OdinSerialize, ShowInInspector]
            private List<IAction> m_Actions = new List<IAction>();


            private bool m_UseExplicitTiming;

            public float StartTime => m_StartTime;
            public float EndTime => m_EndTime;
            public float Duration => m_Duration;
            public float RequestedDuration => m_RequestedDuration;


            public ActionSequenceData()
            {
            }

            public ActionSequenceData(float startTime)
            {
                m_StartTime = Mathf.Max(0f, startTime);
            }

            public ActionSequenceData(ActionSequenceData other)
            {
                m_StartTime = other.m_StartTime;
                m_EndTime = other.m_EndTime;
                m_Duration = other.m_Duration;
                m_RequestedDuration = other.m_RequestedDuration;
                m_UseExplicitTiming = other.m_UseExplicitTiming;

                m_WhenToCalculateDuration =
                    other.m_WhenToCalculateDuration;

                if (other.m_DurationCalculator != null)
                {
                    m_DurationCalculator =
                        other.m_DurationCalculator.Clone();
                }

                if (other.m_Actions != null)
                {
                    m_Actions =
                        new List<IAction>(other.m_Actions.Count);

                    for (int i = 0; i < other.m_Actions.Count; i++)
                    {
                        IAction action = other.m_Actions[i];

                        m_Actions.Add(
                            action != null
                                ? action.Clone()
                                : null);
                    }
                }
                else
                {
                    m_Actions = null;
                }
            }


            // =====================================================
            // TIME
            // =====================================================

            public void SetStartTime(float startTime)
            {
                m_StartTime =
                    Mathf.Max(0f, startTime);

                m_EndTime =
                    m_StartTime + m_Duration;
            }

            public void SetDuration(float duration)
            {
                m_RequestedDuration = Mathf.Max(0f, duration);
                m_Duration = m_RequestedDuration;
                m_EndTime = m_StartTime + m_Duration;
            }

            // Used only by timeline layout: preserves the requested duration.
            public void SetClampedDuration(float duration)
            {
                m_Duration = Mathf.Max(0f, duration);
                m_EndTime = m_StartTime + m_Duration;
            }

            public void SetEndTime(float endTime)
            {
                m_EndTime =
                    Mathf.Max(m_StartTime, endTime);

                m_Duration =
                    m_EndTime - m_StartTime;
                m_RequestedDuration = m_Duration;
            }


            /// <summary>
            /// Evaluates the duration calculator if its configured calculation
            /// mode matches the supplied mode.
            /// </summary>
            public void ConfigureExplicit(float duration, IAction action)
            {
                m_UseExplicitTiming = true;
                SetDuration(duration);
                m_Actions = new List<IAction> { action };
            }

            /// <summary>
            /// Use the nested timeline's actual duration, rather than capturing
            /// its possibly unevaluated Duration at composition time.
            /// This is especially important for timeline-creator results.
            /// </summary>
            public void ConfigureFromNestedTimeline(ActionTimeline timeline)
            {
                if (timeline == null)
                    throw new ArgumentNullException(nameof(timeline));

                m_UseExplicitTiming = false;
                m_DurationCalculator = null;
                m_Actions = new List<IAction> { timeline };
                SetDuration(0f); // Resolved after binding and on each parent start.
            }

            public void CalculateDuration(
                WhenToCalculateTimelineValues calculationMode)
            {
                if (m_UseExplicitTiming || m_WhenToCalculateDuration != calculationMode)
                {
                    return;
                }

                if (m_DurationCalculator == null)
                {
                    return;
                }

                SetDuration(
                    m_DurationCalculator.CalculateScore());
            }

            // Inspector-configured sequences may contain ActionTimeline actions
            // without a separate duration calculator. Such actions are run in
            // parallel, so the longest nested timeline defines the segment length.
            private bool HasNestedTimeline()
            {
                if (m_Actions == null) return false;

                for (int i = 0; i < m_Actions.Count; i++)
                {
                    if (m_Actions[i] is ActionTimeline)
                        return true;
                }

                return false;
            }

            internal void PrepareNestedTimelinesForDuration()
            {
                // A score calculator or explicit timing takes precedence.
                if (m_UseExplicitTiming || m_DurationCalculator != null ||
                    m_Actions == null)
                    return;

                for (int i = 0; i < m_Actions.Count; i++)
                {
                    if (m_Actions[i] is ActionTimeline timeline)
                        timeline.PrepareForStart();
                }
            }

            internal void RefreshDurationFromNestedTimelines()
            {
                if (m_UseExplicitTiming || m_DurationCalculator != null ||
                    m_Actions == null)
                    return;

                bool foundTimeline = false;
                float longestDuration = 0f;

                for (int i = 0; i < m_Actions.Count; i++)
                {
                    if (m_Actions[i] is ActionTimeline timeline)
                    {
                        foundTimeline = true;
                        longestDuration = Mathf.Max(longestDuration, timeline.Duration);
                    }
                }

                if (foundTimeline)
                    SetDuration(longestDuration);
            }

            internal void InvalidateNestedTimelinePreparation()
            {
                if (m_Actions == null) return;

                for (int i = 0; i < m_Actions.Count; i++)
                {
                    if (m_Actions[i] is ActionTimeline timeline)
                        timeline.InvalidatePreparedStartValues();
                }
            }


            // =====================================================
            // INITIALIZATION
            // =====================================================

            public bool SetGameObject(GameObject gameObj)
            {
                if (gameObj == null)
                {
                    Debug.LogError(
                        $"{nameof(ActionSequenceData)}: " +
                        $"Cannot set a null GameObject.");

                    return false;
                }

                bool allGood = true;


                // ----------------------------
                // Duration Calculator
                // ----------------------------

                // A nested ActionTimeline is a valid alternative duration source.
                if (m_DurationCalculator == null && !m_UseExplicitTiming)
                {
                    if (!HasNestedTimeline())
                    {
                        Debug.LogError(
                            $"{nameof(ActionSequenceData)}: " +
                            $"{nameof(m_DurationCalculator)} is null and no nested " +
                            $"{nameof(ActionTimeline)} provides a duration.");

                        allGood = false;
                    }
                }
                else
                {
                    bool calculatorGood = m_UseExplicitTiming ||
                        m_DurationCalculator.SetGameObject(gameObj);

                    allGood &= calculatorGood;

                    if (calculatorGood && !m_UseExplicitTiming &&
                        m_WhenToCalculateDuration ==
                        WhenToCalculateTimelineValues
                            .OnceWhenSettingTheGameObject)
                    {
                        SetDuration(
                            m_DurationCalculator.CalculateScore());
                    }
                }


                // ----------------------------
                // Actions
                // ----------------------------

                if (m_Actions == null)
                {
                    Debug.LogError(
                        $"{nameof(ActionSequenceData)}: " +
                        $"{nameof(m_Actions)} is null.");

                    return false;
                }

                for (int i = 0; i < m_Actions.Count; i++)
                {
                    IAction action = m_Actions[i];

                    if (action == null)
                    {
                        Debug.LogError(
                            $"{nameof(ActionSequenceData)} contains " +
                            $"a null action at index {i}.");

                        allGood = false;
                        continue;
                    }

                    allGood &=
                        action.SetGameObject(gameObj);
                }

                return allGood;
            }


            // =====================================================
            // ACTION LIFECYCLE
            // =====================================================

            public void Start()
            {
                if (m_Actions == null)
                {
                    return;
                }

                for (int i = 0; i < m_Actions.Count; i++)
                {
                    m_Actions[i]?.StartAction();
                }
            }

            public void Update()
            {
                if (m_Actions == null)
                {
                    return;
                }

                for (int i = 0; i < m_Actions.Count; i++)
                {
                    m_Actions[i]?.UpdateAction();
                }
            }

            public void End()
            {
                if (m_Actions == null)
                {
                    return;
                }

                for (int i = 0; i < m_Actions.Count; i++)
                {
                    m_Actions[i]?.EndAction();
                }
            }


            public ActionSequenceData Clone()
            {
                return new ActionSequenceData(this);
            }
        }


        // =========================================================
        // ACTION DATA
        // =========================================================

        [Serializable]
        public class ActionData
        {
            [Title("Time")]
            [OdinSerialize, ShowInInspector]
            private ICachedScore m_StartTimeCalculator;

            [SerializeField]
            private WhenToCalculateTimelineValues m_WhenToCalculateStartTime;


            [OdinSerialize, ShowInInspector]
            private ICachedScore m_DurationCalculator;

            [SerializeField]
            private WhenToCalculateTimelineValues m_WhenToCalculateDuration;


            [FoldoutGroup("Time Debug at Runtime")]
            [ReadOnly]
            private float m_StartTime;

            [FoldoutGroup("Time Debug at Runtime")]
            [ReadOnly]
            private float m_EndTime;

            [FoldoutGroup("Time Debug at Runtime")]
            [ReadOnly]
            private float m_Duration;

            // The values produced by calculators / explicit configuration, before
            // they are clipped to the containing timeline's duration.
            [NonSerialized]
            private float m_RequestedStartTime;
            [NonSerialized]
            private float m_RequestedDuration;


            [Title("Actions")]
            [OdinSerialize, ShowInInspector]
            private List<IAction> m_Actions = new List<IAction>();


            private bool m_UseExplicitTiming;

            // TimelineCreator's free slots are fractions of the parent duration.
            // The parent duration may only become known after SetGameObject(),
            // and can also change on each StartAction().
            private bool m_UseDistributedTiming;
            private int m_DistributedSlotIndex;
            private int m_DistributedSlotCount;

            public float StartTime => m_StartTime;
            public float EndTime => m_EndTime;
            public float Duration => m_Duration;
            public float RequestedDuration => m_RequestedDuration;
            public float RequestedEndTime => m_RequestedStartTime + m_RequestedDuration;


            public ActionData()
            {
            }

            public ActionData(ActionData other)
            {
                m_StartTime = other.m_StartTime;
                m_EndTime = other.m_EndTime;
                m_Duration = other.m_Duration;
                m_RequestedStartTime = other.m_RequestedStartTime;
                m_RequestedDuration = other.m_RequestedDuration;
                m_UseExplicitTiming = other.m_UseExplicitTiming;
                m_UseDistributedTiming = other.m_UseDistributedTiming;
                m_DistributedSlotIndex = other.m_DistributedSlotIndex;
                m_DistributedSlotCount = other.m_DistributedSlotCount;

                m_WhenToCalculateStartTime =
                    other.m_WhenToCalculateStartTime;

                m_WhenToCalculateDuration =
                    other.m_WhenToCalculateDuration;


                if (other.m_StartTimeCalculator != null)
                {
                    m_StartTimeCalculator =
                        other.m_StartTimeCalculator.Clone();
                }

                if (other.m_DurationCalculator != null)
                {
                    m_DurationCalculator =
                        other.m_DurationCalculator.Clone();
                }


                if (other.m_Actions != null)
                {
                    m_Actions =
                        new List<IAction>(other.m_Actions.Count);

                    for (int i = 0; i < other.m_Actions.Count; i++)
                    {
                        IAction action = other.m_Actions[i];

                        m_Actions.Add(
                            action != null
                                ? action.Clone()
                                : null);
                    }
                }
                else
                {
                    m_Actions = null;
                }
            }


            // =====================================================
            // TIME
            // =====================================================

            public void SetStartTime(float startTime)
            {
                m_StartTime = Mathf.Max(0f, startTime);
                m_RequestedStartTime = m_StartTime;

                m_EndTime =
                    m_StartTime + m_Duration;
            }

            public void SetDuration(float duration)
            {
                m_Duration = Mathf.Max(0f, duration);
                m_RequestedDuration = m_Duration;

                m_EndTime =
                    m_StartTime + m_Duration;
            }

            public void SetEndTime(float endTime)
            {
                m_EndTime =
                    Mathf.Max(m_StartTime, endTime);

                m_Duration = m_EndTime - m_StartTime;
                m_RequestedDuration = m_Duration;
            }


            /// <summary>
            /// Evaluates calculators whose calculation mode matches
            /// calculationMode.
            /// </summary>
            public void ConfigureExplicit(float start, float duration, IAction action)
            {
                m_UseExplicitTiming = true;
                m_UseDistributedTiming = false;
                SetStartTime(start);
                SetDuration(duration);
                m_Actions = new List<IAction> { action };
            }

            /// <summary>
            /// Defers creator-generated free-slot timing until the containing
            /// timeline's sequential duration has actually been calculated.
            /// </summary>
            public void ConfigureDistributed(int slotIndex, int slotCount, IAction action)
            {
                if (slotCount <= 0 || slotIndex < 0 || slotIndex >= slotCount)
                    throw new ArgumentOutOfRangeException(nameof(slotIndex));

                m_UseExplicitTiming = true;
                m_UseDistributedTiming = true;
                m_DistributedSlotIndex = slotIndex;
                m_DistributedSlotCount = slotCount;
                m_Actions = new List<IAction> { action };
                SetStartTime(0f);
                SetDuration(0f);
            }

            internal void RefreshDistributedTiming(float timelineDuration)
            {
                if (!m_UseDistributedTiming || m_DistributedSlotCount <= 0)
                    return;

                // Calculate boundaries independently to prevent accumulated
                // rounding error from leaving a gap at the end of the timeline.
                float start = timelineDuration *
                    ((float)m_DistributedSlotIndex / m_DistributedSlotCount);
                float end = timelineDuration *
                    ((float)(m_DistributedSlotIndex + 1) / m_DistributedSlotCount);

                SetStartTime(start);
                SetDuration(Mathf.Max(0f, end - start));
            }

            public void CalculateTimeValues(
                WhenToCalculateTimelineValues calculationMode)
            {
                if (m_UseExplicitTiming) return;
                if (m_WhenToCalculateStartTime == calculationMode &&
                    m_StartTimeCalculator != null)
                {
                    m_RequestedStartTime =
                        Mathf.Max(
                            0f,
                            m_StartTimeCalculator.CalculateScore());
                }

                if (m_WhenToCalculateDuration == calculationMode &&
                    m_DurationCalculator != null)
                {
                    m_RequestedDuration =
                        Mathf.Max(
                            0f,
                            m_DurationCalculator.CalculateScore());
                }

                m_StartTime = m_RequestedStartTime;
                m_Duration = m_RequestedDuration;
                m_EndTime = m_StartTime + m_Duration;
            }

            // Nested timelines in a free segment run simultaneously. When no
            // duration calculator is supplied, infer the duration from the
            // longest nested timeline (not the sum of their durations).
            private bool HasNestedTimeline()
            {
                if (m_Actions == null) return false;

                for (int i = 0; i < m_Actions.Count; i++)
                {
                    if (m_Actions[i] is ActionTimeline)
                        return true;
                }

                return false;
            }

            internal void PrepareNestedTimelinesForDuration()
            {
                if (m_UseExplicitTiming || m_DurationCalculator != null ||
                    m_Actions == null)
                    return;

                for (int i = 0; i < m_Actions.Count; i++)
                {
                    if (m_Actions[i] is ActionTimeline timeline)
                        timeline.PrepareForStart();
                }
            }

            internal void RefreshDurationFromNestedTimelines()
            {
                if (m_UseExplicitTiming || m_DurationCalculator != null ||
                    m_Actions == null)
                    return;

                bool foundTimeline = false;
                float longestDuration = 0f;

                for (int i = 0; i < m_Actions.Count; i++)
                {
                    if (m_Actions[i] is ActionTimeline timeline)
                    {
                        foundTimeline = true;
                        longestDuration = Mathf.Max(longestDuration, timeline.Duration);
                    }
                }

                if (foundTimeline)
                    SetDuration(longestDuration);
            }

            internal void InvalidateNestedTimelinePreparation()
            {
                if (m_Actions == null) return;

                for (int i = 0; i < m_Actions.Count; i++)
                {
                    if (m_Actions[i] is ActionTimeline timeline)
                        timeline.InvalidatePreparedStartValues();
                }
            }


            /// <summary>
            /// Clamps the already calculated time interval so it cannot
            /// exceed the timeline.
            /// </summary>
            public void ClampToTimeline(float timelineDuration)
            {
                timelineDuration =
                    Mathf.Max(0f, timelineDuration);

                m_StartTime =
                    Mathf.Clamp(
                        m_RequestedStartTime,
                        0f,
                        timelineDuration);

                m_EndTime =
                    Mathf.Clamp(
                        m_RequestedStartTime + m_RequestedDuration,
                        m_StartTime,
                        timelineDuration);

                m_Duration =
                    m_EndTime - m_StartTime;
            }


            // =====================================================
            // INITIALIZATION
            // =====================================================

            public bool SetGameObject(GameObject gameObj)
            {
                if (gameObj == null)
                {
                    Debug.LogError(
                        $"{nameof(ActionData)}: " +
                        $"Cannot set a null GameObject.");

                    return false;
                }

                bool allGood = true;


                // ----------------------------
                // Start Time Calculator
                // ----------------------------

                if (m_StartTimeCalculator == null && !m_UseExplicitTiming)
                {
                    Debug.LogError(
                        $"{nameof(ActionData)}: " +
                        $"{nameof(m_StartTimeCalculator)} is null.");

                    allGood = false;
                }
                else
                {
                    bool calculatorGood = m_UseExplicitTiming ||
                        m_StartTimeCalculator.SetGameObject(gameObj);

                    allGood &= calculatorGood;

                    if (calculatorGood && !m_UseExplicitTiming &&
                        m_WhenToCalculateStartTime ==
                        WhenToCalculateTimelineValues
                            .OnceWhenSettingTheGameObject)
                    {
                        m_RequestedStartTime =
                            Mathf.Max(
                                0f,
                                m_StartTimeCalculator.CalculateScore());
                    }
                }


                // ----------------------------
                // Duration Calculator
                // ----------------------------

                if (m_DurationCalculator == null && !m_UseExplicitTiming)
                {
                    if (!HasNestedTimeline())
                    {
                        Debug.LogError(
                            $"{nameof(ActionData)}: " +
                            $"{nameof(m_DurationCalculator)} is null and no nested " +
                            $"{nameof(ActionTimeline)} provides a duration.");

                        allGood = false;
                    }
                }
                else
                {
                    bool calculatorGood = m_UseExplicitTiming ||
                        m_DurationCalculator.SetGameObject(gameObj);

                    allGood &= calculatorGood;

                    if (calculatorGood && !m_UseExplicitTiming &&
                        m_WhenToCalculateDuration ==
                        WhenToCalculateTimelineValues
                            .OnceWhenSettingTheGameObject)
                    {
                        m_RequestedDuration =
                            Mathf.Max(
                                0f,
                                m_DurationCalculator.CalculateScore());
                    }
                }


                m_StartTime = m_RequestedStartTime;
                m_Duration = m_RequestedDuration;
                m_EndTime = m_StartTime + m_Duration;


                // ----------------------------
                // Actions
                // ----------------------------

                if (m_Actions == null)
                {
                    Debug.LogError(
                        $"{nameof(ActionData)}: " +
                        $"{nameof(m_Actions)} is null.");

                    return false;
                }

                for (int i = 0; i < m_Actions.Count; i++)
                {
                    IAction action = m_Actions[i];

                    if (action == null)
                    {
                        Debug.LogError(
                            $"{nameof(ActionData)} contains " +
                            $"a null action at index {i}.");

                        allGood = false;
                        continue;
                    }

                    allGood &=
                        action.SetGameObject(gameObj);
                }

                return allGood;
            }


            // =====================================================
            // ACTION LIFECYCLE
            // =====================================================

            public ActionData Clone()
            {
                return new ActionData(this);
            }


            public void Start()
            {
                if (m_Actions == null)
                {
                    return;
                }

                for (int i = 0; i < m_Actions.Count; i++)
                {
                    m_Actions[i]?.StartAction();
                }
            }

            public void Update()
            {
                if (m_Actions == null)
                {
                    return;
                }

                for (int i = 0; i < m_Actions.Count; i++)
                {
                    m_Actions[i]?.UpdateAction();
                }
            }

            public void End()
            {
                if (m_Actions == null)
                {
                    return;
                }

                for (int i = 0; i < m_Actions.Count; i++)
                {
                    m_Actions[i]?.EndAction();
                }
            }
        }


        // =========================================================
        // TIMELINE DATA
        // =========================================================

        [Title("Duration Config")]
        [SerializeField, Tooltip(
            "Set Duration In Timeline uses the manually configured duration. " +
            "Calculate Duration From Timeline Sequence Data sums the requested sequence durations. " +
            "Calculate Duration From Free Action Data uses the latest requested free-action end time (start + duration).")]
        private HowToCalculateTimelineDuration m_HowToCalculateTimelineDuration;

        [SerializeField, Min(0), ShowIf(nameof(m_HowToCalculateTimelineDuration), HowToCalculateTimelineDuration.SetDurationInTimeline)]
        private float m_Duration;


        [Title("Sequence Config")]
        [SerializeField]
        private List<ActionSequenceData> m_SequenceData =
            new List<ActionSequenceData>();


        [Title("Free Actions Config")]
        [SerializeField]
        private List<ActionData> m_FreeActionData =
            new List<ActionData>();

        private UnityEvent m_OnTimelineStart;
        private UnityEvent m_OnTimelineEnd;

        public void SubscribeOnStart(UnityAction unityAction)
        {
            m_OnTimelineStart ??= new();
            m_OnTimelineStart.AddListener(unityAction);
        }

        public void SubscribeOnEnd(UnityAction unityAction)
        {

            m_OnTimelineEnd ??= new();
            m_OnTimelineEnd.AddListener(unityAction);
        }

        public void UnsubscribeOnStart(UnityAction unityAction)
        {
            m_OnTimelineStart ??= new();
            m_OnTimelineStart.RemoveListener(unityAction);
        }

        public void UnsubscribeOnEnd(UnityAction unityAction)
        {

            m_OnTimelineEnd ??= new();
            m_OnTimelineEnd.RemoveListener(unityAction);
        }

        private bool m_CalculatedDurationAtLeastOnce = false;
        public float Duration { get
            {
                if (!m_CalculatedDurationAtLeastOnce)
                {
                    RecalculateTimelineDuration();
                }

                return m_Duration;

            } }

        public bool IsRunning => m_IsRunning;

        public HowToCalculateTimelineDuration DurationCalculationMode =>
            m_HowToCalculateTimelineDuration;

        public void SetDurationCalculationMode(HowToCalculateTimelineDuration mode)
        {
            m_HowToCalculateTimelineDuration = mode;
            RecalculateTimelineData();
        }

        public void AddSequentialTimeline(ActionTimeline timeline)
        {
            if (timeline == null) return;
            var segment = new ActionSequenceData();
            segment.ConfigureFromNestedTimeline(timeline);
            m_SequenceData ??= new List<ActionSequenceData>();
            m_SequenceData.Add(segment);

            // Before SetGameObject this may only be a provisional duration.
            // The nested timing is re-read after binding and on each start.
            RecalculateTimelineData();
        }

        public void AddFreeTimeline(ActionTimeline timeline, float start, float duration)
        {
            if (timeline == null) return;
            var segment = new ActionData();
            segment.ConfigureExplicit(start, duration, timeline);
            m_FreeActionData ??= new List<ActionData>();
            m_FreeActionData.Add(segment);
            RecalculateTimelineData();
        }

        /// <summary>
        /// Adds a free timeline to one of N equal slots. Unlike AddFreeTimeline,
        /// this does not capture the parent duration before SetGameObject().
        /// Used by ActionTimelineCreatorScriptableObject.
        /// </summary>
        public void AddDistributedFreeTimeline(ActionTimeline timeline, int slotIndex, int slotCount)
        {
            if (timeline == null) return;
            var segment = new ActionData();
            segment.ConfigureDistributed(slotIndex, slotCount, timeline);
            m_FreeActionData ??= new List<ActionData>();
            m_FreeActionData.Add(segment);
            RecalculateTimelineData();
        }

        // =========================================================
        // RUNTIME DATA
        // =========================================================

        private GameObject m_Self;

        private float m_CurrentTime;

        private bool m_IsRunning;

        // A parent can need a nested timeline's duration BEFORE that nested
        // timeline actually starts. This caches the preparation so the nested
        // timeline does not re-evaluate its per-start scores a second time.
        [NonSerialized]
        private bool m_HasPreparedStartValues;

        [NonSerialized]
        private bool m_IsPreparingStartValues;

        private int m_CurrentSequenceIndex = -1;

        private bool[] m_ActionDataActive;


        // =========================================================
        // CONSTRUCTORS
        // =========================================================

        public ActionTimeline()
        {
        }

        public ActionTimeline(ActionTimeline other)
        {
            m_Duration = other.m_Duration;
            m_HowToCalculateTimelineDuration = other.m_HowToCalculateTimelineDuration;


            // ----------------------------
            // Sequence Data
            // ----------------------------

            if (other.m_SequenceData != null)
            {
                m_SequenceData =
                    new List<ActionSequenceData>(
                        other.m_SequenceData.Count);

                for (int i = 0;
                     i < other.m_SequenceData.Count;
                     i++)
                {
                    ActionSequenceData data =
                        other.m_SequenceData[i];

                    m_SequenceData.Add(
                        data != null
                            ? data.Clone()
                            : null);
                }
            }
            else
            {
                m_SequenceData = null;
            }


            // ----------------------------
            // Independent Action Data
            // ----------------------------

            if (other.m_FreeActionData != null)
            {
                m_FreeActionData =
                    new List<ActionData>(
                        other.m_FreeActionData.Count);

                for (int i = 0;
                     i < other.m_FreeActionData.Count;
                     i++)
                {
                    ActionData data =
                        other.m_FreeActionData[i];

                    m_FreeActionData.Add(
                        data != null
                            ? data.Clone()
                            : null);
                }
            }
            else
            {
                m_FreeActionData = null;
            }


            // Runtime state intentionally does not get cloned.
            m_Self = null;

            m_CurrentTime = 0f;

            m_IsRunning = false;
            m_HasPreparedStartValues = false;
            m_IsPreparingStartValues = false;

            m_CurrentSequenceIndex = -1;

            m_ActionDataActive =
                m_FreeActionData != null
                    ? new bool[m_FreeActionData.Count]
                    : Array.Empty<bool>();
        }


        // =========================================================
        // TIMELINE VALUE CALCULATION
        // =========================================================

        /// <summary>
        /// Prepares timing without executing actions or invoking timeline events.
        /// This lets parent timelines derive their own duration from nested
        /// inspector-assigned timelines, including nested per-start scores.
        /// </summary>
        private void PrepareForStart()
        {
            if (m_HasPreparedStartValues)
                return;

            if (m_IsPreparingStartValues)
            {
                Debug.LogError($"{nameof(ActionTimeline)}: Cyclic nested timeline reference detected.");
                return;
            }

            m_IsPreparingStartValues = true;
            try
            {
                // Only prepare children whose duration is needed by a segment.
                // Children with an explicit duration/score will prepare normally
                // when their own StartAction() is called.
                if (m_SequenceData != null)
                {
                    for (int i = 0; i < m_SequenceData.Count; i++)
                        m_SequenceData[i]?.PrepareNestedTimelinesForDuration();
                }

                if (m_FreeActionData != null)
                {
                    for (int i = 0; i < m_FreeActionData.Count; i++)
                        m_FreeActionData[i]?.PrepareNestedTimelinesForDuration();
                }

                CalculateValuesOnTimelineStart();
                RecalculateTimelineData();
                m_HasPreparedStartValues = true;
            }
            finally
            {
                m_IsPreparingStartValues = false;
            }
        }

        /// <summary>
        /// Discards a prepared but unconsumed nested start (e.g. when its
        /// parent ends before this segment is reached).
        /// </summary>
        private void InvalidatePreparedStartValues()
        {
            m_HasPreparedStartValues = false;

            if (m_SequenceData != null)
            {
                for (int i = 0; i < m_SequenceData.Count; i++)
                    m_SequenceData[i]?.InvalidateNestedTimelinePreparation();
            }

            if (m_FreeActionData != null)
            {
                for (int i = 0; i < m_FreeActionData.Count; i++)
                    m_FreeActionData[i]?.InvalidateNestedTimelinePreparation();
            }
        }

        /// <summary>
        /// Recalculates all values configured to calculate whenever
        /// the timeline starts.
        /// </summary>
        private void CalculateValuesOnTimelineStart()
        {
            if (m_SequenceData != null)
            {
                for (int i = 0;
                     i < m_SequenceData.Count;
                     i++)
                {
                    ActionSequenceData data =
                        m_SequenceData[i];

                    if (data == null)
                    {
                        continue;
                    }

                    data.CalculateDuration(
                        WhenToCalculateTimelineValues
                            .EverytimeOnStartingTimeline);
                }
            }


            if (m_FreeActionData != null)
            {
                for (int i = 0;
                     i < m_FreeActionData.Count;
                     i++)
                {
                    ActionData data =
                        m_FreeActionData[i];

                    if (data == null)
                    {
                        continue;
                    }

                    data.CalculateTimeValues(
                        WhenToCalculateTimelineValues
                            .EverytimeOnStartingTimeline);
                }
            }
        }


        /// <summary>
        /// Recalculates values whose final runtime values depend on
        /// other timeline data.
        ///
        /// Does NOT evaluate ICachedScore calculators.
        /// </summary>
        private void RecalculateTimelineData()
        {
            // Inspector-assigned entries do not pass through AddSequentialTimeline
            // or AddFreeTimeline. Read durations from their nested timelines when
            // there is no explicit timing or duration score calculator.
            // The nested timelines are already bound/prepared for this phase.
            if (m_SequenceData != null)
            {
                for (int i = 0; i < m_SequenceData.Count; i++)
                    m_SequenceData[i]?.RefreshDurationFromNestedTimelines();
            }

            if (m_FreeActionData != null)
            {
                for (int i = 0; i < m_FreeActionData.Count; i++)
                    m_FreeActionData[i]?.RefreshDurationFromNestedTimelines();
            }

            // Derive duration before distributing or clipping entries.
            RecalculateTimelineDuration();
            RecalculateSequenceData();

            if (m_FreeActionData != null)
            {
                for (int i = 0; i < m_FreeActionData.Count; i++)
                    m_FreeActionData[i]?.RefreshDistributedTiming(m_Duration);
            }

            if (m_FreeActionData != null)
            {
                for (int i = 0;
                     i < m_FreeActionData.Count;
                     i++)
                {
                    ActionData data =
                        m_FreeActionData[i];

                    if (data != null)
                    {
                        data.ClampToTimeline(
                            m_Duration);
                    }
                }
            }

            EnsureActionRuntimeState();
        }


        /// <summary>
        /// Derives the timeline duration from the selected source. It uses
        /// requested (unclamped) times so a previous layout cannot affect the
        /// next calculation. This method never evaluates ICachedScore values.
        /// </summary>
        private void RecalculateTimelineDuration()
        {
            switch (m_HowToCalculateTimelineDuration)
            {
                case HowToCalculateTimelineDuration.SetDurationInTimeline:
                    m_Duration = Mathf.Max(0f, m_Duration);
                    break;

                case HowToCalculateTimelineDuration.CalculateDurationFromTimelineSequenceData:
                    {
                        float duration = 0f;
                        if (m_SequenceData != null)
                        {
                            foreach (ActionSequenceData data in m_SequenceData)
                            {
                                if (data != null)
                                    duration += Mathf.Max(0f, data.RequestedDuration);
                            }
                        }
                        m_Duration = duration;
                        break;
                    }

                case HowToCalculateTimelineDuration.CalculateDurationFromFreeActionData:
                    {
                        float latestEndTime = 0f;
                        if (m_FreeActionData != null)
                        {
                            foreach (ActionData data in m_FreeActionData)
                            {
                                if (data != null)
                                    latestEndTime = Mathf.Max(latestEndTime, data.RequestedEndTime);
                            }
                        }
                        m_Duration = latestEndTime;
                        break;
                    }

                default:
                    Debug.LogError($"Unknown timeline duration calculation mode: {m_HowToCalculateTimelineDuration}.");
                    m_Duration = Mathf.Max(0f, m_Duration);
                    break;
            }

            m_CalculatedDurationAtLeastOnce = true;
        }


        /// <summary>
        /// Sequence entries are automatically placed consecutively.
        /// </summary>
        private void RecalculateSequenceData()
        {
            if (m_SequenceData == null)
            {
                return;
            }

            float currentStartTime = 0f;

            for (int i = 0;
                 i < m_SequenceData.Count;
                 i++)
            {
                ActionSequenceData data =
                    m_SequenceData[i];

                if (data == null)
                {
                    continue;
                }

                float remainingDuration =
                    Mathf.Max(
                        0f,
                        m_Duration - currentStartTime);

                float clampedDuration =
                    Mathf.Min(
                        Mathf.Max(
                            0f,
                            data.RequestedDuration),
                        remainingDuration);

                data.SetStartTime(
                    currentStartTime);

                data.SetClampedDuration(
                    clampedDuration);

                currentStartTime =
                    data.EndTime;

                if (currentStartTime >= m_Duration)
                {
                    currentStartTime =
                        m_Duration;
                }
            }
        }


        private void EnsureActionRuntimeState()
        {
            int requiredLength =
                m_FreeActionData?.Count ?? 0;

            if (m_ActionDataActive == null ||
                m_ActionDataActive.Length != requiredLength)
            {
                m_ActionDataActive =
                    requiredLength > 0
                        ? new bool[requiredLength]
                        : Array.Empty<bool>();
            }
        }


        // =========================================================
        // ACTION LIFECYCLE
        // =========================================================

        public void StartAction()
        {
            if (m_IsRunning)
            {
                EndAction();
            }


            // Precompute nested durations before deriving this timeline's
            // duration. If this timeline was precomputed by its parent, reuse
            // that result instead of evaluating per-start scores twice.
            PrepareForStart();
            m_HasPreparedStartValues = false;


            m_CurrentTime = 0f;

            m_CurrentSequenceIndex = -1;

            m_IsRunning = true;


            if (m_ActionDataActive.Length > 0)
            {
                Array.Clear(
                    m_ActionDataActive,
                    0,
                    m_ActionDataActive.Length);
            }


            // Notify listeners after runtime state is initialized,
            // but before starting any actions scheduled at time zero.
            m_OnTimelineStart?.Invoke();

            // A start listener may stop the timeline.
            if (!m_IsRunning)
            {
                return;
            }

            /*
             * Process time zero immediately so actions whose start
             * time is zero begin immediately.
             */
            ProcessTimeline();
        }


        public void UpdateAction()
        {
            if (!m_IsRunning)
            {
                return;
            }

            if (m_Duration <= 0f)
            {
                EndAction();
                return;
            }

            m_CurrentTime += Time.deltaTime;

            if (m_CurrentTime >= m_Duration)
            {
                m_CurrentTime =
                    m_Duration;

                ProcessTimeline();

                EndAction();

                return;
            }

            ProcessTimeline();
        }


        public void EndAction()
        {
            if (!m_IsRunning)
            {
                return;
            }


            // ----------------------------
            // Sequence
            // ----------------------------

            if (m_CurrentSequenceIndex >= 0 &&
                m_SequenceData != null &&
                m_CurrentSequenceIndex <
                m_SequenceData.Count)
            {
                m_SequenceData[
                    m_CurrentSequenceIndex]?.End();
            }

            m_CurrentSequenceIndex = -1;


            // ----------------------------
            // Independent Actions
            // ----------------------------

            if (m_FreeActionData != null &&
                m_ActionDataActive != null)
            {
                int count =
                    Mathf.Min(
                        m_FreeActionData.Count,
                        m_ActionDataActive.Length);

                for (int i = 0; i < count; i++)
                {
                    if (!m_ActionDataActive[i])
                    {
                        continue;
                    }

                    m_FreeActionData[i]?.End();

                    m_ActionDataActive[i] = false;
                }
            }


            m_IsRunning = false;

            // Any nested timeline that was only prepared (but never reached)
            // must calculate afresh if it is started independently later.
            InvalidatePreparedStartValues();

            // Notify listeners only after every active action has ended
            // and the timeline is no longer running.
            m_OnTimelineEnd?.Invoke();
        }


        // =========================================================
        // PROCESS TIMELINE
        // =========================================================

        private void ProcessTimeline()
        {
            ProcessSequenceData();
            ProcessActionData();
        }


        // =========================================================
        // SEQUENCE PROCESSING
        // =========================================================

        private void ProcessSequenceData()
        {
            if (m_SequenceData == null ||
                m_SequenceData.Count == 0)
            {
                return;
            }

            int sequenceIndex =
                FindSequenceAtTime(
                    m_CurrentTime);


            if (sequenceIndex !=
                m_CurrentSequenceIndex)
            {
                // ------------------------
                // End Previous
                // ------------------------

                if (m_CurrentSequenceIndex >= 0 &&
                    m_CurrentSequenceIndex <
                    m_SequenceData.Count)
                {
                    m_SequenceData[
                        m_CurrentSequenceIndex]?.End();
                }


                m_CurrentSequenceIndex =
                    sequenceIndex;


                // ------------------------
                // Start New
                // ------------------------

                if (m_CurrentSequenceIndex >= 0)
                {
                    m_SequenceData[
                        m_CurrentSequenceIndex]?.Start();
                }
            }


            // ----------------------------
            // Update Current
            // ----------------------------

            if (m_CurrentSequenceIndex >= 0)
            {
                m_SequenceData[
                    m_CurrentSequenceIndex]?.Update();
            }
        }


        private int FindSequenceAtTime(float time)
        {
            if (m_SequenceData == null)
            {
                return -1;
            }


            // ----------------------------
            // Check Current First
            // ----------------------------

            if (m_CurrentSequenceIndex >= 0 &&
                m_CurrentSequenceIndex <
                m_SequenceData.Count)
            {
                ActionSequenceData current =
                    m_SequenceData[
                        m_CurrentSequenceIndex];

                if (IsTimeInsideSequence(
                    current,
                    time))
                {
                    return m_CurrentSequenceIndex;
                }


                // Timeline time normally only moves forward,
                // so search forward from the current entry.
                for (int i =
                         m_CurrentSequenceIndex + 1;
                     i < m_SequenceData.Count;
                     i++)
                {
                    ActionSequenceData data =
                        m_SequenceData[i];

                    if (data == null ||
                        data.Duration <= 0f)
                    {
                        continue;
                    }

                    if (time < data.StartTime)
                    {
                        return -1;
                    }

                    if (IsTimeInsideSequence(
                        data,
                        time))
                    {
                        return i;
                    }
                }

                return -1;
            }


            // ----------------------------
            // Initial Search
            // ----------------------------

            for (int i = 0;
                 i < m_SequenceData.Count;
                 i++)
            {
                ActionSequenceData data =
                    m_SequenceData[i];

                if (data == null ||
                    data.Duration <= 0f)
                {
                    continue;
                }

                if (time < data.StartTime)
                {
                    return -1;
                }

                if (IsTimeInsideSequence(
                    data,
                    time))
                {
                    return i;
                }
            }

            return -1;
        }


        private static bool IsTimeInsideSequence(
            ActionSequenceData data,
            float time)
        {
            return data != null &&
                   data.Duration > 0f &&
                   time >= data.StartTime &&
                   time < data.EndTime;
        }


        // =========================================================
        // INDEPENDENT ACTION PROCESSING
        // =========================================================

        private void ProcessActionData()
        {
            if (m_FreeActionData == null ||
                m_ActionDataActive == null)
            {
                return;
            }

            int count =
                Mathf.Min(
                    m_FreeActionData.Count,
                    m_ActionDataActive.Length);


            for (int i = 0; i < count; i++)
            {
                ActionData data =
                    m_FreeActionData[i];

                if (data == null)
                {
                    continue;
                }

                bool isActive =
                    m_ActionDataActive[i];

                bool shouldBeActive =
                    data.Duration > 0f &&
                    m_CurrentTime >= data.StartTime &&
                    m_CurrentTime < data.EndTime;


                // ----------------------------
                // Active
                // ----------------------------

                if (shouldBeActive)
                {
                    if (!isActive)
                    {
                        data.Start();

                        m_ActionDataActive[i] =
                            true;
                    }

                    data.Update();

                    continue;
                }


                // ----------------------------
                // End
                // ----------------------------

                if (isActive)
                {
                    data.End();

                    m_ActionDataActive[i] =
                        false;
                }
            }
        }


        // =========================================================
        // SET GAMEOBJECT
        // =========================================================

        public bool SetGameObject(GameObject gameObj)
        {
            if (gameObj == null)
            {
                Debug.LogError(
                    $"{nameof(ActionTimeline)}: " +
                    $"Cannot set a null GameObject.");

                return false;
            }

            m_Self = gameObj;
            InvalidatePreparedStartValues();

            bool allGood = true;


            // ----------------------------
            // Sequence Data
            // ----------------------------

            if (m_SequenceData != null)
            {
                for (int i = 0;
                     i < m_SequenceData.Count;
                     i++)
                {
                    ActionSequenceData data =
                        m_SequenceData[i];

                    if (data == null)
                    {
                        Debug.LogError(
                            $"{nameof(ActionTimeline)} contains a null " +
                            $"{nameof(ActionSequenceData)} at index {i}.");

                        allGood = false;
                        continue;
                    }

                    allGood &=
                        data.SetGameObject(gameObj);
                }
            }


            // ----------------------------
            // Independent Actions
            // ----------------------------

            if (m_FreeActionData != null)
            {
                for (int i = 0;
                     i < m_FreeActionData.Count;
                     i++)
                {
                    ActionData data =
                        m_FreeActionData[i];

                    if (data == null)
                    {
                        Debug.LogError(
                            $"{nameof(ActionTimeline)} contains a null " +
                            $"{nameof(ActionData)} at index {i}.");

                        allGood = false;
                        continue;
                    }

                    allGood &=
                        data.SetGameObject(gameObj);
                }
            }


            /*
             * Values configured as OnceWhenSettingTheGameObject have
             * now been calculated.
             *
             * Values configured as EverytimeOnStartingTimeline may
             * still contain their previous/default values. That's OK:
             * this calculation establishes valid initial runtime
             * structure and StartAction() recalculates it before use.
             */
            RecalculateTimelineData();

            return allGood;
        }


        public GameObject GetGameObject()
        {
            return m_Self;
        }


        public IAction Clone()
        {
            return new ActionTimeline(this);
        }
    }
}