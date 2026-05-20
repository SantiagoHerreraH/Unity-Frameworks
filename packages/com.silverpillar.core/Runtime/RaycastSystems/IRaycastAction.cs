using UnityEngine;

namespace SilverPillar.Core
{
    public interface IRaycastAction
    {
        public GameObject GetGameObject();
        public bool SetGameObject(GameObject gameObj);
        public void Execute(RaycastHit[] hits);

        public IRaycastAction Clone();
    }

    public interface IRaycastScoring
    {
        public GameObject GetGameObject();
        public bool SetGameObject(GameObject gameObj);
        public float CalculateScore(RaycastHit hit);

        public IRaycastScoring Clone();
    }
}
