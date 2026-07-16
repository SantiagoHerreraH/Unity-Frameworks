using UnityEngine;

namespace SilverPillar.Core
{
    public interface IUndoAction
    {
        public bool SetGameObject(GameObject gameObj);
        public GameObject GetGameObject();
        public void StartAction();
        public void UpdateAction();
        public void EndAction();
        public void Undo();

        public IUndoAction Clone();
    }
}
