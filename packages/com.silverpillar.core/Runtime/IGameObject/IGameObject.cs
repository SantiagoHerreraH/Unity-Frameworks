using UnityEngine;

namespace SilverPillar.Core
{
    public interface IGameObject
    {
        public bool SetGameObject(GameObject gameObject);
        public GameObject GetGameObject();
        public GameObject CalculateGameObject();
        public IGameObject Clone();
    }
}
