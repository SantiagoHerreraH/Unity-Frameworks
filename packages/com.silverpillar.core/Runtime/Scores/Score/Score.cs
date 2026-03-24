using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SilverPillar.Core
{

    [CreateAssetMenu(fileName = "Score", menuName = "SilverPillar/Core/Score")]
    public class Score : SaveableScriptableObject, IScore
    {
        [SerializeField]
        private IScore m_Score = null;

        public float CalculateScore(GameObject gameObj)
        {
            return m_Score.CalculateScore(gameObj);
        }
    }
}

