using System;
using UnityEngine;

namespace Pillar
{
    public enum StatVariable
    {
        Current,
        Default,
        MaxLimit,
        MinLimit
    }
    public class Stat
    {
        public Stat()
        {
        }
        public Stat(float currentStat, float defaultStat, float minLimitStat, float maxLimitStat)
        {
            CurrentStat = new StatValue(currentStat);
            DefaultStat = new StatValue(defaultStat);
            MinLimitStat = new StatValue(minLimitStat);
            MaxLimitStat = new StatValue(maxLimitStat);
        }

        public class StatValue
        {
            public StatValue(float value)
            {
                Value = value;
            }
            public float Value { get; private set; }

            public void Set(float value)
            {
                Value = value;
            }

            public void Modify(StatOperation statOperation, float value)
            {
                switch (statOperation)
                {
                    case StatOperation.Add:

                        Value += value;

                        break;
                    case StatOperation.Subtract:

                        Value -= value;
                        break;
                    case StatOperation.Divide:

                        Value /= value;
                        break;
                    case StatOperation.Multiply:

                        Value *= value;

                        break;
                    default:
                        break;
                }
            }
        }

        public StatValue CurrentStat;
        public StatValue DefaultStat;
        public StatValue MinLimitStat;
        public StatValue MaxLimitStat;

        //make a variable to change astats based on inputs -> also take into account the stat modification changer

        public Action<float, float> OnCurrentStatChange;  //past value, new value
        public Action<float, float> OnDefaultStatChange;  //past value, new value
        public Action<float, float> OnMinLimitStatChange; //past value, new value
        public Action<float, float> OnMaxLimitStatChange; //past value, new value

        public void SetStat(float value, StatVariable statVar)
        {
            float pastValue;
            switch (statVar)
            {
                case StatVariable.Current:

                    pastValue = CurrentStat.Value;
                    CurrentStat.Set(Mathf.Clamp(value, MinLimitStat.Value, MaxLimitStat.Value));
                    OnCurrentStatChange.Invoke(pastValue, CurrentStat.Value);

                    break;
                case StatVariable.Default:

                    pastValue = DefaultStat.Value;
                    DefaultStat.Set(Mathf.Clamp(value, MinLimitStat.Value, MaxLimitStat.Value));
                    OnDefaultStatChange.Invoke(pastValue, DefaultStat.Value);

                    break;
                case StatVariable.MaxLimit:

                    pastValue = MaxLimitStat.Value;
                    MaxLimitStat.Set(Mathf.Clamp(value, StatConfiguration.Instance.MinStatValue, StatConfiguration.Instance.MaxStatValue));
                    OnMaxLimitStatChange.Invoke(pastValue, MaxLimitStat.Value);

                    break;
                case StatVariable.MinLimit:

                    pastValue = MinLimitStat.Value;
                    MinLimitStat.Set(Mathf.Clamp(value, StatConfiguration.Instance.MinStatValue, StatConfiguration.Instance.MaxStatValue));
                    OnMinLimitStatChange.Invoke(pastValue, MinLimitStat.Value);

                    break;
                default:
                    break;
            }
        }

        public float GetStat(StatVariable statVar)
        {
            switch (statVar)
            {
                case StatVariable.Current:
                    return CurrentStat.Value;

                case StatVariable.Default:
                    return DefaultStat.Value;

                case StatVariable.MaxLimit:
                    return MaxLimitStat.Value;

                case StatVariable.MinLimit:
                    return MinLimitStat.Value;
                default:
                    break;
            }

            return 0;
        }

        public void ModifyStat(float value, StatOperation statOperation, StatVariable statVar)
        {
            float newValue = 0;
            float pastValue = 0;

            switch (statVar)
            {
                case StatVariable.Current:

                    pastValue = CurrentStat.Value;
                    CurrentStat.Modify(statOperation, value);
                    CurrentStat.Set(Mathf.Clamp(newValue, MinLimitStat.Value, MaxLimitStat.Value));
                    OnCurrentStatChange.Invoke(pastValue, CurrentStat.Value);

                    break;
                case StatVariable.Default:

                    pastValue = DefaultStat.Value;
                    DefaultStat.Modify(statOperation, value);
                    DefaultStat.Set(Mathf.Clamp(newValue, MinLimitStat.Value, MaxLimitStat.Value));
                    OnDefaultStatChange.Invoke(pastValue, DefaultStat.Value);

                    break;
                case StatVariable.MaxLimit:

                    pastValue = MaxLimitStat.Value;
                    MaxLimitStat.Modify(statOperation, value);
                    MaxLimitStat.Set(Mathf.Clamp(newValue, 0, 100));
                    OnMaxLimitStatChange.Invoke(pastValue, MaxLimitStat.Value);

                    break;
                case StatVariable.MinLimit:

                    pastValue = MinLimitStat.Value;
                    MinLimitStat.Modify(statOperation, value);
                    MinLimitStat.Set(Mathf.Clamp(newValue, 0, 100));
                    OnMinLimitStatChange.Invoke(pastValue, MinLimitStat.Value);

                    break;
                default:
                    break;
            }
        }
    }
}
