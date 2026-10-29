using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Core
{
    [CreateAssetMenu(
        fileName = "ActionTimelineCreatorScriptableObject",
        menuName = "SilverPillar/Time/ActionTimelineCreatorScriptableObject")]
    public class ActionTimelineCreatorScriptableObject : ScriptableObject
    {
        public enum SegmentType
        {
            ActionTimeline,
            ActionTimelineCreator
        }

        [Serializable]
        private class PossibleSegment
        {
            [Title("Choosing")]
            [SerializeField, Min(1)]
            private int m_ProbabilityOfBeingChosen = 1;

            public int ProbabilityOfBeingChosen =>
                Mathf.Max(1, m_ProbabilityOfBeingChosen);

            [SerializeField, Min(0)]
            [Tooltip("Zero means unlimited selections.")]
            private int m_NumberOfTimesThatCanBeChosenBeforeDiscarded = 0;

            [NonSerialized]
            private int m_CurrentTimesChosen;

            public int CurrentTimesChosen => m_CurrentTimesChosen;

            [Title("Data")]
            [SerializeField]
            private SegmentType m_SegmentType;

            [SerializeField]
            [ShowIf(nameof(m_SegmentType), SegmentType.ActionTimeline)]
            private ActionTimelineScriptableObject m_ActionTimelineScriptableObject;

            [SerializeField]
            [ShowIf(nameof(m_SegmentType), SegmentType.ActionTimelineCreator)]
            private ActionTimelineCreatorScriptableObject m_ActionTimelineCreatorScriptableObject;

            public ActionTimelineScriptableObject Timeline =>
                m_SegmentType == SegmentType.ActionTimeline
                    ? m_ActionTimelineScriptableObject
                    : null;

            public ActionTimelineCreatorScriptableObject Creator =>
                m_SegmentType == SegmentType.ActionTimelineCreator
                    ? m_ActionTimelineCreatorScriptableObject
                    : null;

            public bool IsValid()
            {
                if (m_SegmentType == SegmentType.ActionTimeline)
                {
                    return Timeline != null && !Timeline.IsNull();
                }

                if (m_SegmentType == SegmentType.ActionTimelineCreator)
                {
                    return Creator != null;
                }

                return false;
            }

            public bool CanBeChosen()
            {
                if (!IsValid())
                    return false;

                return m_NumberOfTimesThatCanBeChosenBeforeDiscarded <= 0 ||
                       m_CurrentTimesChosen <
                       m_NumberOfTimesThatCanBeChosenBeforeDiscarded;
            }

            public void Choose()
            {
                m_CurrentTimesChosen++;
            }

            public void ResetNumberOfTimesChosen()
            {
                m_CurrentTimesChosen = 0;
            }
        }

        [Serializable]
        private class TimelineSegment
        {
            [SerializeField]
            private string Name;
            [SerializeField]
            private List<PossibleSegment> m_PossibleSegments = new();

            public IEnumerable<PossibleSegment> PossibleSegments =>
                m_PossibleSegments ?? (IEnumerable<PossibleSegment>)Array.Empty<PossibleSegment>();

            public PossibleSegment Choose()
            {
                if (m_PossibleSegments == null ||
                    m_PossibleSegments.Count == 0)
                {
                    return null;
                }

                int totalWeight = CalculateTotalWeight();

                // All valid limited choices have been exhausted.
                // Reset them and begin a new selection cycle.
                if (totalWeight == 0)
                {
                    ResetNumberOfTimesChosen();
                    totalWeight = CalculateTotalWeight();
                }

                if (totalWeight <= 0)
                    return null;

                int randomValue = RandomController.Instance.Range(
                    0, totalWeight);

                foreach (var possibleSegment in m_PossibleSegments)
                {
                    if (possibleSegment == null ||
                        !possibleSegment.CanBeChosen())
                    {
                        continue;
                    }

                    int weight = possibleSegment.ProbabilityOfBeingChosen;

                    if (randomValue < weight)
                    {
                        possibleSegment.Choose();
                        return possibleSegment;
                    }

                    randomValue -= weight;
                }

                return null;
            }

            private int CalculateTotalWeight()
            {
                long totalWeight = 0;

                foreach (var possibleSegment in m_PossibleSegments)
                {
                    if (possibleSegment == null ||
                        !possibleSegment.CanBeChosen())
                    {
                        continue;
                    }

                    totalWeight += possibleSegment.ProbabilityOfBeingChosen;
                }

                return (int)Math.Min(totalWeight, int.MaxValue);
            }

            public void ResetNumberOfTimesChosen()
            {
                if (m_PossibleSegments == null)
                    return;

                foreach (var possibleSegment in m_PossibleSegments)
                {
                    possibleSegment?.ResetNumberOfTimesChosen();
                }
            }
        }

        [InfoBox("The duration is calculated by adding the total duration of the sequential timeline segments.")]

        [Title("Sequential Timeline Segments")]
        [SerializeField]
        private List<TimelineSegment> m_SequentialTimelineSegment = new();

        [Title("Free Timeline Segments")]
        [SerializeField]
        [Tooltip("Free timelines are distributed across the final duration of the sequential timeline.")]
        private List<TimelineSegment> m_FreeTimelineSegment = new();

        public List<ActionTimelineScriptableObject>
            GetSequentialActionTimelineScriptableObjects()
        {
            return Collect(false);
        }

        public List<ActionTimelineScriptableObject>
            GetFreeActionTimelineScriptableObjects()
        {
            return Collect(true);
        }

        private List<ActionTimelineScriptableObject> Collect(bool free)
        {
            if (!NoInfiniteLoop())
                return null;

            var result = new List<ActionTimelineScriptableObject>();

            CollectInto(free, result);

            return result;
        }

        private void CollectInto(
            bool free,
            List<ActionTimelineScriptableObject> output)
        {
            var segments = free
                ? m_FreeTimelineSegment
                : m_SequentialTimelineSegment;

            if (segments == null)
                return;

            foreach (var segment in segments)
            {
                if (segment == null)
                    continue;

                PossibleSegment chosenSegment = segment.Choose();

                if (chosenSegment == null)
                    continue;

                if (chosenSegment.Timeline != null)
                {
                    output.Add(chosenSegment.Timeline);
                }
                else if (chosenSegment.Creator != null)
                {
                    chosenSegment.Creator.CollectInto(free, output);
                }
            }
        }

        public ActionTimeline Clone()
        {
            if (!NoInfiniteLoop())
                return null;

            var result = new ActionTimeline();
            result.SetDurationCalculationMode(HowToCalculateTimelineDuration.CalculateDurationFromTimelineSequenceData);

            // Sequential timelines
            var sequential =
                new List<ActionTimelineScriptableObject>();

            CollectInto(false, sequential);

            foreach (var asset in sequential)
            {
                if (asset == null || asset.IsNull())
                    continue;

                result.AddSequentialTimeline(asset.Clone());
            }


            // Free timelines
            var free =
                new List<ActionTimelineScriptableObject>();

            CollectInto(true, free);

            int validCount = 0;

            foreach (var asset in free)
            {
                if (asset != null && !asset.IsNull())
                    validCount++;
            }

            if (validCount > 0)
            {
                // Do NOT use result.Duration here. Any nested timeline that
                // uses ICachedScore may not have calculated its duration yet,
                // because SetGameObject() is called later by the controller.
                // Equal-slot placement is resolved after binding and again
                // on each parent StartAction() (if its duration changes).
                int index = 0;
                foreach (var asset in free)
                {
                    if (asset == null || asset.IsNull())
                        continue;

                    result.AddDistributedFreeTimeline(
                        asset.Clone(), index, validCount);
                    index++;
                }
            }

            return result;
        }

        [Button("Reset Selection Counts")]
        public void ResetSelectionCounts()
        {
            var visited =
                new HashSet<ActionTimelineCreatorScriptableObject>();

            ResetSelectionCountsRecursive(visited);
        }

        private void ResetSelectionCountsRecursive(
            HashSet<ActionTimelineCreatorScriptableObject> visited)
        {
            if (!visited.Add(this))
                return;

            ResetSegments(m_SequentialTimelineSegment, visited);
            ResetSegments(m_FreeTimelineSegment, visited);
        }

        private static void ResetSegments(
            List<TimelineSegment> segments,
            HashSet<ActionTimelineCreatorScriptableObject> visited)
        {
            if (segments == null)
                return;

            foreach (var segment in segments)
            {
                if (segment == null)
                    continue;

                segment.ResetNumberOfTimesChosen();

                foreach (var possible in segment.PossibleSegments)
                {
                    if (possible?.Creator != null)
                    {
                        possible.Creator.ResetSelectionCountsRecursive(
                            visited);
                    }
                }
            }
        }

        private bool NoInfiniteLoop()
        {
            var visiting =
                new HashSet<ActionTimelineCreatorScriptableObject>();

            var visited =
                new HashSet<ActionTimelineCreatorScriptableObject>();

            if (CheckCycles(visiting, visited))
                return true;

            Debug.LogError(
                $"Cyclic ActionTimelineCreator reference detected in '{name}'.",
                this);

            return false;
        }

        private bool CheckCycles(
            HashSet<ActionTimelineCreatorScriptableObject> visiting,
            HashSet<ActionTimelineCreatorScriptableObject> visited)
        {
            if (visiting.Contains(this))
                return false;

            if (visited.Contains(this))
                return true;

            visiting.Add(this);

            if (!CheckSegments(
                    m_SequentialTimelineSegment, visiting, visited) ||
                !CheckSegments(
                    m_FreeTimelineSegment, visiting, visited))
            {
                visiting.Remove(this);
                return false;
            }

            visiting.Remove(this);
            visited.Add(this);

            return true;
        }

        private static bool CheckSegments(
            List<TimelineSegment> segments,
            HashSet<ActionTimelineCreatorScriptableObject> visiting,
            HashSet<ActionTimelineCreatorScriptableObject> visited)
        {
            if (segments == null)
                return true;

            foreach (var segment in segments)
            {
                if (segment == null)
                    continue;

                foreach (var possible in segment.PossibleSegments)
                {
                    if (possible == null ||
                        possible.Creator == null)
                    {
                        continue;
                    }

                    if (!possible.Creator.CheckCycles(visiting, visited))
                        return false;
                }
            }

            return true;
        }
    }
}