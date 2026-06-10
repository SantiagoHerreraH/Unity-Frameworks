using UnityEngine;

namespace SilverPillar.Core
{
    public class CachedInteractionScoreChoosingOption : ChoosingOption<ICachedInteractionScore>
    {
        public override bool Initialize(GameObject gameObj)
        {
            return Value.SetGameObject(gameObj);
        }

        protected override ICachedInteractionScore Clone(ICachedInteractionScore value)
        {
            return value.Clone();
        }
    }
}
