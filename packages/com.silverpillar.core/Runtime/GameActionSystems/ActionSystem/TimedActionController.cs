using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Core
{
    public class TimedActionController : SerializedMonoBehaviour
    {

        public enum WhenToResetTimers
        {
            OnEnable,
            OnStart
        }

        [Header("Settings")]
        [SerializeField]
        private WhenToResetTimers m_WhenToResetTimers = WhenToResetTimers.OnEnable;

        [Header("Data Scriptable Objects")]
        [SerializeField]
        private List<SO_Ref<TimedAction>> m_TimedActions = new();
        private Dictionary<SO_Ref<TimedAction>, List<TimedActionData>> m_TimedActions_To_Instances = new();


        [Header("Non Scriptable Object Data")]
        [OdinSerialize, ShowInInspector]
        private List<TimedActionData> m_TimedActionInstances = new();

        private List<TimedActionData> m_RunningInstances = new();
        private Stack<TimedActionData> m_InstancesToRemove = new();
        void Start()
        {

            foreach (var item in m_TimedActionInstances)
            {
                item.SetGameObject(gameObject);
            }

            foreach (var item in m_TimedActions)
            {
                var clone = item.Get().Clone(gameObject);
                if (!m_TimedActions_To_Instances.ContainsKey(item))
                {
                    m_TimedActions_To_Instances.Add(item, new());
                }

                m_TimedActions_To_Instances[item].Add(clone);
                m_TimedActionInstances.Add(clone);
            }


            if (m_WhenToResetTimers == WhenToResetTimers.OnStart)
            {
                ResetTimers();
            }

        }

        private void OnEnable()
        {
            if (m_WhenToResetTimers == WhenToResetTimers.OnEnable)
            {
                ResetTimers();
            }
        }

        void Update()
        {
            foreach (var actionInstance in m_RunningInstances)
            {
                actionInstance.UpdateAction();

                if (!actionInstance.IsRunning())
                {
                    m_InstancesToRemove.Push(actionInstance);
                }
            }

            while (m_InstancesToRemove.Count > 0)
            {
                m_RunningInstances.Remove(m_InstancesToRemove.Pop());
            }
        }

        private void ResetTimers()
        {
            m_RunningInstances.Clear();
            m_RunningInstances.AddRange(m_TimedActionInstances);

            foreach (var actionInstance in m_RunningInstances)
            {
                actionInstance.StartAction();
            }
        }

        public void AddTimedAction(SO_Ref<TimedAction> timedAction)
        {
            var clone = timedAction.Get().Clone(gameObject);
            if (!m_TimedActions_To_Instances.ContainsKey(timedAction))
            {
                m_TimedActions_To_Instances.Add(timedAction, new());
            }

            m_TimedActions.Add(timedAction);
            m_TimedActions_To_Instances[timedAction].Add(clone);
            m_TimedActionInstances.Add(clone);
            clone.StartAction();
        }

        public void RemoveTimedAction(SO_Ref<TimedAction> timedAction)
        {
            if (m_TimedActions_To_Instances.ContainsKey(timedAction))
            {
                var instances = m_TimedActions_To_Instances[timedAction];

                foreach (var instance in instances)
                {
                    instance.EndAction();
                    m_TimedActionInstances.Remove(instance);
                }

                m_TimedActions_To_Instances.Remove(timedAction);
                m_TimedActions.RemoveAll(refAction => refAction.Equals(timedAction));
            }

        }
    }
}
