using SilverPillar.Core;
using System;
using UnityEngine;

namespace SilverPillar.Stats
{

    [Serializable]
    public class OmniStatModifierHasInteractionCondition : IStatModifierCondition
    {
        [SerializeField]
        private InteractionCondition m_InteractionCondition = null;

        public bool IsFulfilled(IStatModifier modifier)
        {
            OmniStatModifier cast = modifier as OmniStatModifier;

            if (cast != null)
            {
                var conditions = cast.Conditions.Conditions.ConditionList;
                foreach (var condition in conditions)
                {
                    if (condition == m_InteractionCondition as IInteractionCondition)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
