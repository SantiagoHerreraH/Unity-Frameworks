using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SilverPillar.Core
{
    public enum FromWhereToRetrieveChoosingData
    {
        FromScriptableObject,
        FromSelf
    }

    public class ActionTimelineChooser : SerializedMonoBehaviour, IChoose
    {
        [Serializable]
        public class Choice
        {
            [SerializeField]
            private FromWhereToRetrieveChoosingData m_FromWhereToRetrieveChoosingData;
            [SerializeField, ShowIf(nameof(m_FromWhereToRetrieveChoosingData), FromWhereToRetrieveChoosingData.FromScriptableObject)]
            private ActionTimelineChooserData m_ChooseActionScriptableObject;
            [OdinSerialize, ShowIf(nameof(m_FromWhereToRetrieveChoosingData), FromWhereToRetrieveChoosingData.FromSelf)]
            private ChooseActionTimeline m_ChooseActionTimeline;
            private bool m_Cloned = false;

            public void Initialize(TimerName name, GameObject gameObject)
            {
                if (m_FromWhereToRetrieveChoosingData == FromWhereToRetrieveChoosingData.FromScriptableObject)
                {
                    if (m_ChooseActionScriptableObject == null)
                    {
                        Debug.LogError($"No Scriptable Object found in {nameof(ActionTimelineChooser)} in timer name {name.name} in gameObject {gameObject.name}");
                        return;
                    }
                    if (!m_Cloned)
                    {
                        m_ChooseActionTimeline = m_ChooseActionScriptableObject.Clone() as ChooseActionTimeline;
                        m_Cloned = true;
                    }
                }

                m_ChooseActionTimeline.SetGameObject(gameObject);
            }

            public void Choose()
            {
                m_ChooseActionTimeline.Choose();
            }

            public ActionTimeline GetChosenActionTimeline()
            {
                return m_ChooseActionTimeline.DataFromChoosing.ChosenData.FirstOrDefault();
            }
        }

        [Title("References")]
        private ActionTimelineController m_Controller;

        [Title("Settings")]
        [SerializeField]
        private WhenToAutomaticallyChoose m_WhenToAutomaticallyChoose;

        [SerializeField]
        private SelfType m_WhichGameObjectToSet;

        [SerializeField, ShowIf(nameof(m_WhichGameObjectToSet), SelfType.CustomGameObject)]
        private GameObject m_CustomGameObject;

        [Title("Data")]
        [OdinSerialize, ShowInInspector, DictionaryDrawerSettings(DisplayMode = DictionaryDisplayOptions.ExpandedFoldout, KeyLabel = "Timeline Name", ValueLabel = "Choice")]
        private Dictionary<TimerName, Choice> m_Choices = new();
        private bool m_Initialized = false;

        private void Initialize()
        {
            if (m_Initialized || m_Controller == null)
                return;

            GameObject chosen = gameObject;

            switch (m_WhichGameObjectToSet)
            {
                case SelfType.ThisGameObject:
                    break;

                case SelfType.CustomGameObject:

                    if (m_CustomGameObject != null)
                    {
                        chosen = m_CustomGameObject;
                    }
                    else
                    {
                        Debug.LogError("");
                    }

                    break;
            }

            foreach (var item in m_Choices)
            {
                item.Value.Initialize(item.Key, chosen);
            }

            m_Initialized = true;
        }

        private void Awake()
        {
            if (m_Controller == null)
            {
                if (!TryGetComponent(out m_Controller))
                {
                    Debug.LogError("");
                }
            }
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
            if (m_Controller == null)
                return;

            Initialize();

            foreach (var item in m_Choices)
            {
                m_Controller.SetTimeline(item.Key, item.Value.GetChosenActionTimeline());
            }

        }

    }
}