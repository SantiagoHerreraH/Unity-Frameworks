
using SilverPillar.Core;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.Events;

namespace SilverPillar.EventTools
{
    public class ConditionalTrigger : SerializedMonoBehaviour
    {
        [OdinSerialize, ShowInInspector]
        private ICachedCondition Condition = null;

        [SerializeField]
        private UnityEvent m_OnTrue;

        [SerializeField]
        private UnityEvent m_OnFalse;

        private bool m_IsInitialized = false;

        private void Awake()
        {
            Initialize();
        }

        public void Trigger()
        {
            Initialize();

            if (Condition.IsFulfilled())
            {
                m_OnTrue.Invoke();
            }
            else
            {
                m_OnFalse.Invoke();
            }
        }

        private void Initialize()
        {
            if (!m_IsInitialized)
            {
                Condition.SetGameObject(gameObject);

                m_IsInitialized = true;
            }
        }
    }


}
