using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class ChooseInteraction : IInteraction, IChoose
    {
        public enum WhatToExecuteFirst
        {
            ChosenInteractions,
            InteractionsToAlwaysExecute
        }

        [SerializeField]
        private WhatToExecuteFirst m_WhatToExecuteFirst;
        [OdinSerialize, ShowInInspector]
        private List<IInteraction> m_InteractionsToAlwaysExecute;
        [OdinSerialize, ShowInInspector]
        private IChooseData<IInteraction> m_Chooser;
        public IChooseData<IInteraction> Chooser => m_Chooser;
        private List<IInteraction> m_ChosenInteractions;
        private GameObject m_Self;
        private bool m_InitializedCorrectly = false;

        public ChooseInteraction() { }
        public ChooseInteraction(ChooseInteraction other)
        {
            m_Chooser = other.Chooser.Clone();

            if (other.m_ChosenInteractions != null)
            {
                if (m_ChosenInteractions == null)
                {
                    m_ChosenInteractions = new();
                }

                for (int i = 0; i < other.m_ChosenInteractions.Count; i++)
                {
                    m_ChosenInteractions.Add(other.m_ChosenInteractions[i].Clone());
                }
            }

            if (other.m_InteractionsToAlwaysExecute != null)
            {
                if (m_InteractionsToAlwaysExecute == null)
                {
                    m_InteractionsToAlwaysExecute = new();
                }

                for (int i = 0; i < other.m_InteractionsToAlwaysExecute.Count; i++)
                {
                    m_InteractionsToAlwaysExecute.Add(other.m_InteractionsToAlwaysExecute[i].Clone());
                }
            }
        }

        public void Choose()
        {
            m_ChosenInteractions = m_Chooser.ChooseData();
        }

        public IInteraction Clone()
        {
            return new ChooseInteraction(this);
        }

        public void Interact(GameObject other)
        {
            switch (m_WhatToExecuteFirst)
            {
                case WhatToExecuteFirst.ChosenInteractions:
                    InteractChosen(other);
                    InteractForced(other);
                    break;
                case WhatToExecuteFirst.InteractionsToAlwaysExecute:
                    InteractForced(other);
                    InteractChosen(other);
                    break;
                default:
                    break;
            }
        }

        private void InteractChosen(GameObject other)
        {
            if (m_ChosenInteractions == null || m_ChosenInteractions.Count == 0)
            {
                return;
            }

            for (int i = 0; i < m_ChosenInteractions.Count; i++)
            {
                m_ChosenInteractions[i]?.Interact(other);
            }

        }

        private void InteractForced(GameObject other)
        {
            if (m_InteractionsToAlwaysExecute == null || m_InteractionsToAlwaysExecute.Count == 0)
            {
                return;
            }

            for (int i = 0; i < m_InteractionsToAlwaysExecute.Count; i++)
            {
                m_InteractionsToAlwaysExecute[i]?.Interact(other);
            }
        }

        public GameObject GetSelf()
        {
            return m_Self;
        }

        public bool SetSelf(GameObject gameObj)
        {
            m_InitializedCorrectly = true;
            m_Self = gameObj;
            m_InitializedCorrectly &= m_Self != null;

            m_InitializedCorrectly &= m_Chooser == null ? false : m_Chooser.SetGameObject(gameObj);

            if (m_ChosenInteractions != null)
            {
                for (int i = 0; i < m_ChosenInteractions.Count; i++)
                {
                    m_InitializedCorrectly &= m_ChosenInteractions[i] == null ? false : m_ChosenInteractions[i].SetSelf(gameObj);
                }
            }

            if (m_InteractionsToAlwaysExecute != null)
            {
                for (int i = 0; i < m_InteractionsToAlwaysExecute.Count; i++)
                {
                    m_InitializedCorrectly &= m_InteractionsToAlwaysExecute[i].SetSelf(gameObj);
                }
            }


            return m_InitializedCorrectly;
        }
    }
}
