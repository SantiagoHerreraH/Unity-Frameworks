using UnityEngine;

namespace SilverPillar.Core
{
    public interface ICachedCondition
    {
        public bool SetGameObject(GameObject gameObj);
        public GameObject GetGameObject();
        public bool IsFulfilled();

        public ICachedCondition Clone();
    }
}
