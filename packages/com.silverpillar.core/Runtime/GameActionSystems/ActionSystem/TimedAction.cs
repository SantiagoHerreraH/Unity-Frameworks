using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class ActionData : IAction
    {
        [OdinSerialize, ShowInInspector]
        private List<IAction> m_Actions = new();

        private GameObject m_Owner;

        public ActionData() { }

        public ActionData(ActionData other)
        {
            if (other == null) return;

            foreach (var action in other.m_Actions)
            {
                this.m_Actions.Add(action?.Clone());
            }
        }

        public IAction Clone()
        {
            return new ActionData(this);
        }


        public void StartAction()
        {
            foreach (var action in m_Actions)
                action.StartAction();
        }

        public void UpdateAction()
        {
            foreach (var action in m_Actions)
                action.UpdateAction();
        }

        public void EndAction()
        {
            foreach (var action in m_Actions)
                action.EndAction();
        }

        public GameObject GetGameObject() => m_Owner;

        public bool SetGameObject(GameObject gameObj)
        {
            m_Owner = gameObj;
            bool allSet = true;

            foreach (var action in m_Actions)
            {
                if (!action.SetGameObject(gameObj))
                    allSet = false;
            }

            return allSet;
        }
    }


    public enum HowToInterpretTime
    {
        InSeconds,
        InTickCount
    }

    [Serializable]
    public struct ActionTime 
    {
        [OdinSerialize, ShowInInspector]
        public ICachedScore Time;

        [SerializeField]
        public HowToInterpretTime HowToInterpretTime;

        public ActionTime(ActionTime other)
        {
            Time = other.Time.Clone();
            HowToInterpretTime = other.HowToInterpretTime;
        }

        public ActionTime Clone()
        {
            return new ActionTime(this);
        }

    }

    [Serializable]
    public class TimedActionData : IAction
    {
        public enum StartWith
        {
            ActionTime,
            WaitTime
        }

        [FoldoutGroup("Action Time")]
        [SerializeField]
        private ActionTime m_ActionTime = new();

        [FoldoutGroup("Wait Time")]
        [SerializeField]
        private bool m_UsesWaitTime = true;
        [FoldoutGroup("Wait Time")]
        [SerializeField, ShowIf(nameof(m_UsesWaitTime))]
        private ActionTime m_WaitTime = new();
        [FoldoutGroup("Wait Time")]
        [SerializeField, ShowIf(nameof(m_UsesWaitTime))]
        private StartWith m_StartWith;


        [FoldoutGroup("When To Stop Action")]
        [SerializeField, Min(-1), Tooltip("-1 is infinite reps")]
        private int m_RepetitionNumber = 0;

        [FoldoutGroup("When To Stop Action")]
        [OdinSerialize, ShowInInspector]
        private ICachedCondition m_ConditionToStop = null;

        [FoldoutGroup("When To Stop Action")]
        [SerializeField]
        private bool m_CallEndActionsWhenTimeActionEnds;

        [FoldoutGroup("Actions")]
        [SerializeField]
        private List<ActionData> m_Actions = new();

        private int m_CurrentTickNumber = 0;
        private int m_MaxTickNumber = 0;

        private float m_CurrentTime = 0f;
        private float m_MaxTime;

        private bool m_IsOnWaitTime;
        private bool m_IsRunning = true;

        private GameObject m_Owner;
        private int m_CurrentRepetitions = -1;//the first loop is repetition 0

        public TimedActionData() { }

        public TimedActionData(TimedActionData other)
        {
            if (other == null) return;
            m_ActionTime = other.m_ActionTime.Clone();
            m_WaitTime = other.m_WaitTime.Clone();
            m_RepetitionNumber = other.m_RepetitionNumber;
            m_ConditionToStop = other.m_ConditionToStop?.Clone();

            foreach (var item in other.m_Actions)
            {
                m_Actions.Add(new ActionData(item));
            }
        }

        public IAction Clone() => new TimedActionData(this);

        public bool IsOnWaitTime()
        {
            return m_UsesWaitTime ? m_IsOnWaitTime : false;
        }

        public bool IsRunning()
        {
            return m_IsRunning;
        }

        public void StartAction()
        {
            m_IsRunning = true;
            switch (m_StartWith)
            {
                case StartWith.ActionTime:
                    m_IsOnWaitTime = false;

                    break;
                case StartWith.WaitTime:
                    m_IsOnWaitTime = true;

                    break;
                default:
                    break;
            }

            RecalculateTime();

            if (!m_IsOnWaitTime)
            {
                StartAllActions();
            }
        }

        public void UpdateAction()
        {
            if (!m_IsRunning)
            {
                return;
            }

            m_CurrentTime += Time.deltaTime;
            ++m_CurrentTickNumber;

            if (!m_IsOnWaitTime)
            {
                UpdateAllActions();
            }

            if (CanSwitchBetweenTimeModes())
            {
                NextTimeMode();
            }

            if (m_RepetitionNumber >= 0 && m_CurrentRepetitions >= m_RepetitionNumber)
            {
                EndAction();
            }
            else if (m_ConditionToStop != null && m_ConditionToStop.IsFulfilled())
            {
                EndAction();
            }
        }

        public void EndAction()
        {
            if (m_CallEndActionsWhenTimeActionEnds)
            {
                EndAllActions();
            }

            m_IsRunning = false;
        }

        private void StartAllActions()
        {
            foreach (var action in m_Actions)
                action.StartAction();
        }

        private void UpdateAllActions()
        {

            foreach (var action in m_Actions)
                action.UpdateAction();
        }

        private void EndAllActions()
        {
            foreach (var action in m_Actions)
                action.EndAction();

        }

        private bool CanSwitchBetweenTimeModes()
        {
            if (m_IsOnWaitTime)
            {
                switch (m_WaitTime.HowToInterpretTime)
                {
                    case HowToInterpretTime.InSeconds:

                        return m_CurrentTime >= m_MaxTime;

                    case HowToInterpretTime.InTickCount:


                        return m_CurrentTickNumber >= m_MaxTickNumber;
                    default:
                        break;
                }
            }
            else
            {
                switch (m_ActionTime.HowToInterpretTime)
                {
                    case HowToInterpretTime.InSeconds:

                        return m_CurrentTime >= m_MaxTime;

                    case HowToInterpretTime.InTickCount:


                        return m_CurrentTickNumber >= m_MaxTickNumber;
                    default:
                        break;
                }

            }

            return false;
        }

        private void NextTimeMode()
        {
            if (m_UsesWaitTime)
            {
                if (m_IsOnWaitTime)
                {
                    if (m_StartWith == StartWith.ActionTime)
                    {
                        ++m_CurrentRepetitions;
                    }

                    m_IsOnWaitTime = false;
                    RecalculateTime();
                    StartAllActions();
                }
                else
                {
                    if (m_StartWith == StartWith.WaitTime)
                    {
                        ++m_CurrentRepetitions;
                    }

                    EndAllActions();
                    m_IsOnWaitTime = true;
                    RecalculateTime();

                }
            }
            else
            {
                EndAllActions();
                RecalculateTime();
                StartAllActions();
            }
            
        }
        private void RecalculateTime()
        {
            m_CurrentTickNumber = 0;
            m_CurrentTime = 0f;

            if (m_UsesWaitTime && m_IsOnWaitTime)
            {
                switch (m_WaitTime.HowToInterpretTime)
                {
                    case HowToInterpretTime.InSeconds:
                        m_MaxTime = m_WaitTime.Time.CalculateScore();
                        break;
                    case HowToInterpretTime.InTickCount:
                        m_MaxTickNumber = (int)m_WaitTime.Time.CalculateScore();
                        break;
                    default:
                        break;
                }
            }
            else 
            {
                switch (m_ActionTime.HowToInterpretTime)
                {
                    case HowToInterpretTime.InSeconds:
                        m_MaxTime = m_ActionTime.Time.CalculateScore();
                        break;
                    case HowToInterpretTime.InTickCount:
                        m_MaxTickNumber = (int)m_ActionTime.Time.CalculateScore();
                        break;
                    default:
                        break;
                }

            }
        }

        public GameObject GetGameObject() => m_Owner;

        public bool SetGameObject(GameObject gameObj)
        {
            m_Owner = gameObj;
            bool allSet = true;

            m_ActionTime.Time.SetGameObject(gameObj);
            m_WaitTime.Time.SetGameObject(gameObj);
            m_ConditionToStop?.SetGameObject(gameObj);

            foreach (var action in m_Actions)
            {
                if (!action.SetGameObject(gameObj))
                    allSet = false;
            }
            return allSet;
        }
    }

    [CreateAssetMenu(fileName = "TimedAction", menuName = "SilverPillar/Core/TimedAction")]
    public class TimedAction : SaveableScriptableObject, IAction
    {
        [SerializeField]
        private TimedActionData m_TimedActionData = new();

        public TimedActionData Clone(GameObject gameObject)
        {
            var clone = new TimedActionData(m_TimedActionData);
            clone.SetGameObject(gameObject);
            return clone;
        }

        public IAction Clone()
        {
            return m_TimedActionData.Clone();
        }

        public void EndAction()
        {
            m_TimedActionData.EndAction();
        }

        public GameObject GetGameObject()
        {
            return m_TimedActionData.GetGameObject();
        }

        public bool SetGameObject(GameObject gameObj)
        {
            return m_TimedActionData.SetGameObject(gameObj);
        }

        public void StartAction()
        {
            m_TimedActionData.StartAction();
        }

        public void UpdateAction()
        {
            m_TimedActionData.UpdateAction();   
        }
    }
}
