using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace SilverPillar.Core
{
    public class CachedInteractionScoreChooser : SerializedMonoBehaviour, ICachedInteractionScore, IChoose
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
        private ChooseCachedInteractionScore m_ChooseCachedInteractionScore;

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
            if (m_ChooseCachedInteractionScore == null)
                return;

            SetChosenGameObject();
            m_ChooseCachedInteractionScore.Choose();
        }

        public ICachedInteractionScore Clone()
        {
            return m_ChooseCachedInteractionScore.Clone();
        }

        public GameObject GetGameObject()
        {
            return m_ChooseCachedInteractionScore.GetGameObject();
        }

        public float CalculateScore(GameObject target)
        {
            return m_ChooseCachedInteractionScore.CalculateScore(target);
        }

        public bool SetGameObject(GameObject gameObj)
        {
            return m_ChooseCachedInteractionScore.SetGameObject(gameObj);
        }

        private void SetChosenGameObject()
        {
            if (m_ChooseCachedInteractionScore == null)
                return;

            switch (m_WhichGameObjectToSet)
            {
                case SelfType.ThisGameObject:
                    m_ChooseCachedInteractionScore.SetGameObject(gameObject);
                    break;

                case SelfType.CustomGameObject:
                    if (m_CustomGameObject != null)
                        m_ChooseCachedInteractionScore.SetGameObject(m_CustomGameObject);
                    break;
            }
        }
    }
}
