using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Core
{
    public interface ITimerType
    {
        public float IncreaseTime(float currentTime);
    }

    [Serializable]
    public class DeltaSeconds_TimerType : ITimerType
    {
        public float IncreaseTime(float currentTime)
        {
            return currentTime + Time.deltaTime;
        }
    }
    [Serializable]
    public class UnscaledDeltaSeconds_TimerType : ITimerType
    {
        public float IncreaseTime(float currentTime)
        {
            return currentTime + Time.unscaledDeltaTime;
        }
    }

    [Serializable]
    public class FixedDeltaSeconds_TimerType : ITimerType
    {
        public float IncreaseTime(float currentTime)
        {
            return currentTime + Time.fixedDeltaTime;
        }
    }

    [Serializable]
    public class UnscaledFixedDeltaSeconds_TimerType : ITimerType
    {
        public float IncreaseTime(float currentTime)
        {
            return currentTime + Time.fixedUnscaledDeltaTime;
        }
    }

    [Serializable]
    public class TickNumber_TimerType : ITimerType
    {
        public float IncreaseTime(float currentTime)
        {
            return currentTime + 1;
        }
    }

    [Serializable]
    public class TimeTranspired_TimerType : ITimerType
    {
        public float IncreaseTime(float currentTime)
        {
            return Time.time;
        }
    }

    [Serializable]
    public class SimpleTimer 
    {
        [OdinSerialize, ShowInInspector]
        private ITimerType m_TimerType;
        float m_MaxTime = 0;
        float m_CurrentTime = 0;

        public SimpleTimer() { }
        public SimpleTimer(SimpleTimer other)
        {
            m_CurrentTime = other.m_CurrentTime;
            m_MaxTime = other.m_MaxTime;
            m_TimerType = other.m_TimerType;
        }

        public bool IsFinished()
        {
            return m_CurrentTime >= m_MaxTime;
        }

        public void ResetTimer()
        {
            m_CurrentTime = 0;
        }

        public bool SetMaxTime(float max)
        {
            if (max > 0)
            {
                m_MaxTime = max;

                return true;
            }

            return false;
        }

        public void Update()
        {
            m_CurrentTime = m_TimerType.IncreaseTime(m_CurrentTime);
        }
    }
}
