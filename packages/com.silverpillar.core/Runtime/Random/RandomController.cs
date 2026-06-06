using Sirenix.OdinInspector;
using Sirenix.Utilities;
using System;
using UnityEngine;

namespace SilverPillar.Core
{
    [GlobalConfig("Assets/Resources/SilverPillar")]
    public class RandomController : GlobalConfig<RandomController>
    {
        [Title("Random Settings")]
        [SerializeField]
        private int m_Seed = 12345;
        public int Seed => m_Seed;

        private System.Random m_Random;

        protected override void OnConfigInstanceFirstAccessed()
        {
            InitializeRandom();
        }

        [Button]
        public void Reinitialize()
        {
            InitializeRandom();
        }

        private void InitializeRandom()
        {
            m_Random = new System.Random(m_Seed);
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            EnsureInitialized();
            return m_Random.Next(minInclusive, maxExclusive);
        }

        public float Range(float minInclusive, float maxInclusive)
        {
            EnsureInitialized();

            double value = m_Random.NextDouble();
            return minInclusive + (float)value * (maxInclusive - minInclusive);
        }

        public bool Chance(float probability01)
        {
            EnsureInitialized();
            return Range(0f, 1f) <= Mathf.Clamp01(probability01);
        }

        public void SetSeed(int seed)
        {
            m_Seed = seed;
            InitializeRandom();
        }

        /// <summary>
        /// Returns a deterministic random int.
        /// Range: 0 to int.MaxValue - 1.
        /// </summary>
        public int GetRandomInt()
        {
            EnsureInitialized();
            return m_Random.Next();
        }

        /// <summary>
        /// Returns a deterministic random float.
        /// Range: 0.0f to 1.0f.
        /// </summary>
        public float GetRandomFloat()
        {
            EnsureInitialized();
            return (float)m_Random.NextDouble();
        }

        private void EnsureInitialized()
        {
            if (m_Random == null)
            {
                InitializeRandom();
            }
        }
    }
}