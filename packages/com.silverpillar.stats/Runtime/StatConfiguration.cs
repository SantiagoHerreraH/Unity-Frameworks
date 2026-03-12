using Sirenix.Utilities;
using UnityEngine;

namespace Pillar
{
    [GlobalConfig("Assets/Resources/Stats")]
    public class StatConfiguration : GlobalConfig<StatConfiguration>
    {
        [SerializeField]
        private float m_MinStatValue = 0;
        [SerializeField]
        private float m_MaxStatValue = 100;

        public float MinStatValue { get { return m_MinStatValue; } }
        public float MaxStatValue { get { return m_MaxStatValue; } }
    }
}