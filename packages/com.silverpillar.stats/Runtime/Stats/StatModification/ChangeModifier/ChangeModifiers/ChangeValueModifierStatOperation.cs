
using SilverPillar.Core;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Stats
{
    [Serializable]
    public class ChangeValueModifierStatOperation : IChangeModifier
    {
        [SerializeField]
        private StatOperation m_StatOperation;
        public bool ChangeModifier(IStatModifier modifier)
        {
            ValueStatModifier cast = modifier as ValueStatModifier;
            if (cast != null)
            {
                cast.TargetStatData.StatOperation = m_StatOperation;

                return true;
            }

            return false;
        }
    }
}