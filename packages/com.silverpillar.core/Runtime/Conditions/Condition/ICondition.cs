using UnityEngine;

namespace SilverPillar.Core
{
    public interface ICondition
    {
        public bool IsFulfilled(GameObject gameObj);
    }

    
}

