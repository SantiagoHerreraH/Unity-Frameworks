
using SilverPillar.Core;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Stats
{
    [Serializable]
    public class ChangeValueModifierModifyIncoming : IChangeModifier
    {
        [SerializeField]
        private StatOperationGroup m_StatOperationGroup;
        public bool ChangeModifier(IStatModifier modifier)
        {
            ValueStatModifier cast = modifier as ValueStatModifier;
            if (cast != null)
            {
                cast.TargetStatData.ModifyIncoming = m_StatOperationGroup;

                return true;
            }

            return false;
        }
    }
}