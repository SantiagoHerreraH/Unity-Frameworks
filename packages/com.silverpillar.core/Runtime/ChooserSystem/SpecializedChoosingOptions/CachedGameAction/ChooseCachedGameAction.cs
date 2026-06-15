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
        public enum WhichActionsToExecuteFirst
        {
            ChosenActions,
            ActionsToAlwaysExecute
        }

        public enum ChosenActionsProtocolOnChoose
        {
            CloneActionsAndAddThemToChosen,
            CloneActionsAndSetThemToChosen,
            SetActionsToChosenWithoutCloning
        }
        [Title("Execution Settings")]
        [SerializeField]
        private WhichActionsToExecuteFirst m_WhichActionsToExecuteFirst;

        [Title("Actions To Always Execute")]
        [OdinSerialize, ShowInInspector]
        private List<ICachedGameAction> m_ActionsToAlwaysExecute;

        [Title("Actions To Choose")]
        [SerializeField]
        private ChosenActionsProtocolOnChoose m_ChosenActionsProtocolOnChoose;
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

            if (other.m_ActionsToAlwaysExecute != null)
            {
                if (m_ActionsToAlwaysExecute == null)
                {
                    m_ActionsToAlwaysExecute = new();
                }

                for (int i = 0; i < other.m_ActionsToAlwaysExecute.Count; i++)
                {
                    m_ActionsToAlwaysExecute.Add(other.m_ActionsToAlwaysExecute[i].Clone());
                }
            }

            m_WhichActionsToExecuteFirst = other.m_WhichActionsToExecuteFirst;
            m_ChosenActionsProtocolOnChoose = other.m_ChosenActionsProtocolOnChoose;
        }

        public void Choose()
        {
            var chosenActions = m_Chooser.ChooseData();

            switch (m_ChosenActionsProtocolOnChoose)
            {
                case ChosenActionsProtocolOnChoose.CloneActionsAndAddThemToChosen:


                    m_ChosenActions ??= new();

                    for (int i = 0; i < chosenActions.Count; ++i)
                    {
                        m_ChosenActions.Add(chosenActions[i].Clone());
                    }

                    break;
                case ChosenActionsProtocolOnChoose.CloneActionsAndSetThemToChosen:

                    m_ChosenActions ??= new();
                    m_ChosenActions.Clear();

                    for (int i = 0; i < chosenActions.Count; ++i)
                    {
                        m_ChosenActions.Add(chosenActions[i].Clone());
                    }

                    break;
                case ChosenActionsProtocolOnChoose.SetActionsToChosenWithoutCloning:
                    m_ChosenActions = chosenActions;
                    break;
                default:
                    break;
            }
        }

        public ICachedGameAction Clone()
        {
            return new ChooseCachedGameAction(this);
        }

        public void Execute()
        {
            switch (m_WhichActionsToExecuteFirst)
            {
                case WhichActionsToExecuteFirst.ChosenActions:
                    ExecuteChosen();
                    ExecuteForced();
                    break;
                case WhichActionsToExecuteFirst.ActionsToAlwaysExecute:
                    ExecuteForced();
                    ExecuteChosen();
                    break;
                default:
                    break;
            }
        }

        private void ExecuteChosen()
        {
            if (m_ChosenActions == null || m_ChosenActions.Count == 0)
            {
                return;
            }

            for (int i = 0; i < m_ChosenActions.Count; i++)
            {
                m_ChosenActions[i]?.Execute();
            }
        }

        private void ExecuteForced()
        {
            if (m_ActionsToAlwaysExecute == null || m_ActionsToAlwaysExecute.Count == 0)
            {
                return;
            }

            for (int i = 0; i < m_ActionsToAlwaysExecute.Count; i++)
            {
                m_ActionsToAlwaysExecute[i]?.Execute();
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
