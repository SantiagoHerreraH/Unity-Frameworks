using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Stats
{
    public interface IStatModifierFactory
    {
        public IStatModifierFactory Clone();
        public List<IStatModifier> CreateInstances(StatController fromWho);
    }

}
