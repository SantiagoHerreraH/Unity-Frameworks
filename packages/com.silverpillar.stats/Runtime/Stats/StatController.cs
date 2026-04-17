using JetBrains.Annotations;
using SilverPillar.Core;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

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

    [Serializable]
    public class StatModifierFactoryData
    {
        [OdinSerialize, ShowInInspector]
        private List<IStatModifierFactory> m_StatModifierFactories = new();
        public List<IStatModifierFactory> StatModifierFactories { get { return m_StatModifierFactories; } }
        [ReadOnly, ShowInInspector]
        private Dictionary<IStatModifierFactory, List<IStatModifier>> m_StatModifierFactory_To_StatModifiers = new();
        private List<IStatModifier> m_StatModifiersCreatedByFactories = new();
        public List<IStatModifier> StatModifiersCreatedByFactories { get { return m_StatModifiersCreatedByFactories;} }

        public void CreateStatModifiersFromFactories(StatController statController)
        {
            m_StatModifiersCreatedByFactories.Clear();
            m_StatModifierFactory_To_StatModifiers.Clear();

            foreach (var factory in m_StatModifierFactories)
            {
                List<IStatModifier> statModifiers = factory.CreateInstances(statController);

                m_StatModifierFactory_To_StatModifiers.Add(factory, statModifiers);
                m_StatModifiersCreatedByFactories.AddRange(statModifiers);
            }
        }

        public void AddStatModifierFactory(StatController statController, IStatModifierFactory factory)
        {
            if (!m_StatModifierFactory_To_StatModifiers.ContainsKey(factory))
            {
                m_StatModifierFactories.Add(factory);

                List<IStatModifier> statModifiers = factory.CreateInstances(statController);

                m_StatModifierFactory_To_StatModifiers.Add(factory, statModifiers);
                m_StatModifiersCreatedByFactories.AddRange(statModifiers);
            }
        }

        public void RemoveStatModifierFactory(StatController statController, IStatModifierFactory factory)
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
    }

    [Serializable]
    public struct StatSetting
    {
        public StatType StatType;
        [Range(0,100)]
        public float Value;
        
        public StatEvent CurrentStatEvents;
        public StatEvent DefaultStatEvents;
        public StatEvent MinLimitStatEvents;
        public StatEvent MaxLimitStatEvents;

        public void Apply(StatController statController)
        {
            statController.CreateStatType(StatType, Value);
            statController.SubscribeOnCurrentStatChange(StatType, TriggerCurrentStatChange);
            statController.SubscribeOnDefaultStatChange(StatType, TriggerDefaultStatChange);
            statController.SubscribeOnMinLimitStatChange(StatType, TriggerMinLimitStatChange);
            statController.SubscribeOnMaxLimitStatChange(StatType, TriggerMaxLimitStatChange);
        }

        private void TriggerCurrentStatChange(float lastValue, float newValue)
        {
            CurrentStatEvents.Trigger(lastValue, newValue);
        }
        private void TriggerDefaultStatChange(float lastValue, float newValue)
        {
            DefaultStatEvents.Trigger(lastValue, newValue);
        }
        private void TriggerMinLimitStatChange(float lastValue, float newValue)
        {
            MinLimitStatEvents.Trigger(lastValue, newValue);
        }
        private void TriggerMaxLimitStatChange(float lastValue, float newValue)
        {
            MaxLimitStatEvents.Trigger(lastValue, newValue);
        }
    }

    [Serializable]
    public struct StatEvent
    {
        public UnityEvent<float> OnStatChange;
        public UnityEvent<float> OnStatDecrease;
        public UnityEvent<float> OnStatIncrease;
        public UnityEvent<float> OnStatWorldMin;
        public UnityEvent<float> OnStatWorldMax;

        public void Trigger(float lastValue, float newValue)
        {

            if (Mathf.Approximately(lastValue, newValue))
            {
                return;
            }

            OnStatChange?.Invoke(newValue);

            if (newValue > lastValue)
            {
                OnStatIncrease?.Invoke(newValue);
            }

            if (newValue < lastValue)
            {
                OnStatDecrease?.Invoke(newValue);
            }

            if (newValue <= StatConfiguration.Instance.MinStatValue)
            {
                OnStatWorldMin?.Invoke(newValue);
            }
            if (newValue >= StatConfiguration.Instance.MaxStatValue)
            {
                OnStatWorldMax?.Invoke(newValue);
            }

        }
    }

    public class StatController : SerializedMonoBehaviour
    {
        [FoldoutGroup("Stats")]
        [SerializeField]
        private bool m_ResetCurrentStatsOnEnable = true;
        [FoldoutGroup("Stats")]
        [SerializeField]
        private List<StatSetting> m_StatSettings = new();

        [FoldoutGroup("Stats")]
        [Button(ButtonSizes.Small)]
        private void PopulateWithAllStatTypes()
        {
            if (m_StatSettings == null)
            {
                m_StatSettings = new List<StatSetting>();
            }

            m_StatSettings = m_StatSettings
            .Where(x => x.StatType != null) 
            .GroupBy(x => x.StatType)       
            .Select(g => g.First())         
            .ToList();

            var currentStatTypes = m_StatSettings.Select(x => x.StatType).ToHashSet();

            var allStatTypes = ScriptableObjectRegistry.Instance.GetAllOfType<StatType>();

            foreach (var statType in allStatTypes)
            {
                if (statType != null && !currentStatTypes.Contains(statType))
                {
                    m_StatSettings.Add(new StatSetting { StatType = statType });
                }
            }
        }


        [FoldoutGroup("Target Modifiers")]
        [Title("Stat Modifiers")]
        [OdinSerialize, ShowInInspector, Tooltip("These modifiers are applied on others.")]
        private List<IStatModifier> m_TargetStatModifiers = new();
        public List<IStatModifier> TargetStatModifiers { get { return m_TargetStatModifiers; } }

        [FoldoutGroup("Target Modifiers")]
        [Title("Stat Modifier Factories")]
        [SerializeField, Tooltip("These modifier factories create target stat modifiers")]
        private StatModifierFactoryData m_TargetStatModifierFactoryData = new();

        [FoldoutGroup("Target Modifiers")]
        [Button(ButtonSizes.Small)]
        private void CreateTargetStatModifiersFromFactories()
        {
            m_TargetStatModifierFactoryData.CreateStatModifiersFromFactories(this);
        }

        [FoldoutGroup("Stat Modifier Operations")]
        [Title("Stat Modifiers")]
        [SerializeField]
        private StatModificationOperationController m_StatModificationOperationController = new();


        [FoldoutGroup("Debug")]
        [SerializeField]
        private bool m_PrintOnStatChange;
        [ReadOnly, OdinSerialize, ShowInInspector]
        private Dictionary<StatType, Stat> m_StatType_To_Stat = new();

        private void Awake()
        {
            CreateStatTypes();
        }

        private void Start()
        {
            CreateTargetStatModifiersFromFactories();
        }

        private void OnEnable()
        {
            if (m_ResetCurrentStatsOnEnable)
            {
                foreach (var item in m_StatSettings)
                {
                    var stat = GetStat(item.StatType);
                    stat.CurrentStat.Set(item.Value);
                }
            }
        }

        public void CreateStatType(StatType statType, float value = 0)
        {
            if (!HasStatType(statType))
            {
                m_StatType_To_Stat.Add(statType, new Stat(value, value, StatConfiguration.Instance.MinStatValue, StatConfiguration.Instance.MaxStatValue));
            }
        }

        private void CreateStatTypes()
        {
            foreach (var statType in m_StatSettings)
            {
                statType.Apply(this);
            }
        }

        #region Stat Modification

        public List<IStatModifierFactory> GetTargetModifierFactories()
        {
            return m_TargetStatModifierFactoryData.StatModifierFactories;
        }

        public void AddTargetStatModifierFactory(IStatModifierFactory factory)
        {
            m_TargetStatModifierFactoryData.AddStatModifierFactory(this, factory);
        }

        public void RemoveTargetStatModifierFactory(IStatModifierFactory factory)
        {

            m_TargetStatModifierFactoryData.RemoveStatModifierFactory(this, factory);
        }

        public void AddStatModifier(IStatModifier statModifier)
        {
            if (!m_TargetStatModifiers.Contains(statModifier))
            {
                m_TargetStatModifiers.Add(statModifier);
                CreateTargetStatModifiersFromFactories();//Can be optimized, but will suffice for now
            }
        }

        public void RemoveStatModifier(IStatModifier statModifier)
        {
            if (m_TargetStatModifiers.Contains(statModifier))
            {
                m_TargetStatModifiers.Remove(statModifier);
                CreateTargetStatModifiersFromFactories();//Can be optimized, but will suffice for now
            }
        }

        public void ModifyTarget(StatController target)
        {
            foreach (var statModifier in m_TargetStatModifiers)
            {
                statModifier.Modify(this, target);
            }

            var factoryModifiers = m_TargetStatModifierFactoryData.StatModifiersCreatedByFactories;

            foreach (var statModifier in factoryModifiers)
            {
                statModifier.Modify(this, target);
            }
        }

        #endregion

        #region Subscribe and Unsubscribe On Stat Change
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
                ModificationType modType = new ModificationType{StatType = statType, StatOperation = statOperation, StatVariable = statVariable};

                value = m_StatModificationOperationController.ModifyIncoming(modType, this, value);
                m_StatType_To_Stat[statType].ModifyStat(value, statOperation, statVariable);

                if (m_PrintOnStatChange)
                {
                    Debug.Log(gameObject.name + "'s" + statType.name + " " + statVariable.ToString() + " stat is " + GetStat(statType, statVariable));
                }
            }
        }

        #endregion
    }
}

