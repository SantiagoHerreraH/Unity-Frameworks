using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace SilverPillar.Core
{
    [CreateAssetMenu(fileName = "ActionTimelineScriptableObject", menuName = "SilverPillar/Time/ActionTimelineScriptableObject")]
    public class ActionTimelineScriptableObject : SaveableScriptableObject
    {
        [OdinSerialize, ShowInInspector]
        private ActionTimeline m_ActionTimeline;

        public bool IsNull()
        {
            return m_ActionTimeline == null;
        }

        public ActionTimeline Clone()
        {
            return m_ActionTimeline.Clone() as ActionTimeline;
        }
    }
}
