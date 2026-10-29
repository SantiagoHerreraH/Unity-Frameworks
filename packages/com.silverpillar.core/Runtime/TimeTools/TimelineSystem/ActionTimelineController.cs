using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SilverPillar.Core
{
    public enum WhenToStartTimeline
    {
        DontStartAutomatically,
        OnAwake,
        OnStart,
        OnEnable
    }

    public enum WhenToStopTimeline
    {
        DontStopAutomatically,
        OnDisable
    }

    public class ActionTimelineController : SerializedMonoBehaviour
    {
        [Serializable]
        public class ActionTimelineScriptableObjectData
        {
            [Title("Settings")]
            public WhenToStartTimeline WhenToStartTimeline;
            public WhenToStopTimeline WhenToStopTimeline;

            [Title("Name")]
            public TimerName TimelineName;

            [Title("Data")]
            public ActionTimelineCreatorScriptableObject.SegmentType ActionTimelineType;
            [SerializeField, ShowIf(nameof(ActionTimelineType), ActionTimelineCreatorScriptableObject.SegmentType.ActionTimeline)]
            [PropertySpace(SpaceAfter = 10)]
            private ActionTimelineScriptableObject m_ActionTimeline;
            [SerializeField, ShowIf(nameof(ActionTimelineType), ActionTimelineCreatorScriptableObject.SegmentType.ActionTimelineCreator)]
            [PropertySpace(SpaceAfter = 10)]
            private ActionTimelineCreatorScriptableObject m_ActionTimelineCreatorScriptableObject;

            [Title("Events")]
            [SerializeField]
            private UnityEvent m_OnStart;
            [SerializeField]
            private UnityEvent m_OnEnd;

            public ActionTimeline Clone()
            {
                ActionTimeline actionTimeline = null;
                switch (ActionTimelineType)
                {
                    case ActionTimelineCreatorScriptableObject.SegmentType.ActionTimeline:
                        if (m_ActionTimeline != null)
                        {
                            actionTimeline = m_ActionTimeline.Clone();
                        }
                        break;
                    case ActionTimelineCreatorScriptableObject.SegmentType.ActionTimelineCreator:
                        if (m_ActionTimelineCreatorScriptableObject != null)
                        {
                            actionTimeline = m_ActionTimelineCreatorScriptableObject.Clone();
                        }
                        break;
                    default:
                        break;
                }

                if (actionTimeline != null)
                {
                    if (m_OnStart != null)
                    {
                        actionTimeline.SubscribeOnStart(() => m_OnStart?.Invoke()); // if this object is destroyed before timeline, want to prevent accessing null data
                    }
                    if (m_OnEnd != null)
                    {
                        actionTimeline.SubscribeOnEnd(() => m_OnEnd?.Invoke()); // if this object is destroyed before timeline, want to prevent accessing null data
                    }
                }

                return actionTimeline;
            }
        }

        [Serializable]
        public class ActionTimelineData
        {
            public WhenToStartTimeline WhenToStartTimeline;
            public WhenToStopTimeline WhenToStopTimeline;
            public TimerName TimelineName;

            [OdinSerialize]
            public ActionTimeline ActionTimeline;
        }

        private class RuntimeTimelineData
        {
            public WhenToStartTimeline WhenToStartTimeline;
            public WhenToStopTimeline WhenToStopTimeline;
            public TimerName TimelineName;
            public ActionTimeline ActionTimeline;

            public bool IsStarted;
            public bool IsPlaying;

            /// <summary>
            /// Current position inside m_AllTimelineActions.
            /// Allows O(1) reordering when activating/deactivating.
            /// </summary>
            public int ListIndex;
        }


        [Title("Scriptable Object")]
        [OdinSerialize, ShowInInspector]
        private List<ActionTimelineScriptableObjectData> m_ScriptableTimeLineActions =
            new List<ActionTimelineScriptableObjectData>();

        [Title("Non Scriptable Object")]
        [OdinSerialize, ShowInInspector]
        private List<ActionTimelineData> m_TimeLineActions =
            new List<ActionTimelineData>();


        /// <summary>
        /// [0, m_ActiveTimelineCount) = currently playing timelines.
        /// [m_ActiveTimelineCount, Count) = paused/stopped timelines.
        /// </summary>
        private readonly List<RuntimeTimelineData> m_AllTimelineActions =
            new List<RuntimeTimelineData>();

        private readonly Dictionary<TimerName, RuntimeTimelineData> m_TimelineLookup =
            new Dictionary<TimerName, RuntimeTimelineData>();

        private int m_ActiveTimelineCount;

        private bool m_Initialized;


        private void Awake()
        {
            Initialize();

            StartAutomaticTimelines(WhenToStartTimeline.OnAwake);
        }

        private void Start()
        {
            StartAutomaticTimelines(WhenToStartTimeline.OnStart);
        }

        private void OnEnable()
        {
            Initialize();

            StartAutomaticTimelines(WhenToStartTimeline.OnEnable);
        }

        private void Update()
        {
            int i = 0;
            while (i < m_ActiveTimelineCount)
            {
                RuntimeTimelineData data = m_AllTimelineActions[i];
                data.ActionTimeline.UpdateAction();

                // A timeline that reaches its calculated duration ends itself.
                // Keep controller state in sync instead of leaving a completed
                // timeline permanently inside the active region.
                if (data.IsStarted && data.IsPlaying &&
                    !data.ActionTimeline.IsRunning)
                {
                    data.IsStarted = false;
                    data.IsPlaying = false;
                    MoveToInactiveRegion(data);
                }

                // An end callback can remove/reorder timelines. When this
                // index now contains a different active timeline, process it.
                if (i < m_ActiveTimelineCount &&
                    ReferenceEquals(m_AllTimelineActions[i], data))
                    i++;
            }
        }

        private void OnDisable()
        {
            if (!m_Initialized)
            {
                return;
            }

            /*
             * Don't directly iterate while calling StopTimeline(),
             * because stopping a timeline changes its position inside
             * m_AllTimelineActions.
             *
             * We iterate backwards through the inactive region first,
             * then carefully process active timelines.
             */

            // Paused timelines can still technically be "started".
            for (int i = m_AllTimelineActions.Count - 1;
                 i >= m_ActiveTimelineCount;
                 i--)
            {
                RuntimeTimelineData data = m_AllTimelineActions[i];

                if (data.WhenToStopTimeline == WhenToStopTimeline.OnDisable)
                {
                    StopTimeline(data);
                }
            }

            // Stop matching active timelines.
            // If one is stopped, another active timeline is swapped into
            // its position, so we intentionally don't increment i.
            int activeIndex = 0;

            while (activeIndex < m_ActiveTimelineCount)
            {
                RuntimeTimelineData data =
                    m_AllTimelineActions[activeIndex];

                if (data.WhenToStopTimeline ==
                    WhenToStopTimeline.OnDisable)
                {
                    StopTimeline(data);
                }
                else
                {
                    activeIndex++;
                }
            }
        }

        private void OnDestroy()
        {
            if (!m_Initialized)
            {
                return;
            }

            // Stop active timelines first. StopTimeline() reduces
            // m_ActiveTimelineCount every time.
            while (m_ActiveTimelineCount > 0)
            {
                StopTimeline(m_AllTimelineActions[0]);
            }

            // Paused timelines are not in the active region but may still
            // have IsStarted == true.
            for (int i = m_AllTimelineActions.Count - 1; i >= 0; i--)
            {
                RuntimeTimelineData data = m_AllTimelineActions[i];

                if (data.IsStarted)
                {
                    StopTimeline(data);
                }
            }

            m_AllTimelineActions.Clear();
            m_TimelineLookup.Clear();

            m_ActiveTimelineCount = 0;
            m_Initialized = false;
        }

        public void SetTimeline(TimerName timelineName, ActionTimeline actionTimeline)
        {
            Initialize();

            if (timelineName == null)
            {
                Debug.LogError(
                    $"{nameof(ActionTimelineController)} on {name}: " +
                    $"Cannot set a timeline with a null {nameof(TimerName)}.",
                    this);

                return;
            }

            if (actionTimeline == null)
            {
                Debug.LogError(
                    $"{nameof(ActionTimelineController)} on {name}: " +
                    $"Cannot set timeline '{timelineName}' because its " +
                    $"{nameof(ActionTimeline)} is null.",
                    this);

                return;
            }

            if (!m_TimelineLookup.TryGetValue(
                timelineName,
                out RuntimeTimelineData data))
            {
                AddTimeline(timelineName, actionTimeline);
                return;
            }

            /*
             * If the existing timeline is running or paused-but-started,
             * end it before replacing the underlying ActionTimeline.
             */
            if (data.IsStarted)
            {
                StopTimeline(data);
            }

            if (!actionTimeline.SetGameObject(gameObject))
            {
                Debug.LogError(
                    $"{nameof(ActionTimelineController)} on {name}: " +
                    $"One or more actions in timeline '{timelineName}' " +
                    $"failed to initialize with GameObject '{gameObject.name}'.",
                    this);
            }

            data.ActionTimeline = actionTimeline;
        }

        public void AddTimeline(TimerName timelineName, ActionTimeline actionTimeline)
        {
            Initialize();

            if (timelineName == null)
            {
                Debug.LogError(
                    $"{nameof(ActionTimelineController)} on {name}: " +
                    $"Cannot add a timeline with a null {nameof(TimerName)}.",
                    this);

                return;
            }

            if (actionTimeline == null)
            {
                Debug.LogError(
                    $"{nameof(ActionTimelineController)} on {name}: " +
                    $"Cannot add timeline '{timelineName}' because its " +
                    $"{nameof(ActionTimeline)} is null.",
                    this);

                return;
            }

            if (m_TimelineLookup.ContainsKey(timelineName))
            {
                Debug.LogError(
                    $"{nameof(ActionTimelineController)} on {name}: " +
                    $"A timeline named '{timelineName}' already exists. " +
                    $"Timeline names must be unique inside a controller.",
                    this);

                return;
            }

            if (!actionTimeline.SetGameObject(gameObject))
            {
                Debug.LogError(
                    $"{nameof(ActionTimelineController)} on {name}: " +
                    $"One or more actions in timeline '{timelineName}' " +
                    $"failed to initialize with GameObject '{gameObject.name}'.",
                    this);
            }

            RuntimeTimelineData runtimeData =
                new RuntimeTimelineData
                {
                    WhenToStartTimeline =
                        WhenToStartTimeline.DontStartAutomatically,

                    WhenToStopTimeline =
                        WhenToStopTimeline.DontStopAutomatically,

                    TimelineName = timelineName,
                    ActionTimeline = actionTimeline,

                    IsStarted = false,
                    IsPlaying = false,

                    ListIndex = m_AllTimelineActions.Count
                };

            m_AllTimelineActions.Add(runtimeData);
            m_TimelineLookup.Add(timelineName, runtimeData);
        }

        public void RemoveTimeline(TimerName timelineName)
        {
            if (timelineName == null)
            {
                Debug.LogError(
                    $"{nameof(ActionTimelineController)} on {name}: " +
                    $"Cannot remove a timeline with a null {nameof(TimerName)}.",
                    this);

                return;
            }

            Initialize();

            if (!m_TimelineLookup.TryGetValue(
                timelineName,
                out RuntimeTimelineData data))
            {
                Debug.LogError(
                    $"{nameof(ActionTimelineController)} on {name}: " +
                    $"No timeline named '{timelineName}' exists.",
                    this);

                return;
            }

            // Ensure EndAction() is called and that the timeline
            // is moved out of the active region before removal.
            if (data.IsStarted)
            {
                StopTimeline(data);
            }

            int indexToRemove = data.ListIndex;
            int lastIndex = m_AllTimelineActions.Count - 1;

            /*
             * Swap the timeline with the last entry so removal is O(1).
             * SwapTimelineEntries also updates ListIndex on both entries.
             */
            if (indexToRemove != lastIndex)
            {
                SwapTimelineEntries(indexToRemove, lastIndex);
            }

            m_AllTimelineActions.RemoveAt(lastIndex);
            m_TimelineLookup.Remove(timelineName);

            data.ListIndex = -1;
        }

        private void Initialize()
        {
            if (m_Initialized)
            {
                return;
            }

            m_Initialized = true;

            m_AllTimelineActions.Clear();
            m_TimelineLookup.Clear();

            m_ActiveTimelineCount = 0;

            InitializeScriptableTimelines();
            InitializeInlineTimelines();
        }

        private void InitializeInlineTimelines()
        {
            if (m_TimeLineActions == null)
                return;

            for (int i = 0; i < m_TimeLineActions.Count; i++)
            {
                ActionTimelineData source = m_TimeLineActions[i];
                if (source == null || source.TimelineName == null ||
                    source.ActionTimeline == null)
                {
                    Debug.LogError(
                        $"{nameof(ActionTimelineController)} on {name}: " +
                        $"Invalid inline timeline at index {i}.", this);
                    continue;
                }

                if (m_TimelineLookup.ContainsKey(source.TimelineName))
                {
                    Debug.LogError(
                        $"{nameof(ActionTimelineController)} on {name}: " +
                        $"Duplicate timeline name '{source.TimelineName}'.", this);
                    continue;
                }

                ActionTimeline runtime = (ActionTimeline)source.ActionTimeline.Clone();
                if (!runtime.SetGameObject(gameObject))
                {
                    Debug.LogError(
                        $"{nameof(ActionTimelineController)} on {name}: " +
                        $"Failed to initialize inline timeline '{source.TimelineName}'.", this);
                }

                RuntimeTimelineData data = new RuntimeTimelineData
                {
                    WhenToStartTimeline = source.WhenToStartTimeline,
                    WhenToStopTimeline = source.WhenToStopTimeline,
                    TimelineName = source.TimelineName,
                    ActionTimeline = runtime,
                    IsStarted = false,
                    IsPlaying = false,
                    ListIndex = m_AllTimelineActions.Count
                };

                m_AllTimelineActions.Add(data);
                m_TimelineLookup.Add(source.TimelineName, data);
            }
        }

        private void InitializeScriptableTimelines()
        {
            if (m_ScriptableTimeLineActions == null)
            {
                return;
            }

            for (int i = 0; i < m_ScriptableTimeLineActions.Count; i++)
            {
                ActionTimelineScriptableObjectData source =
                    m_ScriptableTimeLineActions[i];

                if (source == null)
                {
                    Debug.LogError(
                        $"{nameof(ActionTimelineController)} on {name}: " +
                        $"Null ScriptableObject timeline data at index {i}.",
                        this);

                    continue;
                }

                AddRuntimeTimeline(source);
            }
        }

        private void AddRuntimeTimeline(ActionTimelineScriptableObjectData data)
        {
            if (data == null)
            {
                Debug.LogError($"{nameof(ActionTimelineController)} on {name}: Null timeline configuration.", this);
                return;
            }

            if (data.TimelineName == null)
            {
                Debug.LogError(
                    $"{nameof(ActionTimelineController)} on {name}: " +
                    $"A timeline has a null {nameof(TimerName)}.",
                    this);

                return;
            }

            if (m_TimelineLookup.ContainsKey(data.TimelineName))
            {
                Debug.LogError(
                    $"{nameof(ActionTimelineController)} on {name}: " +
                    $"More than one timeline uses the name '{data.TimelineName}'. " +
                    $"Timeline names must be unique inside a controller.",
                    this);

                return;
            }

            ActionTimeline runtimeTimeline =
                data.Clone();

            if (runtimeTimeline == null)
            {
                Debug.LogError(
                    $"{nameof(ActionTimelineController)} on {name}: " +
                    $"Failed to clone timeline '{data.TimelineName}'.",
                    this);

                return;
            }

            if (!runtimeTimeline.SetGameObject(gameObject))
            {
                Debug.LogError(
                    $"{nameof(ActionTimelineController)} on {name}: " +
                    $"One or more actions in timeline '{data.TimelineName}' " +
                    $"failed to initialize with GameObject '{gameObject.name}'.",
                    this);
            }

            RuntimeTimelineData runtimeData =
                new RuntimeTimelineData
                {
                    WhenToStartTimeline = data.WhenToStartTimeline,
                    WhenToStopTimeline = data.WhenToStopTimeline,
                    TimelineName = data.TimelineName,
                    ActionTimeline = runtimeTimeline,

                    IsStarted = false,
                    IsPlaying = false,

                    ListIndex = m_AllTimelineActions.Count
                };

            m_AllTimelineActions.Add(runtimeData);
            m_TimelineLookup.Add(data.TimelineName, runtimeData);
        }


        private void StartAutomaticTimelines(
            WhenToStartTimeline trigger)
        {
            if (!m_Initialized)
                return;

            // Snapshot candidates before starting them: StartAction callbacks can
            // end/reorder entries immediately, so iterating the active/inactive
            // partition in-place could retry the same entry indefinitely.
            var candidates = new List<RuntimeTimelineData>();
            for (int i = m_ActiveTimelineCount; i < m_AllTimelineActions.Count; i++)
            {
                RuntimeTimelineData data = m_AllTimelineActions[i];
                if (data.WhenToStartTimeline == trigger && !data.IsStarted)
                    candidates.Add(data);
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                RuntimeTimelineData data = candidates[i];
                if (data.ListIndex >= 0 && !data.IsStarted &&
                    data.ListIndex < m_AllTimelineActions.Count &&
                    ReferenceEquals(m_AllTimelineActions[data.ListIndex], data))
                    StartTimeline(data);
            }
        }


        public void StartTimeline(TimerName timelineName)
        {
            if (!TryGetTimeline(
                timelineName,
                out RuntimeTimelineData data))
            {
                return;
            }

            StartTimeline(data);
        }

        private void StartTimeline(RuntimeTimelineData data)
        {
            if (data == null ||
                data.ActionTimeline == null)
            {
                return;
            }

            if (data.IsStarted)
            {
                data.ActionTimeline.EndAction();
            }

            data.ActionTimeline.StartAction();

            // A start-event listener may immediately end the timeline.
            data.IsStarted = data.ActionTimeline.IsRunning;
            data.IsPlaying = data.IsStarted;

            if (data.IsPlaying)
                MoveToActiveRegion(data);
            else
                MoveToInactiveRegion(data);
        }


        public void StopTimeline(TimerName timelineName)
        {
            if (!TryGetTimeline(
                timelineName,
                out RuntimeTimelineData data))
            {
                return;
            }

            StopTimeline(data);
        }

        private void StopTimeline(RuntimeTimelineData data)
        {
            if (data == null ||
                data.ActionTimeline == null ||
                !data.IsStarted)
            {
                return;
            }

            data.ActionTimeline.EndAction();

            data.IsStarted = false;
            data.IsPlaying = false;

            MoveToInactiveRegion(data);
        }


        public void PlayTimeline(TimerName timelineName)
        {
            if (!TryGetTimeline(
                timelineName,
                out RuntimeTimelineData data))
            {
                return;
            }

            if (data.IsStarted)
            {
                if (data.IsPlaying)
                {
                    return;
                }

                data.IsPlaying = true;

                MoveToActiveRegion(data);
            }
            else
            {
                StartTimeline(data);
            }
        }


        public void PauseTimeline(TimerName timelineName)
        {
            if (!TryGetTimeline(
                timelineName,
                out RuntimeTimelineData data))
            {
                return;
            }

            if (!data.IsStarted ||
                !data.IsPlaying)
            {
                return;
            }

            data.IsPlaying = false;

            // Don't call EndAction().
            // The timeline remains started, but is removed from
            // the per-frame Update region.
            MoveToInactiveRegion(data);
        }


        /// <summary>
        /// Moves a timeline into the contiguous playing region.
        ///
        /// O(1).
        /// </summary>
        private void MoveToActiveRegion(RuntimeTimelineData data)
        {
            int currentIndex = data.ListIndex;

            // Already active.
            if (currentIndex < m_ActiveTimelineCount)
            {
                return;
            }

            int targetIndex = m_ActiveTimelineCount;

            SwapTimelineEntries(currentIndex, targetIndex);

            m_ActiveTimelineCount++;
        }

        /// <summary>
        /// Removes a timeline from the contiguous playing region.
        ///
        /// O(1).
        /// </summary>
        private void MoveToInactiveRegion(RuntimeTimelineData data)
        {
            int currentIndex = data.ListIndex;

            // Already inactive.
            if (currentIndex >= m_ActiveTimelineCount)
            {
                return;
            }

            int lastActiveIndex =
                m_ActiveTimelineCount - 1;

            SwapTimelineEntries(
                currentIndex,
                lastActiveIndex);

            m_ActiveTimelineCount--;
        }

        /// <summary>
        /// Swaps two entries without changing their RuntimeTimelineData
        /// references, so the dictionary remains valid.
        /// </summary>
        private void SwapTimelineEntries(
            int firstIndex,
            int secondIndex)
        {
            if (firstIndex == secondIndex)
            {
                return;
            }

            RuntimeTimelineData first =
                m_AllTimelineActions[firstIndex];

            RuntimeTimelineData second =
                m_AllTimelineActions[secondIndex];

            m_AllTimelineActions[firstIndex] = second;
            m_AllTimelineActions[secondIndex] = first;

            second.ListIndex = firstIndex;
            first.ListIndex = secondIndex;
        }


        private bool TryGetTimeline(
            TimerName timelineName,
            out RuntimeTimelineData timelineData)
        {
            timelineData = null;

            if (timelineName == null)
            {
                Debug.LogError(
                    $"{nameof(ActionTimelineController)} on {name}: " +
                    $"Cannot search for a null {nameof(TimerName)}.",
                    this);

                return false;
            }

            Initialize();

            if (!m_TimelineLookup.TryGetValue(
                timelineName,
                out timelineData))
            {
                Debug.LogError(
                    $"{nameof(ActionTimelineController)} on {name}: " +
                    $"No timeline named '{timelineName}' exists.",
                    this);

                return false;
            }

            return true;
        }
    }
}