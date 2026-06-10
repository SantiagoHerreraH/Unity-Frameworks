using UnityEngine;

namespace SilverPillar.Core
{
    public class InteractionChoosingOption : ChoosingOption<IInteraction>
    {
        public override bool Initialize(GameObject gameObj)
        {
            return Value.SetSelf(gameObj);
        }

        protected override IInteraction Clone(IInteraction value)
        {
            return value.Clone();
        }
    }
}
