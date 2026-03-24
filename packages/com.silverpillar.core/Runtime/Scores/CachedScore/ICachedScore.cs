using UnityEngine;

namespace SilverPillar.Core
{
#nullable enable
    public interface ICachedScore
    {
        public ICachedScore Clone();
        public GameObject? GetGameObject();
        public bool SetGameObject(GameObject self);
        public float CalculateScore();
    }
}

