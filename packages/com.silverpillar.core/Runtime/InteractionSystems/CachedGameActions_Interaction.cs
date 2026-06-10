using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class CachedGameActions_Interaction : IInteraction
    {
        public enum TargetTypes
        {
            Self,
            Target,
            Custom
        }

        [Serializable]
        public struct Data
        {
            public TargetTypes ActionOnWho;

            [ShowIf(nameof(ActionOnWho), TargetTypes.Custom)]
            public GameObject Custom;

            [OdinSerialize, ShowInInspector]
            public ICachedGameAction CachedGameAction;
        }

        [OdinSerialize, ShowInInspector]
        private List<Data> m_Actions = new();

        private GameObject m_Self;

        public IInteraction Clone()
        {
            CachedGameActions_Interaction clone = new CachedGameActions_Interaction();

            clone.m_Self = m_Self;
            clone.m_Actions = new();

            if (m_Actions != null)
            {
                foreach (Data data in m_Actions)
                {
                    Data clonedData = new Data
                    {
                        ActionOnWho = data.ActionOnWho,
                        Custom = data.Custom,
                        CachedGameAction = data.CachedGameAction != null
                            ? data.CachedGameAction.Clone()
                            : null
                    };

                    clone.m_Actions.Add(clonedData);
                }
            }

            return clone;
        }

        public GameObject GetSelf()
        {
            return m_Self;
        }

        public void Interact(GameObject target)
        {
            if (m_Actions == null)
                return;

            for (int i = 0; i < m_Actions.Count; i++)
            {
                if (m_Actions[i].CachedGameAction == null)
                    continue;

                GameObject actionTarget = GetTarget(m_Actions[i].ActionOnWho, m_Actions[i].Custom, target);

                if (actionTarget == null)
                    continue;

                m_Actions[i].CachedGameAction.SetGameObject(actionTarget);
                m_Actions[i].CachedGameAction.Execute();
            }
        }

        public bool SetSelf(GameObject self)
        {
            m_Self = self;

            if (m_Actions == null)
                return m_Self != null;

            bool allGood = m_Self != null;

            foreach (Data data in m_Actions)
            {
                if (data.CachedGameAction == null)
                {
                    allGood = false;
                    continue;
                }

                GameObject actionTarget = GetTarget(data.ActionOnWho, data.Custom, null);

                if (actionTarget != null)
                    allGood &= data.CachedGameAction.SetGameObject(actionTarget);
            }

            return allGood;
        }

        private GameObject GetTarget(TargetTypes targetTypes, GameObject custom, GameObject target)
        {
            switch (targetTypes)
            {
                case TargetTypes.Self:
                    return m_Self;

                case TargetTypes.Target:
                    return target;

                case TargetTypes.Custom:
                    return custom;

                default:
                    return m_Self;
            }
        }
    }
}