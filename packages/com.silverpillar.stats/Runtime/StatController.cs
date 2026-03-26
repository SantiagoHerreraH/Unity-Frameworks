using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;
using SilverPillar.Core;

namespace SilverPillar.Stats
{

    public enum StatOperation
    {
        Add,
        Subtract,
        Divide,
        Multiply
    }

    [Serializable]
    public struct StatValueRange
    {
        public SO_Ref<StatType> StatTypeToGetValueFrom;
        public StatVariable StatVariable;
        public ValueRange ValueRange;

        public float GetValue(StatController statController)
        {
            return ValueRange.GetValue(statController, StatTypeToGetValueFrom.Get(), StatVariable);
        }
    }

    [Serializable]
    public struct EntityStatIdentity
    {
        [SerializeField]
        public StatType StatType;
        [SerializeField]
        public StatVariable StatVariable;
        [SerializeField]
        public TargetType From;

        public float GetValue(GameObject self, GameObject other)
        {
            StatController statController = null;
            switch (From)
            {
                case TargetType.Self:
                    statController = self.GetComponent<StatController>();
                    break;
                case TargetType.Other:
                    statController = other.GetComponent<StatController>();
                    break;
                default:
                    break;
            }

            if (statController)
            {
                return statController.GetStat(StatType, StatVariable);
            }

            return 0;
        }

        public float GetValue(StatController self, StatController other)
        {
            StatController statController = null;
            switch (From)
            {
                case TargetType.Self:
                    statController = self;
                    break;
                case TargetType.Other:
                    statController = other;
                    break;
                default:
                    break;
            }

            if (statController)
            {
                return statController.GetStat(StatType, StatVariable);
            }

            return 0;
        }

        public bool CanGetValue(StatController self, StatController other)
        {
            StatController statController = null;
            switch (From)
            {
                case TargetType.Self:
                    statController = self;
                    break;
                case TargetType.Other:
                    statController = other;
                    break;
                default:
                    break;
            }

            if (statController)
            {
                return statController.HasStatType(StatType);
            }

            return false;
        }
    }

    [Serializable]
    public struct ValueRange
    {
        public float MinValue;

        public float MaxValue;
        public AnimationCurve InBetween;

        public float GetValue(StatController statController, StatType statType, StatVariable statVar)
        {
            if (statController.HasStatType(statType))
            {
                float defaultPercentage = statController.GetStat(statType, statVar);
                defaultPercentage = InBetween.Evaluate(defaultPercentage / StatConfiguration.Instance.MaxStatValue);
                return ((MaxValue - MinValue) * defaultPercentage) + MinValue;
            }

            return 0;
        }

        public float GetValue(float value)
        {
            float clamped = Mathf.Clamp(value, StatConfiguration.Instance.MinStatValue, StatConfiguration.Instance.MaxStatValue);
            float defaultPercentage = InBetween.Evaluate(clamped / StatConfiguration.Instance.MaxStatValue);
            return ((MaxValue - MinValue) * defaultPercentage) + MinValue;
        }
    }

    public class StatController : MonoBehaviour
    {
        [Title("Stat Modifiers")]
        [OdinSerialize]
        private List<SO_Ref<StatModifier>> m_StatModifiers = new();
        public List<SO_Ref<StatModifier>> StatModifiers { get { return m_StatModifiers; } }

        [Title("Stat Modifier Factories")]
        private List<SO_Ref<StatModifierFactory>> m_StatModifierFactories = new();
        public List<SO_Ref<StatModifierFactory>> StatModifierFactories { get { return m_StatModifierFactories; } }
        [ReadOnly]
        private Dictionary<SO_Ref<StatModifierFactory>, List<IStatModifier>> m_StatModifierFactory_To_StatModifiers = new();
        private List<IStatModifier> m_StatModifiersCreatedByFactories = new();

        [Title("Stat Tags")]
        public HashSet<SO_Ref<StatTag>> StatTags = new();

        private Dictionary<StatType, Stat> m_StatType_To_Stat = new();

        private void Start()
        {
            CreateStatModifiersFromFactories();
        }

