using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SilverPillar.Stats
{
    [Serializable]
    public class IsModifierType : IStatModifierCondition
    {
        [OdinSerialize]
        [TypeFilter(nameof(GetTypes))]
        private Type m_ModifierType;

        private IEnumerable<Type> GetTypes()
        {
            return typeof(IStatModifier).Assembly
                .GetTypes()
                .Where(t => typeof(IStatModifier).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);
        }

        public bool IsFulfilled(IStatModifier modifier)
        {
            return modifier != null &&
                   m_ModifierType != null &&
                   m_ModifierType.IsInstanceOfType(modifier);
        }
    }
}
