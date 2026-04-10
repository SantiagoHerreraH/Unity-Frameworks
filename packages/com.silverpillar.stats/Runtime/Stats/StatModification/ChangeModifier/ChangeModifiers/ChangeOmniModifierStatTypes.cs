
using SilverPillar.Core;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Stats
{

    [Serializable]
    public class ChangeOmniModifierStatTypes : IChangeModifier
    {
        [SerializeField]
        private StatType m_StatTypeToReplaceWith;
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

                        cast.SelfStatType = m_StatTypeToReplaceWith;

                        break;
                    case StatTarget.Target:

                        cast.TargetStatType = m_StatTypeToReplaceWith;

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