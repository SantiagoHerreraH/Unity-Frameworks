using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class ChooseAction : IAction, IChoose
    {
        [OdinSerialize, ShowInInspector]
        private IChooseData<IAction> m_Chooser;
        public IChooseData<IAction> Chooser => m_Chooser;
        private List<IAction> m_ChosenActions;
        private GameObject m_Self;
        private bool m_InitializedCorrectly = false;

        public ChooseAction() { }
        public ChooseAction(ChooseAction other)
        {
            m_Chooser = other.Chooser.Clone();

            if (m_ChosenActions != null)
            {
                for (int i = 0; i < other.m_ChosenActions.Count; i++)
                {
                    m_ChosenActions.Add(other.m_ChosenActions[i].Clone());
                }
            }
        }

        public void Choose()
        {
            if (m_InitializedCorrectly)
            {
                m_ChosenActions = m_Chooser.ChooseData();
            }
        }

        public IAction Clone()
        {
            return new ChooseAction(this);
        }

        public void EndAction()
        {
            if (m_ChosenActions != null)
            {
                for (int i = 0; i < m_ChosenActions.Count; i++)
                {
                    m_ChosenActions[i]?.EndAction();
                }
            }
        }

        public GameObject GetGameObject()
        {
            return m_Self;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            m_InitializedCorrectly = true;
            m_Self = gameObj;
            m_InitializedCorrectly &= m_Self != null;

            m_InitializedCorrectly &= m_Chooser == null ? false : m_Chooser.SetGameObject(gameObj);

            if (m_ChosenActions != null)
            {
                for (int i = 0; i < m_ChosenActions.Count; i++)
                {
                    m_InitializedCorrectly &= m_ChosenActions[i] == null ? false : m_ChosenActions[i].SetGameObject(gameObj);
                }
            }

            return m_InitializedCorrectly;
        }

        public void StartAction()
        {
            if (m_ChosenActions != null)
            {
                for (int i = 0; i < m_ChosenActions.Count; i++)
                {
                    m_ChosenActions[i]?.StartAction();
                }
            }
        }

        public void UpdateAction()
        {
            if (m_ChosenActions != null)
            {
                for (int i = 0; i < m_ChosenActions.Count; i++)
                {
                    m_ChosenActions[i]?.UpdateAction();
                }
            }
        }
    }
}
