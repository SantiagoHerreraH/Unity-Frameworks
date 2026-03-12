using UnityEngine;


namespace Pillar
{
    public interface IScore
    {
        public float CalculateScore(GameObject gameObject);
    }
    public interface IInteractionScore
    {
        public float CalculateScore(GameObject self, GameObject target);
    }

    public abstract class SaveableInteractionScore : SaveableScriptableObject, IInteractionScore
    {
        public abstract float CalculateScore(GameObject self, GameObject target);
    }
}

