using UnityEngine;

namespace SilverPillar.Core
{
#nullable enable
    public interface ICachedInteractionScore
    {
        public ICachedInteractionScore Clone();
        public GameObject? GetGameObject();
        public bool SetGameObject(GameObject self);
        public float CalculateScore(GameObject target);
    }
}

