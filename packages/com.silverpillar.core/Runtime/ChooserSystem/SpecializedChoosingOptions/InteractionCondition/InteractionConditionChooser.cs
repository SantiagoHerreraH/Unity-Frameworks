using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace SilverPillar.Core
{
    public class InteractionConditionChooser : SerializedMonoBehaviour, IInteractionCondition, IChoose
    {
        [Title("Settings")]
        [SerializeField]
        private WhenToAutomaticallyChoose m_WhenToAutomaticallyChoose;

        [SerializeField]
        private SelfType m_WhichGameObjectToSet;

        [SerializeField, ShowIf(nameof(m_WhichGameObjectToSet), SelfType.CustomGameObject)]
        private GameObject m_CustomGameObject;

        [Title("Data")]
        [OdinSerialize, ShowInInspector]
        private ChooseInteractionCondition m_ChooseCachedCondition;


        private void Awake()
        {
            if (m_WhenToAutomaticallyChoose == WhenToAutomaticallyChoose.OnAwake)
                Choose();
        }

        private void Start()
        {
            if (m_WhenToAutomaticallyChoose == WhenToAutomaticallyChoose.OnStart)
                Choose();
        }

        private void OnEnable()
        {
            if (m_WhenToAutomaticallyChoose == WhenToAutomaticallyChoose.OnEnable)
                Choose();
        }

        private void Update()
        {
            if (m_WhenToAutomaticallyChoose == WhenToAutomaticallyChoose.OnUpdate)
                Choose();
        }

        private void FixedUpdate()
        {
            if (m_WhenToAutomaticallyChoose == WhenToAutomaticallyChoose.OnFixedUpdate)
                Choose();
        }

        private void LateUpdate()
        {
            if (m_WhenToAutomaticallyChoose == WhenToAutomaticallyChoose.OnLateUpdate)
                Choose();
        }

        private void OnDisable()
        {
            if (m_WhenToAutomaticallyChoose == WhenToAutomaticallyChoose.OnDisable)
                Choose();
        }
        public void Choose()
        {
            if (m_ChooseCachedCondition == null)
                return;

            SetChosenGameObject();
            m_ChooseCachedCondition.Choose();
        }

        public IInteractionCondition Clone()
        {
            return m_ChooseCachedCondition.Clone();
        }

        public GameObject GetGameObject()
        {
            return m_ChooseCachedCondition.GetGameObject();
        }

        public bool IsFulfilled(GameObject target)
        {
            return m_ChooseCachedCondition.IsFulfilled(target);
        }

        public bool SetGameObject(GameObject gameObj)
        {
            return m_ChooseCachedCondition.SetGameObject(gameObj);
        }

        private void SetChosenGameObject()
        {
            if (m_ChooseCachedCondition == null)
                return;

            switch (m_WhichGameObjectToSet)
            {
                case SelfType.ThisGameObject:
                    m_ChooseCachedCondition.SetGameObject(gameObject);
                    break;

                case SelfType.CustomGameObject:
                    if (m_CustomGameObject != null)
                        m_ChooseCachedCondition.SetGameObject(m_CustomGameObject);
                    break;
            }
        }
    }
}
