using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

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
        public SimpleTimer Timer;

        public void ResetTimer()
        {
            Timer.SetMaxTime(Time.CalculateScore());
            Timer.ResetTimer();
        }


        public ActionTime(ActionTime other)
        {
            Time = other.Time.Clone();
            Timer = other.Timer;
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
        [SerializeField]
        private string m_Description;
        [FoldoutGroup("Data")]
        [FoldoutGroup("Data/Time Settings")]
        [SerializeField, ShowIf(nameof(m_UsesWaitTime))]
        private StartWith m_StartWith;
        [FoldoutGroup("Data")]
        [FoldoutGroup("Data/Action Time")]
        [SerializeField]
        private ActionTime m_ActionTime = new();

        [FoldoutGroup("Data")]
        [FoldoutGroup("Data/Wait Time")]
        [SerializeField]
        private bool m_UsesWaitTime = true;
        [FoldoutGroup("Data")]
        [FoldoutGroup("Data/Wait Time")]
        [SerializeField, ShowIf(nameof(m_UsesWaitTime))]
        private ActionTime m_WaitTime = new();


        [FoldoutGroup("Data")]
        [FoldoutGroup("Data/When To Stop Action")]
        [SerializeField, Min(-1), Tooltip("-1 is infinite reps")]
        private int m_RepetitionNumber = 0;

        [FoldoutGroup("Data")]
        [FoldoutGroup("Data/When To Stop Action")]
        [OdinSerialize, ShowInInspector]
        private ICachedCondition m_ConditionToStop = null;

        [FoldoutGroup("Data")]
        [FoldoutGroup("Data/When To Stop Action")]
        [SerializeField]
        private bool m_CallEndActionsWhenTimeActionEnds;

        [FoldoutGroup("Data")]
        [FoldoutGroup("Data/Actions")]
        [SerializeField]
        private List<ActionData> m_Actions = new();


        [FoldoutGroup("Data")]
        [FoldoutGroup("Data/Action Events")]
        [SerializeField]
        private UnityEvent m_OnStartActionTime = new();
        [FoldoutGroup("Data")]
        [FoldoutGroup("Data/Action Events")]
        [SerializeField]
        private UnityEvent m_OnEndActionTime = new();
        [FoldoutGroup("Data")]
        [FoldoutGroup("Data/Wait Time Events")]
        [SerializeField]
        private UnityEvent m_OnStartWaitTime = new();
        [FoldoutGroup("Data")]
        [FoldoutGroup("Data/Wait Time Events")]
        [SerializeField]
        private UnityEvent m_OnEndWaitTime = new();

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


            if (!m_IsOnWaitTime)
            {
                m_ActionTime.Timer.Update();
                UpdateAllActions();
            }
            else
            {
                m_WaitTime.Timer.Update();
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
            m_OnStartActionTime.Invoke();
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
            m_OnEndActionTime.Invoke();
            foreach (var action in m_Actions)
                action.EndAction();

        }

        private bool CanSwitchBetweenTimeModes()
        {
            if (!m_UsesWaitTime)
            {
                return m_ActionTime.Timer.IsFinished();
            }

            if (m_IsOnWaitTime)
            {
                return m_WaitTime.Timer.IsFinished();
            }
            else
            {

                return m_ActionTime.Timer.IsFinished();
            }

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

                    m_OnEndWaitTime.Invoke();
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
            if (m_UsesWaitTime && m_IsOnWaitTime)
            {
                m_OnStartWaitTime.Invoke();
                m_WaitTime.ResetTimer();
            }
            else 
            {
                m_ActionTime.ResetTimer();
            }
        }

        public GameObject GetGameObject() => m_Owner;

        public bool SetGameObject(GameObject gameObj)
        {
            m_Owner = gameObj;
            bool allSet = true;

            m_ActionTime.Time.SetGameObject(gameObj);
            m_WaitTime.Time?.SetGameObject(gameObj);
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
