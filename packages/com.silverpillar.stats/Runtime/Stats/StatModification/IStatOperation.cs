using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace SilverPillar.Stats
{
    public interface IStatOperation
    {
        public float ModifyIncomingValue(StatController controllerThatWantsToBeModified, float incomingModificationValueFromData);

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
            if (m_StatConditions == null)
            {
                return true;
            }

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
            if (m_OrConditions == null)
            {
                return true;
            }

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
            if (StatOperatorsThatModifyIncoming == null)
            {
                return incomingModificationValueFromData;
            }
            foreach (var item in StatOperatorsThatModifyIncoming)
            {
                incomingModificationValueFromData = item.ModifyIncomingValue(controllerThatWantsToBeModified, incomingModificationValueFromData);
            }

            return incomingModificationValueFromData;
        }
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

        public void ModifyIncomingAndApply(StatController target, StatType statType, float modificationValue)
        {
            modificationValue = ModifyIncoming.ModifyIncomingValue(target, modificationValue);

            target.Modify(statType, modificationValue, StatOperation, StatVariable);
        }
    }

    [Serializable]
    public struct ModificationType : IEquatable<ModificationType>
    {
        public StatType StatType;
        public StatOperation StatOperation;
        public StatVariable StatVariable;

        public override bool Equals(object obj)
        {
            return obj is ModificationType other && Equals(other);
        }

        public bool Equals(ModificationType other)
        {
            return StatType == other.StatType &&
               StatOperation == other.StatOperation &&
               StatVariable == other.StatVariable;
        }

        public override int GetHashCode()
        {
            uint objectPart = (uint)(StatType != null ? StatType.GetInstanceID() & 0xFFFF : 0);
            uint enumAPart = (uint)((byte)StatOperation);
            uint enumBPart = (uint)((byte)StatVariable);

            uint hash = (objectPart << 16) | (enumAPart << 8) | enumBPart;
            return hash.GetHashCode();
        }

        
    }

    [Serializable]
    public struct StatModificationOperation
    {
        public ModificationType ModificationType;
        public StatOperationGroup StatOperationGroup;

        public void ModifyIncomingAndApply(StatController target, float modificationValue)
        {
            modificationValue = StatOperationGroup.ModifyIncomingValue(target, modificationValue);

            target.Modify(ModificationType.StatType, modificationValue, ModificationType.StatOperation, ModificationType.StatVariable);
        }
    }

    [Serializable]
    public class StatModificationOperationController
    {
        public struct InternalStatOperationGroup
        {
            public StatOperationGroup Data;
            public int Key;
        }
        public struct Data
        {
            private List<InternalStatOperationGroup> m_Data;
            private int m_CurrentKey;

            public int AddData(StatOperationGroup data)
            {
                m_Data ??= new List<InternalStatOperationGroup>();

                int key = m_CurrentKey;
                m_Data.Add(new InternalStatOperationGroup
                {
                    Key = key,
                    Data = data
                });

                m_CurrentKey++;
                return key;
            }

            public int ChangeData(StatOperationGroup data)
            {
                m_Data ??= new List<InternalStatOperationGroup>();
                m_Data.Clear();
                return AddData(data);
            }

            public bool RemoveData(int key)
            {
                if (m_Data == null)
                    return false;

                int index = m_Data.FindIndex(x => x.Key == key);
                if (index < 0)
                    return false;

                m_Data.RemoveAt(index);
                return true;
            }

            public float ModifyIncoming(StatController target, float modificationValue)
            {
                foreach (var data in m_Data)
                {
                    modificationValue = data.Data.ModifyIncomingValue(target, modificationValue);
                }

                return modificationValue;
            }
        }

        [SerializeField]
        private List<StatModificationOperation> m_StatModificationOperations = new();

        private Dictionary<ModificationType, Data> m_Data = new();

        private bool m_Initialized = false;

        private void Initialize()
        {
            if (!m_Initialized)
            {
                m_Data.Clear();

                foreach (var operation in m_StatModificationOperations)
                {
                    if (!m_Data.ContainsKey(operation.ModificationType))
                    { 
                        m_Data.Add(operation.ModificationType, new());
                    }

                    m_Data[operation.ModificationType].AddData(operation.StatOperationGroup);
                }

                m_Initialized = true;
            }
        }

        public bool HasModificationType(ModificationType modificationType)
        {
            return m_Data.ContainsKey(modificationType);
        }

        public int AddOperation(StatModificationOperation operation)
        {
            if (HasModificationType(operation.ModificationType))
            {
                return m_Data[operation.ModificationType].AddData(operation.StatOperationGroup);
            }

            return -1;
        }

        public int ChangeOperation(StatModificationOperation operation)
        {
            if (HasModificationType(operation.ModificationType))
            {
                return m_Data[operation.ModificationType].ChangeData(operation.StatOperationGroup);
            }

            return -1;
        }

        public bool RemoveModificationType(ModificationType modType)
        {
            return m_Data.Remove(modType);
        }

        public bool RemoveOperation(ModificationType modType, int operationKey)
        {
            if (HasModificationType(modType))
            {
                return m_Data[modType].RemoveData(operationKey);
            }

            return false;
        }

        public float ModifyIncoming(ModificationType modType, StatController controllerThatWantsToBeModified, float incoming)
        {
            Initialize();

            if (HasModificationType(modType))
            {
                return m_Data[modType].ModifyIncoming(controllerThatWantsToBeModified, incoming);
            }

            return incoming;
        }
    }

}
