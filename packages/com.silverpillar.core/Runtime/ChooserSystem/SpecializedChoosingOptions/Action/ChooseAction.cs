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
        public enum WhatToExecuteFirst
        {
            ChosenActions,
            ActionsToAlwaysChoose
        }

        [SerializeField]
        private WhatToExecuteFirst m_WhatToExecuteFirst;

        [OdinSerialize, ShowInInspector]
        private List<IAction> ActionsToAlwaysChoose;

        [OdinSerialize, ShowInInspector]
        private IChooseData<IAction> m_Chooser;
        public IChooseData<IAction> Chooser => m_Chooser;
        private DataFromChoosing<IAction> m_DataFromChoosing;
        private GameObject m_Self;
        private bool m_InitializedCorrectly = false;

        public ChooseAction() { }
        public ChooseAction(ChooseAction other)
        {
            m_Chooser = other.Chooser.Clone();

            m_DataFromChoosing.Clear();

            for (int i = 0; i < other.m_DataFromChoosing.ChosenData.Count; i++)
            {
                m_DataFromChoosing.AddChosen(other.m_DataFromChoosing.ChosenData[i].Clone());
            }

            for (int i = 0; i < other.m_DataFromChoosing.NotChosenData.Count; i++)
            {
                m_DataFromChoosing.AddNotChosen(other.m_DataFromChoosing.ChosenData[i].Clone());

            }

            if (other.ActionsToAlwaysChoose != null)
            {
                for (int i = 0; i < other.ActionsToAlwaysChoose.Count; i++)
                {
                    ActionsToAlwaysChoose.Add(other.ActionsToAlwaysChoose[i].Clone());
                }
            }
        }

        public void Choose()
        {
            if (m_InitializedCorrectly)
            {
                m_DataFromChoosing = m_Chooser.ChooseData();
            }
        }

        public IAction Clone()
        {
            return new ChooseAction(this);
        }

        public void StartAction()
        {
            switch (m_WhatToExecuteFirst)
            {
                case WhatToExecuteFirst.ChosenActions:

                    StartChosenActions();
                    StartForcedActions();

                    break;
                case WhatToExecuteFirst.ActionsToAlwaysChoose:

                    StartForcedActions();
                    StartChosenActions();

                    break;
                default:
                    break;
            }
        }

        public void UpdateAction()
        {
            switch (m_WhatToExecuteFirst)
            {
                case WhatToExecuteFirst.ChosenActions:

                    UpdateChosenActions();
                    UpdateForcedActions();

                    break;
                case WhatToExecuteFirst.ActionsToAlwaysChoose:

                    UpdateForcedActions();
                    UpdateChosenActions();

                    break;
                default:
                    break;
            }
        }
        public void EndAction()
        {
            switch (m_WhatToExecuteFirst)
            {
                case WhatToExecuteFirst.ChosenActions:

                    EndChosenActions();
                    EndForcedActions();

                    break;
                case WhatToExecuteFirst.ActionsToAlwaysChoose:

                    EndForcedActions(); 
                    EndChosenActions();

                    break;
                default:
                    break;
            }
        }

        private void StartChosenActions()
        {
            for (int i = 0; i < m_DataFromChoosing.ChosenData.Count; i++)
            {
                m_DataFromChoosing.ChosenData[i]?.StartAction();
            }
        }

        private void StartForcedActions()
        {
            if (ActionsToAlwaysChoose != null)
            {
                for (int i = 0; i < ActionsToAlwaysChoose.Count; i++)
                {
                    ActionsToAlwaysChoose[i]?.StartAction();
                }
            }
        }

        private void UpdateChosenActions()
        {
            for (int i = 0; i < m_DataFromChoosing.ChosenData.Count; i++)
            {
                m_DataFromChoosing.ChosenData[i]?.UpdateAction();
            }
        }

        private void UpdateForcedActions()
        {
            if (ActionsToAlwaysChoose != null)
            {
                for (int i = 0; i < ActionsToAlwaysChoose.Count; i++)
                {
                    ActionsToAlwaysChoose[i]?.UpdateAction();
                }
            }
        }

        private void EndChosenActions()
        {
            for (int i = 0; i < m_DataFromChoosing.ChosenData.Count; i++)
            {
                m_DataFromChoosing.ChosenData[i]?.EndAction();
            }
        }

        private void EndForcedActions()
        {
            if (ActionsToAlwaysChoose != null)
            {
                for (int i = 0; i < ActionsToAlwaysChoose.Count; i++)
                {
                    ActionsToAlwaysChoose[i]?.EndAction();
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

            for (int i = 0; i < m_DataFromChoosing.ChosenData.Count; i++)
            {
                m_InitializedCorrectly &= m_DataFromChoosing.ChosenData[i] == null ? false : m_DataFromChoosing.ChosenData[i].SetGameObject(gameObj);
            }
            if (ActionsToAlwaysChoose != null)
            {
                for (int i = 0; i < ActionsToAlwaysChoose.Count; i++)
                {
                    m_InitializedCorrectly &= ActionsToAlwaysChoose[i].SetGameObject(gameObj);
                }
            }

            return m_InitializedCorrectly;
        }

    }
}
