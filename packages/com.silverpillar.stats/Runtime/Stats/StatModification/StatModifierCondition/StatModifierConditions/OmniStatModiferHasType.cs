using SilverPillar.Core;
using System;
using UnityEngine;

namespace SilverPillar.Stats
{

    [Serializable]
    public class OmniStatModifierHasStatType : IStatModifierCondition
    {
        [SerializeField]
        private StatType m_StatType;
        [SerializeField]
        private StatTarget m_FromWhichStatTarget;

        public bool IsFulfilled(IStatModifier modifier)
        {
            OmniStatModifier cast = modifier as OmniStatModifier;

            if (cast != null)
            {
                switch (m_FromWhichStatTarget)
                {
                    case StatTarget.Self:

                        return cast.SelfStatType == m_StatType;

                    case StatTarget.Target:


                        return cast.TargetStatType == m_StatType;

                    default:
                        break;
                }
            }

            return false;
        }
    }
}
