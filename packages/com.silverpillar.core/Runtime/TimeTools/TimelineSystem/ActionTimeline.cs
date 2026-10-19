using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Core
{
    public enum WhenToCalculateTimelineValues
    {
        OnceWhenSettingTheGameObject,
        EverytimeOnStartingTimeline
    }

    public enum HowToCalculateTimelineDuration
    {
        SetDurationInTimeline,
        CalculateDurationFromTimelineSequenceData,
        CalculateDurationFromFreeActionData
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

                if (m_DurationCalculator == null && !m_UseExplicitTiming)
                {
                    Debug.LogError(
                        $"{nameof(ActionSequenceData)}: " +
                        $"{nameof(m_DurationCalculator)} is null.");

                    allGood = false;
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
                SetStartTime(start);
                SetDuration(duration);
                m_Actions = new List<IAction> { action };
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
                    Debug.LogError(
                        $"{nameof(ActionData)}: " +
                        $"{nameof(m_DurationCalculator)} is null.");

                    allGood = false;
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


        public float Duration => m_Duration;

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
            segment.ConfigureExplicit(timeline.Duration, timeline);
            m_SequenceData.Add(segment);

            // Preserve the original programmatic composition behavior when
            // the duration is set explicitly. In calculated modes, derive it.
            if (m_HowToCalculateTimelineDuration ==
                HowToCalculateTimelineDuration.SetDurationInTimeline)
            {
                m_Duration += timeline.Duration;
            }
            else
            {
                RecalculateTimelineDuration();
            }
        }

        public void AddFreeTimeline(ActionTimeline timeline, float start, float duration)
        {
            if (timeline == null) return;
            var segment = new ActionData();
            segment.ConfigureExplicit(start, duration, timeline);
            m_FreeActionData.Add(segment);
            if (m_HowToCalculateTimelineDuration !=
                HowToCalculateTimelineDuration.SetDurationInTimeline)
            {
                RecalculateTimelineDuration();
            }
        }

        // =========================================================
        // RUNTIME DATA
        // =========================================================

        private GameObject m_Self;

        private float m_CurrentTime;

        private bool m_IsRunning;

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
            // All score calculators have already been evaluated for this phase.
            // Derive duration before clipping entries to the timeline.
            RecalculateTimelineDuration();
            RecalculateSequenceData();

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


            /*
             * Calculate all values configured to change every time
             * the timeline starts.
             *
             * This happens BEFORE arranging sequence entries and
             * clamping independent actions.
             */
            CalculateValuesOnTimelineStart();

            RecalculateTimelineData();


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