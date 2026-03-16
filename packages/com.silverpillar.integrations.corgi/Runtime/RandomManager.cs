using MoreMountains.Tools;
using UnityEngine;

namespace SilverPillar.Integrations.Corgi
{
    public enum RandomChoice
    {
        SeededRandom,
        Random
    }

    public class RandomManager : MMSingleton<RandomManager>
    {
        [SerializeField]
        private int m_Seed;
        private System.Random m_RandomGenerator;

        protected override void InitializeSingleton()
        {
            base.InitializeSingleton();
            m_RandomGenerator = new System.Random(m_Seed);
        }

        public int GetRandomInRange(int minInclusive, int maxExclusive)
        {
            return m_RandomGenerator.Next(minInclusive, maxExclusive);
        }

        public float GetRandomInRange(float minInclusive, float maxExclusive)
        {
            return (float)(minInclusive + m_RandomGenerator.NextDouble() * (maxExclusive - minInclusive));
        }
    }
}

