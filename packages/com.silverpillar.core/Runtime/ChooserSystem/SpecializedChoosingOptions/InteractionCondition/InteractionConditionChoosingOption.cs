using UnityEngine;

namespace SilverPillar.Core
{
    public class InteractionConditionChoosingOption : ChoosingOption<IInteractionCondition>
    {
        public override bool Initialize(GameObject gameObj)
        {
            return Value.SetGameObject(gameObj);
        }

        protected override IInteractionCondition Clone(IInteractionCondition value)
        {
            return value.Clone();
        }
    }
}
