using UnityEngine;

namespace SilverPillar.Core
{
    public class ActionTimelineChoosingOption : ChoosingOption<ActionTimeline>
    {
        public override bool Initialize(GameObject gameObj)
        {
            return Value.SetGameObject(gameObj);
        }

        protected override ActionTimeline Clone(ActionTimeline value)
        {
            return value.Clone() as ActionTimeline;
        }
    }
}
