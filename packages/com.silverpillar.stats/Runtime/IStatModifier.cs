using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Pillar
{
    public interface IStatOperation
    {
        public float ModifyIncomingValue(StatController controllerThatWantsToBeModified, float incomingModificationValueFromData);

    }

    [Serializable]
    public class SimpleStatOperation : IStatOperation
    {
        public enum WhenToModifyStatTypeToOperate
        {
            BeforeModifyingIncomingValue,
            AfterModifyingIncomingValue
        }

        [Title("Stat Operation")]
        [SerializeField]
        private StatType m_StatTypeToOperateModification;
        [SerializeField]
        private StatOperation m_StatOperation;
        [SerializeField]
        private StatVariable m_StatVariable;

        [Title("Operation on Stat Type To Operate")]
        [SerializeField]
        private bool m_ApplyOperationToStatTypeToOperate;
        [SerializeField, ShowIf(nameof(m_ApplyOperationToStatTypeToOperate))]
        private StatOperation m_StatOperationToApplyOnStatTypeToOperate;
        [SerializeField, ShowIf(nameof(m_ApplyOperationToStatTypeToOperate))]
        private StatVariable m_StatVariableToModifyOnStatTypeToOperate;
        [SerializeField, ShowIf(nameof(m_ApplyOperationToStatTypeToOperate))]
        private WhenToModifyStatTypeToOperate m_WhenToModifyStatTypeToOperate;

        public float ModifyIncomingValue(StatController controllerThatWantsToBeModified, float incomingModificationValueFromData)
        {
            if (controllerThatWantsToBeModified.HasStatType(m_StatTypeToOperateModification))
            {
                float modValue = controllerThatWantsToBeModified.GetStat(m_StatTypeToOperateModification, m_StatVariable);

                if (m_ApplyOperationToStatTypeToOperate && m_WhenToModifyStatTypeToOperate == WhenToModifyStatTypeToOperate.BeforeModifyingIncomingValue)
                {
                    controllerThatWantsToBeModified.Modify(m_StatTypeToOperateModification, incomingModificationValueFromData, m_StatOperationToApplyOnStatTypeToOperate, m_StatVariableToModifyOnStatTypeToOperate);
                }

                switch (m_StatOperation)
                {
                    case StatOperation.Add:
                        incomingModificationValueFromData += modValue;
                        break;
                    case StatOperation.Subtract:
                        incomingModificationValueFromData -= modValue;
                        break;
                    case StatOperation.Divide:
                        incomingModificationValueFromData /= modValue == 0 ? 0.0001f : modValue;
                        break;
                    case StatOperation.Multiply:
                        incomingModificationValueFromData *= modValue;
                        break;
                    default:
                        break;
                }

                if (m_ApplyOperationToStatTypeToOperate && m_WhenToModifyStatTypeToOperate == WhenToModifyStatTypeToOperate.AfterModifyingIncomingValue)
                {
                    controllerThatWantsToBeModified.Modify(m_StatTypeToOperateModification, incomingModificationValueFromData, m_StatOperationToApplyOnStatTypeToOperate, m_StatVariableToModifyOnStatTypeToOperate);
                }
            }

            return incomingModificationValueFromData;
        }
    }

    [Serializable]
    public struct StatOperationGroup
    {
        [OdinSerialize]
        public List<IStatOperation> StatOperatorsThatModifyIncoming;

        public float ModifyIncomingValue(StatController controllerThatWantsToBeModified, float incomingModificationValueFromData)
        {
            foreach (var item in StatOperatorsThatModifyIncoming)
            {
                incomingModificationValueFromData = item.ModifyIncomingValue(controllerThatWantsToBeModified, incomingModificationValueFromData);
            }

            return incomingModificationValueFromData;
        }
    }

    [Serializable]
    public class StatModificationData
    {
        [Title("Self Stat")]
        public StatType SelfStatType;
        [OdinSerialize]
        public HashSet<SO_Ref<StatTag>> NecessarySelfTags = new();
        public bool CanModifySelfStat = false;
        public StatOperationGroup ModifyIncomingSelf;


        [Title("Target Stat")]
        public StatType TargetStatType;
        [OdinSerialize]
        public HashSet<SO_Ref<StatTag>> NecessaryTargetTags = new();
        public StatOperationGroup ModifyIncomingTarget;
    }

    [Serializable]
    public struct IncomingModificationData
    {
        public StatOperationGroup ModifyIncoming;
        public StatOperation StatOperation;
        public StatVariable StatVariable;

        public float ModifyIncomingValue(StatController controllerThatWantsToBeModified, float incomingModificationValueFromData)
        {
            return ModifyIncoming.ModifyIncomingValue(controllerThatWantsToBeModified, incomingModificationValueFromData);
        }
    }

    public interface IStatModifier
    {
        public void Modify(StatController self, StatController target);
        public IStatModifier Clone();
    }

    public interface IStatModifierFactory
    {
        public IStatModifierFactory Clone();
        public List<IStatModifier> CreateInstances(StatController fromWho);
    }

    public enum StatTarget
    {
        Self,
        Target
    }

    public enum StatTargets
    {
        Self,
        Target,
        Both
    }

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

    [Serializable]
    public class ValueStatModifier : IStatModifier
    {
        [Title("How to get Modification Value")]
        public HowToGetModificationValue HowToGetModificationValue;

        [Title("Self Stat")]
        public float ModificationValue;
        [OdinSerialize]
        public HashSet<SO_Ref<StatTag>> NecessarySelfTags = new();


        [Title("Target Stat")]
        [SerializeField]
        public StatType TargetStatType;
        [OdinSerialize]
        public HashSet<SO_Ref<StatTag>> NecessaryTargetTags = new();
        [SerializeField]
        public IncomingModificationData TargetStatData;

        public ValueStatModifier(ValueStatModifier other)
        {
            HowToGetModificationValue = other.HowToGetModificationValue;

            ModificationValue = other.ModificationValue;
            NecessarySelfTags = new(other.NecessarySelfTags);


            TargetStatType = other.TargetStatType;
            NecessaryTargetTags = new(other.NecessaryTargetTags);
            TargetStatData = other.TargetStatData;
        }

        public IStatModifier Clone()
        {
            return new ValueStatModifier(this);
        }

        public void Modify(StatController self, StatController target)
        {
            if (target.HasStatType(TargetStatType) &&
                self.StatTags.Overlaps(NecessarySelfTags) &&
                target.StatTags.Overlaps(NecessaryTargetTags))
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

    [Serializable]
    public class OmniStatModifier : IStatModifier
    {
        [Title("How to get Modification Value")]
        public HowToGetModificationValue HowToGetModificationValue;

        [Title("Self Stat")]
        public StatType SelfStatType;
        [OdinSerialize]
        public HashSet<SO_Ref<StatTag>> NecessarySelfTags = new();
        
        public bool CanModifySelfStat = false;
        [EnableIf(nameof(ModifySelfStat))]
        public IncomingModificationData SelfStatData;


        [Title("Target Stat")]
        [SerializeField]
        public StatType TargetStatType;
        [OdinSerialize]
        public HashSet<SO_Ref<StatTag>> NecessaryTargetTags = new();
        [SerializeField]
        public IncomingModificationData TargetStatData;

        public OmniStatModifier(OmniStatModifier other)
        {
            HowToGetModificationValue = other.HowToGetModificationValue;

            SelfStatType = other.SelfStatType;
            NecessarySelfTags = new(other.NecessarySelfTags);
            CanModifySelfStat = other.CanModifySelfStat;
            SelfStatData = other.SelfStatData;


            TargetStatType = other.TargetStatType;
            NecessaryTargetTags = new(other.NecessaryTargetTags);
            TargetStatData = other.TargetStatData; 
        }

        public IStatModifier Clone()
        {
            return new OmniStatModifier(this);
        }

        public void Modify(StatController self, StatController target)
        {
            if (self.HasStatType(SelfStatType) &&
                target.HasStatType(TargetStatType) &&
                self.StatTags.Overlaps(NecessarySelfTags) &&
                target.StatTags.Overlaps(NecessaryTargetTags))
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

    [Serializable]
    public class StatModifierCopier : IStatModifierFactory
    {
        //Make both stats data available to all stat modifiers
        // make both tag hashsets available to all stat modifiers

        [Title("Copy Modifier if")]
        [SerializeField]
        private StatConditionGroup m_ConditionsAreFulfilled = new();


        [Title("Once copied, Change modifier")]
        [InfoBox("The Modifiers will be pasted on Self.")]
        [OdinSerialize]
        private List<IChangeModifier> m_ModificationsToMakeOnCopiedModifiers = new();

        public StatModifierCopier(StatModifierCopier other)
        {
            m_ConditionsAreFulfilled = other.m_ConditionsAreFulfilled;
            m_ModificationsToMakeOnCopiedModifiers = new(other.m_ModificationsToMakeOnCopiedModifiers);
        }

        public IStatModifierFactory Clone()
        {
            return new StatModifierCopier(this);
        }

        public List<IStatModifier> CreateInstances(StatController fromWho)
        {
            List<IStatModifier> modifiers = GetModificationsToCopy(fromWho);
            return PasteOn(modifiers);
        }

        private List<IStatModifier> GetModificationsToCopy(StatController self)
        {
            List<IStatModifier> modifications = new List<IStatModifier>();

            foreach (var modifier in self.StatModifiers)
            {
                IStatModifier mod = modifier.Get().Get().Clone();
                if (m_ConditionsAreFulfilled.IsFulfilled(mod))
                {
                    modifications.Add(mod);
                }
            }

            return modifications;
        }

        private List<IStatModifier> PasteOn(List<IStatModifier> modifiers)
        {
            List<IStatModifier> modedModifiers = new();

            foreach (var modifier in modifiers)
            {
                modedModifiers.Add(modifier.Clone());

                foreach (var modification in m_ModificationsToMakeOnCopiedModifiers)
                {
                    modification.ChangeModifier(modedModifiers.Last());
                }
            }

            return modedModifiers;
        }
    }

    public interface IStatModifierCondition
    {
        public bool IsFulfilled(IStatModifier modifier);
    }

    [Serializable]
    public class IsModifierType : IStatModifierCondition
    {
        [OdinSerialize]
        [TypeFilter(nameof(GetTypes))]
        private Type m_ModifierType;

        private IEnumerable<Type> GetTypes()
        {
            return typeof(IStatModifier).Assembly
                .GetTypes()
                .Where(t => typeof(IStatModifier).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);
        }

        public bool IsFulfilled(IStatModifier modifier)
        {
            return modifier != null &&
                   m_ModifierType != null &&
                   m_ModifierType.IsInstanceOfType(modifier);
        }
    }

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

    [Serializable]
    public class ValueStatModifierHasStatType : IStatModifierCondition
    {
        [SerializeField]
        private StatType m_TargetStatType;

        public bool IsFulfilled(IStatModifier modifier)
        {
            ValueStatModifier cast = modifier as ValueStatModifier;

            if (cast != null)
            {
                return cast.TargetStatType == m_TargetStatType;
            }

            return false;
        }
    }

    [Serializable]
    public class OmniStatModifierHasStatTags : IStatModifierCondition
    {
        [SerializeField]
        private HashSet<SO_Ref<StatTag>> m_StatTags = new();
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

                        return cast.NecessarySelfTags.Overlaps(m_StatTags);

                    case StatTarget.Target:


                        return cast.NecessaryTargetTags.Overlaps(m_StatTags);

                    default:
                        break;
                }
            }

            return false;
        }
    }


    [Serializable]
    public class ValueStatModifierHasStatTags : IStatModifierCondition
    {
        [SerializeField]
        private HashSet<SO_Ref<StatTag>> m_StatTags = new();
        [SerializeField]
        private StatTarget m_FromWhichStatTarget;

        public bool IsFulfilled(IStatModifier modifier)
        {
            ValueStatModifier cast = modifier as ValueStatModifier;

            if (cast != null)
            {
                switch (m_FromWhichStatTarget)
                {
                    case StatTarget.Self:

                        return cast.NecessarySelfTags.Overlaps(m_StatTags);

                    case StatTarget.Target:


                        return cast.NecessaryTargetTags.Overlaps(m_StatTags);

                    default:
                        break;
                }
            }

            return false;
        }
    }

    [Serializable]
    public struct StatCondition
    {
        [OdinSerialize]
        private IStatModifierCondition m_StatModifierCondition;
        [SerializeField]
        private bool m_ReturnValueIfIsFulfilled;

        public bool IsFulfilled(IStatModifier modifier)
        {
            return m_ReturnValueIfIsFulfilled == m_StatModifierCondition.IsFulfilled(modifier);
        }
    }

    [Serializable]
    public struct AndStatConditions
    {
        [SerializeField]
        private List<StatCondition> m_StatConditions;
        public bool IsFulfilled(IStatModifier modifier)
        {
            foreach (var condition in m_StatConditions)
            {
                if (!condition.IsFulfilled(modifier))
                {
                    return false;
                }
            }

            return true;
        }
    }

    [Serializable]
    public struct StatConditionGroup
    {
        public List<AndStatConditions> m_OrConditions;

        public bool IsFulfilled(IStatModifier modifier)
        {
            foreach (var condition in m_OrConditions)
            {
                if (condition.IsFulfilled(modifier))
                {
                    return true;
                }
            }

            return false;
        }

    }

    public interface IChangeModifier
    {
        public bool ChangeModifier(IStatModifier modifier);
    }

    [Serializable]
    public class ChangeOmniModifierTags : IChangeModifier
    {
        public enum TagOperation
        {
            AddNew,
            ReplaceWithNew
        }

        [SerializeField]
        public TagOperation m_TagOperation;
        [OdinSerialize]
        public HashSet<SO_Ref<StatTag>> m_NewTags = new();
        [SerializeField]
        private StatTarget m_FromWhichStatTarget;
        public bool ChangeModifier(IStatModifier modifier)
        {
            OmniStatModifier cast = modifier as OmniStatModifier;
            if (cast != null)
            {
                HashSet<SO_Ref<StatTag>> oldTags = null;

                switch (m_FromWhichStatTarget)
                {
                    case StatTarget.Self:

                        oldTags = cast.NecessarySelfTags;

                        break;
                    case StatTarget.Target:

                        oldTags = cast.NecessaryTargetTags;

                        break;

                    default:
                        break;
                }

                switch (m_TagOperation)
                {
                    case TagOperation.AddNew:
                        oldTags.UnionWith(m_NewTags);
                        break;
                    case TagOperation.ReplaceWithNew:
                        oldTags.Clear();
                        oldTags.UnionWith(m_NewTags);
                        break;
                    default:
                        break;
                }

                return true;
            }

            return false;
        }
    }

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

    [Serializable]
    public class ChangeValueModifierTags : IChangeModifier
    {
        public enum TagOperation
        {
            AddNew,
            ReplaceWithNew
        }

        [SerializeField]
        public TagOperation m_TagOperation;
        [OdinSerialize]
        public HashSet<SO_Ref<StatTag>> m_NewTags = new();
        [SerializeField]
        private StatTarget m_FromWhichStatTarget;
        public bool ChangeModifier(IStatModifier modifier)
        {
            ValueStatModifier cast = modifier as ValueStatModifier;
            if (cast != null)
            {
                HashSet<SO_Ref<StatTag>> oldTags = null;

                switch (m_FromWhichStatTarget)
                {
                    case StatTarget.Self:

                        oldTags = cast.NecessarySelfTags;

                        break;
                    case StatTarget.Target:

                        oldTags = cast.NecessaryTargetTags;

                        break;

                    default:
                        break;
                }

                switch (m_TagOperation)
                {
                    case TagOperation.AddNew:
                        oldTags.UnionWith(m_NewTags);
                        break;
                    case TagOperation.ReplaceWithNew:
                        oldTags.Clear();
                        oldTags.UnionWith(m_NewTags);
                        break;
                    default:
                        break;
                }

                return true;
            }

            return false;
        }
    }

    [Serializable]
    public class ChangeValueModifierStatTypes : IChangeModifier
    {
        [SerializeField]
        private StatType m_StatTypeToReplaceTargetStatTypeWith;
        public bool ChangeModifier(IStatModifier modifier)
        {
            ValueStatModifier cast = modifier as ValueStatModifier;
            if (cast != null)
            {
                cast.TargetStatType = m_StatTypeToReplaceTargetStatTypeWith;

                return true;
            }

            return false;
        }
    }

    [Serializable]
    public class ChangeValueModifierStatOperation : IChangeModifier
    {
        [SerializeField]
        private StatOperation m_StatOperation;
        public bool ChangeModifier(IStatModifier modifier)
        {
            ValueStatModifier cast = modifier as ValueStatModifier;
            if (cast != null)
            {
                cast.TargetStatData.StatOperation = m_StatOperation;

                return true;
            }

            return false;
        }
    }

    [Serializable]
    public class ChangeValueModifierStatVariableToOperate : IChangeModifier
    {
        [SerializeField]
        private StatVariable m_StatVariable;
        public bool ChangeModifier(IStatModifier modifier)
        {
            ValueStatModifier cast = modifier as ValueStatModifier;
            if (cast != null)
            {
                cast.TargetStatData.StatVariable = m_StatVariable;
                return true;
            }

            return false;
        }
    }

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

