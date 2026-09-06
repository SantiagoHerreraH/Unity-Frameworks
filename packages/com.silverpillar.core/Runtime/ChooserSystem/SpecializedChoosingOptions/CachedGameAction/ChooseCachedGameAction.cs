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
        private DataFromChoosing<ICachedGameAction> m_DataFromChoosing;
        private GameObject m_Self;
        private bool m_InitializedCorrectly = false;

        public ChooseCachedGameAction() { }
        public ChooseCachedGameAction(ChooseCachedGameAction other)
        {
            m_Chooser = other.Chooser.Clone();

            m_DataFromChoosing.Clear();
            m_DataFromChoosing.Append(other.m_DataFromChoosing);

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
            var choosingData = m_Chooser.ChooseData();

            switch (m_ChosenActionsProtocolOnChoose)
            {
                case ChosenActionsProtocolOnChoose.CloneActionsAndSetThemToChosen:

                    m_DataFromChoosing.Clear();

                    for (int i = 0; i < choosingData.ChosenData.Count; ++i)
                    {
                        m_DataFromChoosing.AddChosen(choosingData.ChosenData[i].Clone());
                    }

                    for (int i = 0; i < choosingData.NotChosenData.Count; ++i)
                    {
                        m_DataFromChoosing.AddNotChosen(choosingData.NotChosenData[i].Clone());
                    }

                    break;
                case ChosenActionsProtocolOnChoose.SetActionsToChosenWithoutCloning:
                    m_DataFromChoosing = choosingData;
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
            for (int i = 0; i < m_DataFromChoosing.ChosenData.Count; i++)
            {
                m_DataFromChoosing.ChosenData[i]?.Execute();
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

            for (int i = 0; i < m_DataFromChoosing.ChosenData.Count; i++)
            {
                m_InitializedCorrectly &= m_DataFromChoosing.ChosenData[i] == null ? false : m_DataFromChoosing.ChosenData[i].SetGameObject(gameObj);
            }

            return m_InitializedCorrectly;
        }
    }
}
