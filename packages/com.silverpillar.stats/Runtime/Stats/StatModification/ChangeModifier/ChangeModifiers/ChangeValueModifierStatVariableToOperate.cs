
using SilverPillar.Core;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Stats
{
    [Serializable]
    public class ChangeValueModifierStatVariableToOperate : IChangeModifier
    {
        [SerializeField]
        private StatVariable m_StatVariable;
        public bool ChangeModifier(IStatModifier modifier)
        {
            ValueStatModifier cast = modifier as ValueStatModifier;
            if (cast != null)
            {
                cast.TargetStatData.StatVariable = m_StatVariable;
                return true;
            }

            return false;
        }
    }
}