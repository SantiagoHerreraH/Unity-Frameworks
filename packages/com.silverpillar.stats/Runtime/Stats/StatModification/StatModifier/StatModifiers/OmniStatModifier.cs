using SilverPillar.Core;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Stats
{

    [Serializable]
    public class OmniStatModifier : IStatModifier
    {
        [Title("How to get Modification Value")]
        public HowToGetModificationValue HowToGetModificationValue;

        [Title("Self Stat")]
        public StatType SelfStatType;

        public bool CanModifySelfStat = false;
        [EnableIf(nameof(CanModifySelfStat))]
        public IncomingModificationData SelfStatData;


        [Title("Target Stat")]
        [SerializeField]
        public StatType TargetStatType;
        [SerializeField]
        public IncomingModificationData TargetStatData;


        [Title("Condition")]
        [OdinSerialize, ShowInInspector]
        public InteractionConditionGroup Conditions = new();

        public OmniStatModifier() { }

        public OmniStatModifier(OmniStatModifier other)
        {
            HowToGetModificationValue = other.HowToGetModificationValue;

            SelfStatType = other.SelfStatType;
            CanModifySelfStat = other.CanModifySelfStat;
            SelfStatData = other.SelfStatData;


            TargetStatType = other.TargetStatType;
            TargetStatData = other.TargetStatData;

            Conditions = other.Conditions.Clone() as InteractionConditionGroup;
        }

        public IStatModifier Clone()
        {
            return new OmniStatModifier(this);
        }

        public void Modify(StatController self, StatController target)
        {
            if (Conditions.GetGameObject() != self.gameObject)
            {
                Conditions.SetGameObject(self.gameObject);
            }
            if (self.HasStatType(SelfStatType) &&
                target.HasStatType(TargetStatType) &&
                Conditions.IsFulfilled(target.gameObject))
            {
                float modificationValue = HowToGetModificationValue.GetModificationValue(self, target, SelfStatType);

                if (CanModifySelfStat)
                {
                    ModifySelfStat(self, modificationValue);
                }

                ModifyTargetStat(target, modificationValue);
            }
        }

        private void ModifySelfStat(StatController self, float modificationValue)
        {
            modificationValue = SelfStatData.ModifyIncomingValue(self, modificationValue);
            self.Modify(SelfStatType, modificationValue, SelfStatData.StatOperation, SelfStatData.StatVariable);
        }

        private void ModifyTargetStat(StatController target, float modificationValue)
        {
            modificationValue = TargetStatData.ModifyIncomingValue(target, modificationValue);

            target.Modify(TargetStatType, modificationValue, TargetStatData.StatOperation, TargetStatData.StatVariable);
        }
    }
}
