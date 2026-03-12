using Sirenix.Serialization;
using UnityEngine;

namespace Pillar
{
    [CreateAssetMenu(fileName = "StatModifier", menuName = "Stats/StatModifierFactory")]
    public class StatModifierFactory : SaveableScriptableObject
    {
        [OdinSerialize]
        private IStatModifierFactory m_StatModifierFactory;

        public IStatModifierFactory Get() { return m_StatModifierFactory; }
    }

    [CreateAssetMenu(fileName = "StatModifier", menuName = "Stats/StatModifier")]
    public class StatModifier : SaveableScriptableObject
    {
        [OdinSerialize]
        private IStatModifier m_StatModifier;

        public IStatModifier Get() { return m_StatModifier; }
    }
}

