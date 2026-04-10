
using SilverPillar.Core;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Stats
{
    [Serializable]
    public class ChangeValueModifierStatTypes : IChangeModifier
    {
        [SerializeField]
        private StatType m_StatTypeToReplaceTargetStatTypeWith;
        public bool ChangeModifier(IStatModifier modifier)
        {
            ValueStatModifier cast = modifier as ValueStatModifier;
            if (cast != null)
            {
                cast.TargetStatType = m_StatTypeToReplaceTargetStatTypeWith;

                return true;
            }

            return false;
        }
    }
}