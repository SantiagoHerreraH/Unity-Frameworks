using Sirenix.Serialization;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Pillar.SaveableScore;

namespace Pillar
{
    public enum TargetType
    {
        Self,
        Other
    }
    public interface IScore
    {
        public float CalculateScore(GameObject gameObject);
    }
    public interface IInteractionScore
    {
        public float CalculateScore(GameObject self, GameObject target);
    }

    public class SaveableScore : SaveableScriptableObject
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
        [SerializeField]
        private HowToCalculateScore m_HowToCalculateScore;
        [OdinSerialize]
        private List<IScore> m_Scores = new();

        public float CalculateScore(GameObject gameObj)
        {
            if (m_Scores == null || m_Scores.Count == 0) return 0f;

            // get individual values first
            var values = m_Scores.Select(s => s.CalculateScore(gameObj)).ToList();

            return CalculateScore(m_HowToCalculateScore, values);
        }

        public static float CalculateScore(HowToCalculateScore howToCalculateScore, List<float> values)
        {
            return howToCalculateScore switch
            {
                HowToCalculateScore.AddAll => values.Sum(),
                HowToCalculateScore.ChooseHighest => values.Max(),
                HowToCalculateScore.ChooseLowest => values.Min(),
                HowToCalculateScore.Mean => values.Average(),
                HowToCalculateScore.Medium => GetMedian(values),
                HowToCalculateScore.Mode => values.GroupBy(v => v)
                                                  .OrderByDescending(g => g.Count())
                                                  .First().Key,
                _ => 0f
            };
        }

        private static float GetMedian(List<float> source)
        {
            var sorted = source.OrderBy(n => n).ToList();
            int count = sorted.Count;
            int mid = count / 2;
            return (count % 2 != 0)
                ? sorted[mid]
                : (sorted[mid - 1] + sorted[mid]) / 2f;
        }
    }

    public abstract class SaveableInteractionScore : SaveableScriptableObject, IInteractionScore
    {
        public abstract float CalculateScore(GameObject self, GameObject target);
    }
}

