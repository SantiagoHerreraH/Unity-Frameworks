using Codice.CM.Triggers;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class ChooseCachedGameAction : ICachedGameAction, IChoose
    {
        [OdinSerialize, ShowInInspector]
        private IChooseData<ICachedGameAction> m_Chooser;
        public IChooseData<ICachedGameAction> Chooser => m_Chooser;
        private List<ICachedGameAction> m_ChosenActions;
        private GameObject m_Self;
        private bool m_InitializedCorrectly = false;

        public ChooseCachedGameAction() { }
        public ChooseCachedGameAction(ChooseCachedGameAction other)
        {
            m_Chooser = other.Chooser.Clone();

            if (other.m_ChosenActions != null)
            {
                if (m_ChosenActions == null)
                {
                    m_ChosenActions = new();
                }

                for (int i = 0; i < other.m_ChosenActions.Count; i++)
                {
                    m_ChosenActions.Add(other.m_ChosenActions[i].Clone());
                }
            }
        }

        public void Choose()
        {
            m_ChosenActions = m_Chooser.ChooseData();
        }

        public ICachedGameAction Clone()
        {
            return new ChooseCachedGameAction(this);
        }

        public void Execute()
        {
            if (m_ChosenActions == null || m_ChosenActions.Count == 0)
            {
                return;
            }

            for(int i = 0;i < m_ChosenActions.Count;i++)
            {
                m_ChosenActions[i]?.Execute();
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
    }
}
