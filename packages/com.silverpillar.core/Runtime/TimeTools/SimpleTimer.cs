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
    public class TimerType_CachedScore : ICachedScore
    {
        [OdinSerialize, ShowInInspector]
        private ITimerType m_TimerType = new DeltaSeconds_TimerType();

        private GameObject m_GameObject;
        private float m_CurrentTime;

        public ICachedScore Clone()
        {
            return new TimerType_CachedScore
            {
                m_TimerType = m_TimerType,
                m_GameObject = m_GameObject,
                m_CurrentTime = m_CurrentTime
            };
        }

        public GameObject GetGameObject()
        {
            return m_GameObject;
        }

        public bool SetGameObject(GameObject self)
        {
            m_GameObject = self;
            return true;
        }

        public float CalculateScore()
        {
            if (m_TimerType == null)
            {
                Debug.LogError($"{nameof(TimerType_CachedScore)} could not calculate score because no {nameof(ITimerType)} was assigned.");
                return m_CurrentTime;
            }

            m_CurrentTime = m_TimerType.IncreaseTime(m_CurrentTime);
            return m_CurrentTime;
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
