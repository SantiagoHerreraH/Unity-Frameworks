using UnityEngine;

namespace SilverPillar.Core
{
    public class CachedConditionChoosingOption : ChoosingOption<ICachedCondition>
    {
        public override bool Initialize(GameObject gameObj)
        {
            return Value.SetGameObject(gameObj);
        }

        protected override ICachedCondition Clone(ICachedCondition value)
        {
            return value.Clone();
        }
    }
}
