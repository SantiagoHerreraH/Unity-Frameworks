using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;

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
            for (int i = 0; i < m_ActiveTimelineCount; i++)
            {
                m_AllTimelineActions[i].ActionTimeline.UpdateAction();
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
            if (data.TimelineName == null)
            {
                Debug.LogError(
                    $"{nameof(ActionTimelineController)} on {name}: " +
                    $"A timeline has a null {nameof(TimerName)}.",
                    this);

                return;
            }

            if (data == null)
            {
                Debug.LogError(
                    $"{nameof(ActionTimelineController)} on {name}: " +
                    $"ScriptableObject assigned to '{data.TimelineName}' " +
                    $"contains no valid {nameof(ActionTimeline)}.",
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
            {
                return;
            }

            /*
             * Activating entries rearranges the list, so a standard for-loop
             * would potentially skip entries.
             *
             * Scan only the inactive region. If an entry is activated,
             * another inactive item is swapped into its old slot, so test
             * the same index again.
             */
            int index = m_ActiveTimelineCount;

            while (index < m_AllTimelineActions.Count)
            {
                RuntimeTimelineData data =
                    m_AllTimelineActions[index];

                if (data.WhenToStartTimeline == trigger)
                {
                    StartTimeline(data);

                    // StartTimeline moves it into the active prefix.
                    // Another inactive item now occupies 'index'.
                }
                else
                {
                    index++;
                }
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

            data.IsStarted = true;
            data.IsPlaying = true;

            MoveToActiveRegion(data);
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