        public void CreateStatType(StatType statType)
        {
            if (!HasStatType(statType))
            {
                m_StatType_To_Stat.Add(statType, new());
            }
        }

        #region Stat Modification

        private void CreateStatModifiersFromFactories()
        {
            m_StatModifiersCreatedByFactories.Clear();
            m_StatModifierFactory_To_StatModifiers.Clear();

            foreach (var factory in m_StatModifierFactories)
            {
                List<IStatModifier> statModifiers = factory.Get().Get().CreateInstances(this);

                m_StatModifierFactory_To_StatModifiers.Add(factory, statModifiers);
                m_StatModifiersCreatedByFactories.AddRange(statModifiers);
            }
        }

        public void AddStatModifierFactory(SO_Ref<StatModifierFactory> factory)
        {
            if (!m_StatModifierFactory_To_StatModifiers.ContainsKey(factory))
            {
                m_StatModifierFactories.Add(factory);

                List<IStatModifier> statModifiers = factory.Get().Get().CreateInstances(this);

                m_StatModifierFactory_To_StatModifiers.Add(factory, statModifiers);
                m_StatModifiersCreatedByFactories.AddRange(statModifiers);
            }
        }

        public void RemoveStatModifierFactory(SO_Ref<StatModifierFactory> factory)
        {
            if (m_StatModifierFactory_To_StatModifiers.ContainsKey(factory))
            {
                m_StatModifierFactories.Remove(factory);
                List<IStatModifier> modifiers = m_StatModifierFactory_To_StatModifiers[factory];

                foreach (IStatModifier statModifier in modifiers)
                {
                    m_StatModifiersCreatedByFactories.Remove(statModifier);
                }

                m_StatModifierFactory_To_StatModifiers.Remove(factory);
            }
        }

        public void AddStatModifier(SO_Ref<StatModifier> statModifier)
        {
            if (!m_StatModifiers.Contains(statModifier))
            {
                m_StatModifiers.Add(statModifier);
                CreateStatModifiersFromFactories();//Can be optimized, but will suffice for now
            }
        }

        public void RemoveStatModifier(SO_Ref<StatModifier> statModifier)
        {
            if (m_StatModifiers.Contains(statModifier))
            {
                m_StatModifiers.Remove(statModifier);
                CreateStatModifiersFromFactories();//Can be optimized, but will suffice for now
            }
        }

        public void ModifyTarget(StatController target)
        {
            foreach (var statModifier in m_StatModifiers)
            {
                statModifier.Get().Get().Modify(this, target);
            }

            foreach (var statModifier in m_StatModifiersCreatedByFactories)
            {
                statModifier.Modify(this, target);
            }
        }

        #endregion

        #region Subscribe and Unsubscribe On Root Stat Change
        public void SubscribeOnStatChange(StatType statType, Action<float, float> action, StatVariable statVariable)
        {
            switch (statVariable)
            {
                case StatVariable.Current:
                    SubscribeOnCurrentStatChange(statType, action);
                    break;
                case StatVariable.Default:
                    SubscribeOnDefaultStatChange(statType, action);
                    break;
                case StatVariable.MaxLimit:
                    SubscribeOnMaxLimitStatChange(statType, action);
                    break;
                case StatVariable.MinLimit:
                    SubscribeOnMinLimitStatChange(statType, action);
                    break;
                default:
                    break;
            }
        }
        public void UnsubscribeOnStatChange(StatType statType, Action<float, float> action, StatVariable statVariable)
        {
            switch (statVariable)
            {
                case StatVariable.Current:
                    UnsubscribeOnCurrentStatChange(statType, action);
                    break;
                case StatVariable.Default:
                    UnsubscribeOnDefaultStatChange(statType, action);
                    break;
                case StatVariable.MaxLimit:
                    UnsubscribeOnMaxLimitStatChange(statType, action);
                    break;
                case StatVariable.MinLimit:
                    UnsubscribeOnMinLimitStatChange(statType, action);
                    break;
                default:
                    break;
            }
        }

