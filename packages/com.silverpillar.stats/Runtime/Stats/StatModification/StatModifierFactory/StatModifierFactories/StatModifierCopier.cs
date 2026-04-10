using SilverPillar.Core;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SilverPillar.Stats
{

    [Serializable]
    public class StatModifierCopier : IStatModifierFactory
    {
        //Make both stats data available to all stat modifiers
        // make both tag hashsets available to all stat modifiers

        [Title("Copy Modifier if")]
        [SerializeField]
        private StatConditionGroup m_StatConditionsAreFulfilled = new();
        [SerializeField]
        private BoolOperation m_BoolOperation;
        [OdinSerialize, ShowInInspector]
        private ICachedCondition m_CachedCondition;


        [Title("Once copied, Change modifier")]
        [InfoBox("The Modifiers will be pasted on Self.")]
        [OdinSerialize]
        private List<IChangeModifier> m_ModificationsToMakeOnCopiedModifiers = new();

        public StatModifierCopier(StatModifierCopier other)
        {
            m_StatConditionsAreFulfilled = other.m_StatConditionsAreFulfilled;
            m_ModificationsToMakeOnCopiedModifiers = new(other.m_ModificationsToMakeOnCopiedModifiers);
        }

        public IStatModifierFactory Clone()
        {
            return new StatModifierCopier(this);
        }

        public List<IStatModifier> CreateInstances(StatController fromWho)
        {
            if (m_CachedCondition.GetGameObject() != fromWho.gameObject)
            {
                m_CachedCondition.SetGameObject(fromWho.gameObject);
            }

            if (!m_CachedCondition.IsFulfilled())
            {
                return new();
            }

            List<IStatModifier> modifiers = GetModificationsToCopy(fromWho);
            return PasteOn(modifiers);
        }

        private List<IStatModifier> GetModificationsToCopy(StatController self)
        {
            List<IStatModifier> modifications = new List<IStatModifier>();

            foreach (var modifier in self.TargetStatModifiers)
            {
                IStatModifier mod = modifier.Clone();
                if (m_StatConditionsAreFulfilled.IsFulfilled(mod))
                {
                    modifications.Add(mod);
                }
            }

            return modifications;
        }

        private List<IStatModifier> PasteOn(List<IStatModifier> modifiers)
        {
            List<IStatModifier> modedModifiers = new();

            foreach (var modifier in modifiers)
            {
                modedModifiers.Add(modifier.Clone());

                foreach (var modification in m_ModificationsToMakeOnCopiedModifiers)
                {
                    modification.ChangeModifier(modedModifiers.Last());
                }
            }

            return modedModifiers;
        }
    }
}
