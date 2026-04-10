
using SilverPillar.Core;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Stats
{

    [Serializable]
    public class ChangeOmniModifierStatOperation : IChangeModifier
    {
        [SerializeField]
        private StatOperation m_StatOperation;
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

                        cast.SelfStatData.StatOperation = m_StatOperation;

                        break;
                    case StatTarget.Target:

                        cast.TargetStatData.StatOperation = m_StatOperation;

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