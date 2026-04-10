using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace SilverPillar.Stats
{
    [Serializable]
    public class HowToGetModificationValue
    {
        [SerializeField]
        private StatTarget m_FromWhoToGetModifierStat;
        [SerializeField, Tooltip("This is the normal behaviour")]
        private bool m_InterpretModificationValueAsPercentageOfWorldMax = true;
        [SerializeField, HideIf(nameof(m_InterpretModificationValueAsPercentageOfWorldMax))]
        private StatVariable m_InterpretModificationValueAsAPercentageOf;

        public float GetModificationValue(StatController self, StatController target, StatType statType)
        {
            StatController used = null;

            switch (m_FromWhoToGetModifierStat)
            {
                case StatTarget.Self:
                    used = self;
                    break;
                case StatTarget.Target:
                    used = target;
                    break;
                default:
                    break;
            }

            float modificationValue = used.GetCurrentStat(statType);

            float percentageModifier = 1;

            if (!m_InterpretModificationValueAsPercentageOfWorldMax)
            {
                switch (m_InterpretModificationValueAsAPercentageOf)
                {
                    case StatVariable.Current:

                        percentageModifier = modificationValue;

                        break;
                    case StatVariable.Default:

                        percentageModifier = used.GetDefaultStat(statType);

                        break;
                    case StatVariable.MaxLimit:

                        percentageModifier = used.GetMaxLimitStat(statType);
                        break;
                    case StatVariable.MinLimit:

                        percentageModifier = used.GetMinLimitStat(statType);
                        break;
                    default:
                        break;
                }


                modificationValue /= percentageModifier == 0 ? 0.001f : percentageModifier;
            }

            return modificationValue;
        }
    }
}
