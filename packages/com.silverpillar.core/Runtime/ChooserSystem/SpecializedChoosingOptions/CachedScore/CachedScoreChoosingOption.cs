using UnityEngine;

namespace SilverPillar.Core
{
    public class CachedScoreChoosingOption : ChoosingOption<ICachedScore>
    {
        public override bool Initialize(GameObject gameObj)
        {
            return Value.SetGameObject(gameObj);
        }

        protected override ICachedScore Clone(ICachedScore value)
        {
            return value.Clone();
        }
    }
}
