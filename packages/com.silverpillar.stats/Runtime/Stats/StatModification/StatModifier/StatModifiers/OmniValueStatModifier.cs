using SilverPillar.Core;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Stats
{

    [Serializable]
    public class OmniValueStatModifier : IStatModifier
    {
        [Title("Modification Value")]
        [SerializeField, Tooltip("This is the normal behaviour.")]
        public bool InterpretModificationValueAsPercentageOfWorldMax = true;
        [SerializeField, HideIf(nameof(InterpretModificationValueAsPercentageOfWorldMax))]
        public StatVariable InterpretModificationValueAsAPercentageOfTargetStatVariable;
        [OdinSerialize, ShowInInspector]
        public ICachedInteractionScore ModificationValue;

        [Title("Target Stat")]
        [SerializeField]
        public StatType TargetStatType;
        [SerializeField]
        public IncomingModificationData TargetStatData;

        [Title("Condition")]
        [OdinSerialize, ShowInInspector]
        public InteractionConditionGroup Conditions = new();

        public OmniValueStatModifier() { }

        public OmniValueStatModifier(OmniValueStatModifier other)
        {
            InterpretModificationValueAsPercentageOfWorldMax = other.InterpretModificationValueAsPercentageOfWorldMax;
            InterpretModificationValueAsAPercentageOfTargetStatVariable = other.InterpretModificationValueAsAPercentageOfTargetStatVariable;
            ModificationValue = other.ModificationValue.Clone();

            TargetStatType = other.TargetStatType;
            TargetStatData = other.TargetStatData;

            Conditions = other.Conditions.Clone() as InteractionConditionGroup;
        }

        public IStatModifier Clone()
        {
            return new OmniValueStatModifier(this);
        }

        public void Modify(StatController self, StatController target)
        {
            if (Conditions.GetGameObject() != self.gameObject)
            {
                Conditions.SetGameObject(self.gameObject);
            }

            if (ModificationValue.GetGameObject() != self.gameObject)
            {
                ModificationValue.SetGameObject(self.gameObject);
            }

            if (target.HasStatType(TargetStatType) &&
                Conditions.IsFulfilled(target.gameObject))
            {
                float modificationValue = GetModificationValue(target, TargetStatType);

                ModifyTargetStat(target, modificationValue);
            }
        }

        private void ModifyTargetStat(StatController target, float modificationValue)
        {
            modificationValue = TargetStatData.ModifyIncomingValue(target, modificationValue);

            target.Modify(TargetStatType, modificationValue, TargetStatData.StatOperation, TargetStatData.StatVariable);
        }

        public float GetModificationValue(StatController target, StatType targetStat)
        {
            float modificationValue = ModificationValue.CalculateScore(target.gameObject);

            float percentageModifier = 1;

            if (!InterpretModificationValueAsPercentageOfWorldMax)
            {
                switch (InterpretModificationValueAsAPercentageOfTargetStatVariable)
                {
                    case StatVariable.Current:

                        percentageModifier = modificationValue;

                        break;
                    case StatVariable.Default:

                        percentageModifier = target.GetDefaultStat(targetStat);

                        break;
                    case StatVariable.MaxLimit:

                        percentageModifier = target.GetMaxLimitStat(targetStat);
                        break;
                    case StatVariable.MinLimit:

                        percentageModifier = target.GetMinLimitStat(targetStat);
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