        public void SubscribeOnCurrentStatChange(StatType statType, Action<float, float> action)
        {
            var statStruct = GetStat(statType);
            if (statStruct != null)
            {
                statStruct.OnCurrentStatChange += action;
            }
        }
        public void SubscribeOnDefaultStatChange(StatType statType, Action<float, float> action)
        {
            var statStruct = GetStat(statType);
            if (statStruct != null)
            {
                statStruct.OnDefaultStatChange += action;
            }
        }
        public void SubscribeOnMinLimitStatChange(StatType statType, Action<float, float> action)
        {
            var statStruct = GetStat(statType);
            if (statStruct != null)
            {
                statStruct.OnMinLimitStatChange += action;
            }
        }
        public void SubscribeOnMaxLimitStatChange(StatType statType, Action<float, float> action)
        {
            var statStruct = GetStat(statType);
            if (statStruct != null)
            {
                statStruct.OnMaxLimitStatChange += action;
            }
        }

        public void UnsubscribeOnCurrentStatChange(StatType statType, Action<float, float> action)
        {
            var statStruct = GetStat(statType);
            if (statStruct != null)
            {
                statStruct.OnCurrentStatChange -= action;
            }
        }
        public void UnsubscribeOnDefaultStatChange(StatType statType, Action<float, float> action)
        {
            var statStruct = GetStat(statType);
            if (statStruct != null)
            {
                statStruct.OnDefaultStatChange -= action;
            }
        }
        public void UnsubscribeOnMinLimitStatChange(StatType statType, Action<float, float> action)
        {
            var statStruct = GetStat(statType);
            if (statStruct != null)
            {
                statStruct.OnMinLimitStatChange -= action;
            }
        }
        public void UnsubscribeOnMaxLimitStatChange(StatType statType, Action<float, float> action)
        {
            var statStruct = GetStat(statType);
            if (statStruct != null)
            {
                statStruct.OnMaxLimitStatChange -= action;
            }
        }
        #endregion

        #region Has and Get Stat
        public bool HasStatType(StatType statType)
        {
            return m_StatType_To_Stat.ContainsKey(statType);
        }

        public float GetCurrentStat(StatType statType)
        {
            CreateStatType(statType);

            if (m_StatType_To_Stat.ContainsKey(statType))
            {
                return m_StatType_To_Stat[statType].CurrentStat.Value;
            }

            return 0;
        }

        public float GetDefaultStat(StatType statType)
        {
            CreateStatType(statType);

            if (m_StatType_To_Stat.ContainsKey(statType))
            {
                return m_StatType_To_Stat[statType].DefaultStat.Value;
            }

            return 0;
        }

        public float GetMaxLimitStat(StatType statType)
        {
            CreateStatType(statType);

            if (m_StatType_To_Stat.ContainsKey(statType))
            {
                return m_StatType_To_Stat[statType].MaxLimitStat.Value;
            }

            return 0;
        }

        public float GetMinLimitStat(StatType statType)
        {
            CreateStatType(statType);

            if (m_StatType_To_Stat.ContainsKey(statType))
            {
                return m_StatType_To_Stat[statType].MinLimitStat.Value;
            }

            return 0;
        }

        public float GetStat(StatType statType, StatVariable statVar)
        {
            
            switch (statVar)
            {
                case StatVariable.Current:
                    return GetCurrentStat(statType);
                case StatVariable.Default:
                    return GetDefaultStat(statType);
                case StatVariable.MaxLimit:
                    return GetMaxLimitStat(statType);
                case StatVariable.MinLimit:
                    return GetMinLimitStat(statType);
                default:
                    Debug.LogError("No Valid stat variable in " + nameof(StatController) + " in gameObject " + gameObject.name);
                    return 0;
            }
        }

        private Stat GetStat(StatType statType)
        {
            if (HasStatType(statType))
            {
                return m_StatType_To_Stat[statType];
            }
            return null;
        }

        #endregion

        #region Stat Modification
        
        public void Modify(StatType statType, float value, StatOperation statOperation, StatVariable statVariable)
        {
            if (HasStatType(statType))
            {
                m_StatType_To_Stat[statType].ModifyStat(value, statOperation, statVariable);
            }
        }

        #endregion
    }
}

