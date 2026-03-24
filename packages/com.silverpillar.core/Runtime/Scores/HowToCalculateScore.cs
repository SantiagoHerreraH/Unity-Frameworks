using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SilverPillar.Core
{
    public enum HowToCalculateScore
    {
        AddAll,
        ChooseHighest,
        ChooseLowest,
        Mean,
        Medium,
        Mode
    }

    public class ScoreTools
    {
        public static float CalculateScore(HowToCalculateScore howToCalculateScore, List<float> values)
        {
            return howToCalculateScore switch
            {
                HowToCalculateScore.AddAll => values.Sum(),
                HowToCalculateScore.ChooseHighest => values.Max(),
                HowToCalculateScore.ChooseLowest => values.Min(),
                HowToCalculateScore.Mean => values.Average(),
                HowToCalculateScore.Medium => Median.Calculate(values),
                HowToCalculateScore.Mode => values.GroupBy(v => v)
                                                  .OrderByDescending(g => g.Count())
                                                  .First().Key,
                _ => 0f
            };
        }

    }
}

