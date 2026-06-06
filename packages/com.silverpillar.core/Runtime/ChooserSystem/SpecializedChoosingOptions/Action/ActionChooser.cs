using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace SilverPillar.Core
{
    public class ActionChooser : SerializedMonoBehaviour, IAction, IChoose
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
        private ChooseAction m_ChooseAction;

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
            if (m_ChooseAction == null)
                return;

            SetChosenGameObject();
            m_ChooseAction.Choose();
        }

        public IAction Clone()
        {
            return m_ChooseAction?.Clone();
        }

        public void StartAction()
        {
            if (m_ChooseAction == null)
                return;

            SetChosenGameObject();
            m_ChooseAction.StartAction();
        }

        public void UpdateAction()
        {
            if (m_ChooseAction == null)
                return;

            m_ChooseAction.UpdateAction();
        }

        public void EndAction()
        {
            if (m_ChooseAction == null)
                return;

            m_ChooseAction.EndAction();
        }

        public GameObject GetGameObject()
        {
            return m_ChooseAction != null ? m_ChooseAction.GetGameObject() : null;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            return m_ChooseAction != null && m_ChooseAction.SetGameObject(gameObj);
        }

        private void SetChosenGameObject()
        {
            if (m_ChooseAction == null)
                return;

            switch (m_WhichGameObjectToSet)
            {
                case SelfType.ThisGameObject:
                    m_ChooseAction.SetGameObject(gameObject);
                    break;

                case SelfType.CustomGameObject:
                    if (m_CustomGameObject != null)
                        m_ChooseAction.SetGameObject(m_CustomGameObject);
                    break;
            }
        }
    }
}