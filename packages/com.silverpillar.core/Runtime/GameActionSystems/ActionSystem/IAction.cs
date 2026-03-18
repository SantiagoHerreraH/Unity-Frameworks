using UnityEngine;

namespace SilverPillar.Core
{
    public interface IAction
    {
        public bool SetGameObject(GameObject gameObj);
        public GameObject GetGameObject();
        public void StartAction();
        public void UpdateAction();
        public void EndAction();
        public IAction Clone();
    }
}
