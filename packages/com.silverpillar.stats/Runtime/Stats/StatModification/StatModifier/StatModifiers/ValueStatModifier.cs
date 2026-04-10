using SilverPillar.Core;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Stats
{
    [Serializable]
    public class ValueStatModifier : IStatModifier
    {

        [Title("Self Stat")]
        [SerializeField]
        public float ModificationValue;


        [Title("Target Stat")]
        [SerializeField]
        public StatType TargetStatType;
        [SerializeField]
        public IncomingModificationData TargetStatData;

        [Title("Condition")]
        [OdinSerialize, ShowInInspector]
        public InteractionConditionGroup Conditions = new();

        public ValueStatModifier() { }

        public ValueStatModifier(ValueStatModifier other)
        {

            ModificationValue = other.ModificationValue;
            Conditions = other.Conditions.Clone() as InteractionConditionGroup;


            TargetStatType = other.TargetStatType;
            TargetStatData = other.TargetStatData;
        }

        public IStatModifier Clone()
        {
            return new ValueStatModifier(this);
        }

        public void Modify(StatController self, StatController target)
        {
            if (Conditions.GetGameObject() != self.gameObject)
            {
                Conditions.SetGameObject(self.gameObject);
            }
            if (target.HasStatType(TargetStatType) &&
                Conditions.IsFulfilled(target.gameObject))
            {

                ModifyTargetStat(target, ModificationValue);
            }
        }

        private void ModifyTargetStat(StatController target, float modificationValue)
        {
            modificationValue = TargetStatData.ModifyIncomingValue(target, modificationValue);

            target.Modify(TargetStatType, modificationValue, TargetStatData.StatOperation, TargetStatData.StatVariable);
        }
    }
}
