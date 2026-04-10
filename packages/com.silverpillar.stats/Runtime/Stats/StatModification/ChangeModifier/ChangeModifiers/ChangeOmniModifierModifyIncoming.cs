
using SilverPillar.Core;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Stats
{

    [Serializable]
    public class ChangeOmniModifierModifyIncoming : IChangeModifier
    {
        [SerializeField]
        private StatOperationGroup m_StatOperationGroup;
        [SerializeField]
        private StatTarget m_FromWhichStatTarget;
        public bool ChangeModifier(IStatModifier modifier)
        {
            OmniStatModifier cast = modifier as OmniStatModifier;
            if (cast != null)
            {
                switch (m_FromWhichStatTarget)
                {
                    case StatTarget.Self:

                        cast.SelfStatData.ModifyIncoming = m_StatOperationGroup;

                        break;
                    case StatTarget.Target:

                        cast.TargetStatData.ModifyIncoming = m_StatOperationGroup;

                        break;
                    default:
                        break;

                }

                return true;
            }

            return false;
        }
    }
}