using UnityEngine;

namespace SilverPillar.Core
{
    public interface ICachedGameAction
    {
        public bool SetGameObject(GameObject gameObj);
        public GameObject GetGameObject();
        public void Execute();

        public ICachedGameAction Clone();
    }
}

