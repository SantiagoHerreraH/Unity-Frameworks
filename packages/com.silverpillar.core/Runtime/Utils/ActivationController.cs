using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace SilverPillar.Core
{
    public class ActivationController : SerializedMonoBehaviour
    {
        [Serializable]
        private class ActionsAndEvents
        {
            private enum WhatToTriggerFirst
            {
                Actions,
                Event
            }

            [SerializeField]
            private WhatToTriggerFirst m_WhatToTriggerFirst;

            [OdinSerialize, ShowInInspector]
            private List<ICachedGameAction> m_Actions;

            [SerializeField]
            private UnityEvent m_Event;

            public void SetGameObject(GameObject gameObj)
            {
                if (m_Actions != null)
                {
                    foreach (var item in m_Actions)
                    {
                        item.SetGameObject(gameObj);
                    }
                }
            }
            public void Trigger()
            {
                switch (m_WhatToTriggerFirst)
                {
                    case WhatToTriggerFirst.Actions:

                        TriggerActions();
                        m_Event?.Invoke();

                        break;
                    case WhatToTriggerFirst.Event:

                        m_Event?.Invoke();
                        TriggerActions();

                        break;
                    default:
                        break;
                }
            }

            private void TriggerActions()
            {
                foreach (var item in m_Actions)
                {
                    item.Execute();
                }
            }
        }

        [Title("Target")]
        [SerializeField]
        private SelfType m_WhichGameObject;
        [SerializeField, ShowIf(nameof(m_WhichGameObject), SelfType.CustomGameObject)]
        private GameObject m_TargetGameObject;

        [Title("Activation")]
        [OdinSerialize, ShowInInspector]
        private ActionsAndEvents m_OnBeforeActivating;
        [OdinSerialize, ShowInInspector]
        private ActionsAndEvents m_OnAfterActivating;

        [Title("Deactivation")]
        [OdinSerialize, ShowInInspector]
        private ActionsAndEvents m_OnBeforeDeactivating;
        [OdinSerialize, ShowInInspector]
        private ActionsAndEvents m_OnAfterDeactivating;

        private bool m_Initialized = false;
        private void Initialize()
        {
            if (m_Initialized) return;

            if (m_WhichGameObject == SelfType.ThisGameObject)
            {
                m_TargetGameObject = gameObject;
            }

            if (m_TargetGameObject == null)
            {
                Debug.LogError($"{nameof(m_TargetGameObject)} is null in {nameof(ActivationController)} in {gameObject.name} gameobject");
            }

            m_OnBeforeActivating.SetGameObject(m_TargetGameObject);
            m_OnAfterActivating.SetGameObject(m_TargetGameObject);

            m_OnBeforeDeactivating.SetGameObject(m_TargetGameObject);
            m_OnAfterDeactivating.SetGameObject(m_TargetGameObject);

            m_Initialized = true;

        }

        public void Activate()
        {
            Initialize();

            m_OnBeforeActivating.Trigger();

            if (m_TargetGameObject != null)
            {
                m_TargetGameObject.SetActive(true);
            }

            m_OnAfterActivating.Trigger();
        }

        public void Deactivate()
        {
            Initialize();

            m_OnBeforeDeactivating.Trigger();

            if (m_TargetGameObject != null)
            {
                m_TargetGameObject.SetActive(false);
            }

            m_OnAfterDeactivating.Trigger();
        }
    }
}
