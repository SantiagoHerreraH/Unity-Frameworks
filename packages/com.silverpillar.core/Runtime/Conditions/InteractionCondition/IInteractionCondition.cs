using UnityEngine;

namespace SilverPillar.Core
{
#nullable enable

    public interface IInteractionCondition
    {
        public bool SetGameObject(GameObject self);
        public GameObject? GetGameObject();
        public bool IsFulfilled(GameObject target);

        public IInteractionCondition Clone();
    }
}
