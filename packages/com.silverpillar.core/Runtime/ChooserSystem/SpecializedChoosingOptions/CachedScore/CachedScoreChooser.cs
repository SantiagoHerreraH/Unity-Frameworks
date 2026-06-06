using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace SilverPillar.Core
{
    public class CachedScoreChooser : SerializedMonoBehaviour, ICachedScore, IChoose
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
        private ChooseCachedScore m_ChooseCachedScore;

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
            if (m_ChooseCachedScore == null)
                return;

            SetChosenGameObject();
            m_ChooseCachedScore.Choose();
        }

        public ICachedScore Clone()
        {
            return m_ChooseCachedScore.Clone();
        }

        public GameObject GetGameObject()
        {
            return m_ChooseCachedScore.GetGameObject();
        }

        public float CalculateScore()
        {
            return m_ChooseCachedScore.CalculateScore();
        }

        public bool SetGameObject(GameObject gameObj)
        {
            return m_ChooseCachedScore.SetGameObject(gameObj);
        }

        private void SetChosenGameObject()
        {
            if (m_ChooseCachedScore == null)
                return;

            switch (m_WhichGameObjectToSet)
            {
                case SelfType.ThisGameObject:
                    m_ChooseCachedScore.SetGameObject(gameObject);
                    break;

                case SelfType.CustomGameObject:
                    if (m_CustomGameObject != null)
                        m_ChooseCachedScore.SetGameObject(m_CustomGameObject);
                    break;
            }
        }
    }
}
