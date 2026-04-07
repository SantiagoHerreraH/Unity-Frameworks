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
        private float m_Score;

        public float CalculateScore()
        {
            return m_Score;
        }

        public ICachedScore Clone()
        {
            return new Constant_CachedScore { m_Score = this.m_Score };
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

