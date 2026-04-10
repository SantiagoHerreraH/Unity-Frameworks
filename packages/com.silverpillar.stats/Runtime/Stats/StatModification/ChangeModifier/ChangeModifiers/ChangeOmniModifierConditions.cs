
using SilverPillar.Core;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Stats
{
    [Serializable]
    public class ChangeOmniModifierConditions : IChangeModifier
    {
        public enum Operation
        {
            AddNew,
            ReplaceWithNew
        }

        [SerializeField]
        public Operation m_Operation;
        [OdinSerialize]
        public InteractionConditionGroup m_NewConditionGroup = new();
        public bool ChangeModifier(IStatModifier modifier)
        {
            OmniStatModifier cast = modifier as OmniStatModifier;
            if (cast != null)
            {
                switch (m_Operation)
                {
                    case Operation.AddNew:
                        cast.Conditions.Add(m_NewConditionGroup);
                        break;
                    case Operation.ReplaceWithNew:
                        cast.Conditions = m_NewConditionGroup.Clone() as InteractionConditionGroup;
                        break;
                    default:
                        break;
                }

                return true;
            }

            return false;
        }
    }
}