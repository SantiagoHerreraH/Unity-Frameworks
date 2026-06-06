using UnityEngine;

namespace SilverPillar.Core
{
    public class CachedGameActionChoosingOption : ChoosingOption<ICachedGameAction>
    {
        public override bool Initialize(GameObject gameObj)
        {
            return Value.SetGameObject(gameObj);
        }

        protected override ICachedGameAction Clone(ICachedGameAction value)
        {
            return value.Clone();
        }
    }
}
