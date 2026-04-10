using UnityEngine;

namespace SilverPillar.Stats
{
    public interface IStatModifierCondition
    {
        public bool IsFulfilled(IStatModifier modifier);
    }
}
