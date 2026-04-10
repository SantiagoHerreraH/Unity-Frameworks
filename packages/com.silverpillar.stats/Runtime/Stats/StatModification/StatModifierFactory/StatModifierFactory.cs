using Sirenix.Serialization;
using UnityEngine;
using SilverPillar.Core;


namespace SilverPillar.Stats
{
    [CreateAssetMenu(fileName = "StatModifierFactory", menuName = "SilverPillar/Stats/StatModifierFactory")]
    public class StatModifierFactory : SaveableScriptableObject
    {
        [OdinSerialize]
        private IStatModifierFactory m_StatModifierFactory;

        public IStatModifierFactory Get() { return m_StatModifierFactory; }
    }
}

