
using SilverPillar.Core;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Stats
{

    [Serializable]
    public class ChangeOmniModifierStatVariableToOperate : IChangeModifier
    {
        [SerializeField]
        private StatVariable m_StatVariable;
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

                        cast.SelfStatData.StatVariable = m_StatVariable;

                        break;
                    case StatTarget.Target:

                        cast.TargetStatData.StatVariable = m_StatVariable;

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