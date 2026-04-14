using System;
using UnityEngine;

namespace SilverPillar.Stats
{
    public enum StatVariable
    {
        Current,
        Default,
        MaxLimit,
        MinLimit
    }

    [Serializable]
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

        [Serializable]
        public class StatValue
        {
            public StatValue(float value)
            {
                m_Value = value;
            }

            [SerializeField]
            private float m_Value;

            public float Value { get { return m_Value; } }

            public void Set(float value)
            {
                m_Value = value;
            }

            public void Modify(StatOperation statOperation, float value)
            {
                switch (statOperation)
                {
                    case StatOperation.Add:

                        m_Value += value;

                        break;
                    case StatOperation.Subtract:

                        m_Value -= value;
                        break;
                    case StatOperation.Divide:

                        m_Value /= value;
                        break;
                    case StatOperation.Multiply:

                        m_Value *= value;

                        break;
                    default:
                        break;
                }
            }
        }

        [SerializeField]
        public StatValue CurrentStat;
        [SerializeField]
        public StatValue DefaultStat;
        [SerializeField]
        public StatValue MinLimitStat;
        [SerializeField]
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
            float pastValue = 0;

            switch (statVar)
            {
                case StatVariable.Current:

                    pastValue = CurrentStat.Value;
                    CurrentStat.Modify(statOperation, value);
                    CurrentStat.Set(Mathf.Clamp(CurrentStat.Value, MinLimitStat.Value, MaxLimitStat.Value));
                    OnCurrentStatChange.Invoke(pastValue, CurrentStat.Value);

                    break;
                case StatVariable.Default:

                    pastValue = DefaultStat.Value;
                    DefaultStat.Modify(statOperation, value);
                    DefaultStat.Set(Mathf.Clamp(DefaultStat.Value, MinLimitStat.Value, MaxLimitStat.Value));
                    OnDefaultStatChange.Invoke(pastValue, DefaultStat.Value);

                    break;
                case StatVariable.MaxLimit:

                    pastValue = MaxLimitStat.Value;
                    MaxLimitStat.Modify(statOperation, value);
                    MaxLimitStat.Set(Mathf.Clamp(MaxLimitStat.Value, StatConfiguration.Instance.MinStatValue, StatConfiguration.Instance.MaxStatValue));
                    OnMaxLimitStatChange.Invoke(pastValue, MaxLimitStat.Value);

                    break;
                case StatVariable.MinLimit:

                    pastValue = MinLimitStat.Value;
                    MinLimitStat.Modify(statOperation, value);
                    MinLimitStat.Set(Mathf.Clamp(MinLimitStat.Value, StatConfiguration.Instance.MinStatValue, StatConfiguration.Instance.MaxStatValue));
                    OnMinLimitStatChange.Invoke(pastValue, MinLimitStat.Value);

                    break;
                default:
                    break;
            }
        }
    }
}
