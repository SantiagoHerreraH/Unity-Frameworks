using UnityEngine;

namespace SilverPillar.Core
{
#nullable enable
    public interface IInteraction
    {
        public IInteraction Clone();
        public bool SetSelf(GameObject self);
        public GameObject? GetSelf();

        public void Interact(GameObject target);
    }
}
