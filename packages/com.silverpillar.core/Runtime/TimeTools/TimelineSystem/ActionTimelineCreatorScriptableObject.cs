using System;
using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Core
{
    [CreateAssetMenu(fileName = "ActionTimelineCreatorScriptableObject", menuName = "SilverPillar/Time/ActionTimelineCreatorScriptableObject")]
    public class ActionTimelineCreatorScriptableObject : ScriptableObject
    {
        private enum SegmentType { ActionTimeline, ActionTimelineCreator }

        [Serializable]
        private class TimelineSegment
        {
            [SerializeField] private SegmentType m_SegmentType;
            [SerializeField] private ActionTimelineScriptableObject m_ActionTimelineScriptableObject;
            [SerializeField] private ActionTimelineCreatorScriptableObject m_ActionTimelineCreatorScriptableObject;

            public ActionTimelineScriptableObject Timeline => m_SegmentType == SegmentType.ActionTimeline ? m_ActionTimelineScriptableObject : null;
            public ActionTimelineCreatorScriptableObject Creator => m_SegmentType == SegmentType.ActionTimelineCreator ? m_ActionTimelineCreatorScriptableObject : null;
        }

        [SerializeField] private List<TimelineSegment> m_SequentialTimelineSegment = new();
        [SerializeField, Tooltip("Free timelines are distributed across the final duration of the sequential timeline.")]
        private List<TimelineSegment> m_FreeTimelineSegment = new();

        public List<ActionTimelineScriptableObject> GetSequentialActionTimelineScriptableObjects()
        {
            return Collect(false);
        }

        public List<ActionTimelineScriptableObject> GetFreeActionTimelineScriptableObjects()
        {
            return Collect(true);
        }

        private List<ActionTimelineScriptableObject> Collect(bool free)
        {
            if (!NoInfiniteLoop()) return null;
            var result = new List<ActionTimelineScriptableObject>();
            CollectInto(free, result);
            return result;
        }

        private void CollectInto(bool free, List<ActionTimelineScriptableObject> output)
        {
            var segments = free ? m_FreeTimelineSegment : m_SequentialTimelineSegment;
            if (segments == null) return;
            foreach (var segment in segments)
            {
                if (segment == null) continue;
                if (segment.Timeline != null) output.Add(segment.Timeline);
                else if (segment.Creator != null) segment.Creator.CollectInto(free, output);
            }
        }

        public ActionTimeline Clone()
        {
            if (!NoInfiniteLoop()) return null;
            var result = new ActionTimeline();
            var sequential = GetSequentialActionTimelineScriptableObjects();
            foreach (var asset in sequential)
            {
                if (asset == null || asset.IsNull()) continue;
                result.AddSequentialTimeline(asset.Clone());
            }

            var free = GetFreeActionTimelineScriptableObjects();
            // Free segments are arranged evenly across the composed duration.
            // Each nested timeline retains its own actions and relative timing.
            int validCount = 0;
            foreach (var asset in free)
                if (asset != null && !asset.IsNull()) validCount++;

            if (validCount > 0)
            {
                float slotDuration = result.Duration / validCount;
                int index = 0;
                foreach (var asset in free)
                {
                    if (asset == null || asset.IsNull()) continue;
                    result.AddFreeTimeline(asset.Clone(), index * slotDuration, slotDuration);
                    index++;
                }
            }
            return result;
        }

        private bool NoInfiniteLoop()
        {
            var visiting = new HashSet<ActionTimelineCreatorScriptableObject>();
            var visited = new HashSet<ActionTimelineCreatorScriptableObject>();
            if (CheckCycles(visiting, visited)) return true;
            Debug.LogError($"Cyclic ActionTimelineCreator reference detected in '{name}'.", this);
            return false;
        }

        private bool CheckCycles(HashSet<ActionTimelineCreatorScriptableObject> visiting,
            HashSet<ActionTimelineCreatorScriptableObject> visited)
        {
            if (visiting.Contains(this)) return false;
            if (visited.Contains(this)) return true;
            visiting.Add(this);
            if (!CheckSegments(m_SequentialTimelineSegment, visiting, visited) ||
                !CheckSegments(m_FreeTimelineSegment, visiting, visited)) return false;
            visiting.Remove(this);
            visited.Add(this);
            return true;
        }

        private static bool CheckSegments(List<TimelineSegment> segments,
            HashSet<ActionTimelineCreatorScriptableObject> visiting,
            HashSet<ActionTimelineCreatorScriptableObject> visited)
        {
            if (segments == null) return true;
            foreach (var segment in segments)
                if (segment?.Creator != null && !segment.Creator.CheckCycles(visiting, visited)) return false;
            return true;
        }
    }
}
