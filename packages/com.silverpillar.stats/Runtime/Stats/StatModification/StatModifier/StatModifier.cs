using SilverPillar.Core;
using Sirenix.Serialization;
using UnityEngine;

namespace SilverPillar.Stats
{
    [CreateAssetMenu(fileName = "StatModifier", menuName = "SilverPillar/Stats/StatModifier")]
    public class StatModifier : SaveableScriptableObject
    {
        [OdinSerialize]
        private IStatModifier m_StatModifier;

        public IStatModifier Get() { return m_StatModifier; }
    }
}
