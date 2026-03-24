using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SilverPillar.Core
{
    [CreateAssetMenu(fileName = "CachedScore", menuName = "SilverPillar/Core/Score")]
    public class CachedScore : SaveableScriptableObject, ICachedScore
    {
        [SerializeField]
        private ICachedScore m_Score = null;

        public ICachedScore Clone(GameObject gameObj)
        {
            var clone = m_Score.Clone();
            clone.SetGameObject(gameObj);
            return clone;
        }
        public ICachedScore Clone()
        {
            return m_Score.Clone();
        }

        public float CalculateScore()
        {
            return m_Score.CalculateScore();
        }

        public GameObject GetGameObject()
        {
            return m_Score.GetGameObject();
        }

        public bool SetGameObject(GameObject self)
        {
            return m_Score.SetGameObject(self);
        }
    }
}

