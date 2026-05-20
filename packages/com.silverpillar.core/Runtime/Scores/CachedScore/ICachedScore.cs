using System;
using UnityEngine;

namespace SilverPillar.Core
{
#nullable enable
    public interface ICachedScore
    {
        public ICachedScore Clone();
        public GameObject? GetGameObject();
        public bool SetGameObject(GameObject self);
        public float CalculateScore();
    }

    [Serializable]
    public class Constant_CachedScore : ICachedScore
    {
        [SerializeField]
        private float m_Value;

        public Constant_CachedScore() { }
        public Constant_CachedScore(float value) 
        {
            m_Value = value;
        }

        public Constant_CachedScore(Constant_CachedScore value)
        {
            m_Value = value.m_Value;
        }

        public float CalculateScore()
        {
            return m_Value;
        }

        public ICachedScore Clone()
        {
            return new Constant_CachedScore { m_Value = this.m_Value };
        }

        public GameObject? GetGameObject()
        {
            return null;
        }

        public bool SetGameObject(GameObject self)
        {
            return true;
        }
    }

}

