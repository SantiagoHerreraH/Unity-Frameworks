using UnityEngine;

namespace SilverPillar.Core
{
    public abstract class SaveableCondition : SaveableScriptableObject, ICondition
    {
        public abstract bool IsFulfilled(GameObject gameObj);
    }
}
