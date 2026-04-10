using SilverPillar.Core;
using System;
using UnityEngine;

namespace SilverPillar.Stats
{
    [Serializable]
    public class ValueStatModifierHasStatType : IStatModifierCondition
    {
        [SerializeField]
        private StatType m_TargetStatType;

        public bool IsFulfilled(IStatModifier modifier)
        {
            ValueStatModifier cast = modifier as ValueStatModifier;

            if (cast != null)
            {
                return cast.TargetStatType == m_TargetStatType;
            }

            return false;
        }
    }
}
