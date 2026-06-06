using UnityEngine;

namespace SilverPillar.Core
{
    public class ActionChoosingOption : ChoosingOption<IAction>
    {
        public override bool Initialize(GameObject gameObj)
        {
            return Value.SetGameObject(gameObj);
        }

        protected override IAction Clone(IAction value)
        {
            return value.Clone();
        }
    }
}
