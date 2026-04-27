using UnityEngine;

namespace SilverPillar.Core
{
#nullable enable

    public interface IString
    {
        public IString Clone();
        public bool SetGameObject(GameObject gameObject);
        public GameObject? GetGameObject();
        public string CalculateString();
    }
}
