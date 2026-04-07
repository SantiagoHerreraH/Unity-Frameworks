using Sirenix.OdinInspector.Editor;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SilverPillar.Core
{
    public class TimedActionController : MonoBehaviour
    {
        [Header("Data Scriptable Objects")]
        [SerializeField]
        private List<SO_Ref<TimedAction>> m_TimedActions = new();
        private Dictionary<SO_Ref<TimedAction>, List<TimedActionData>> m_TimedActions_To_Instances = new();


        [Header("Non Scriptable Object Data")]
        [SerializeField]
        private List<TimedActionData> m_TimedActionInstances = new();
        private List<TimedActionData> m_InstancesToRemove = new();
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

            foreach (var actionInstance in m_TimedActionInstances)
            {
                actionInstance.StartAction();
            }
        }

        void Update()
        {
            foreach (var actionInstance in m_TimedActionInstances)
            {
                actionInstance.UpdateAction();

                if (!actionInstance.IsRunning())
                {
                    m_InstancesToRemove.Add(actionInstance);
                }
            }

            while (m_InstancesToRemove.Count > 0)
            {
                m_TimedActionInstances.Remove(m_InstancesToRemove.Last());
                m_InstancesToRemove.Remove(m_InstancesToRemove.Last());
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
