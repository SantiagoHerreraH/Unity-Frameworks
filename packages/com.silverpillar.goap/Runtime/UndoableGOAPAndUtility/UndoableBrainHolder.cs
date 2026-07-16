using UnityEngine;
using SilverPillar.Core;
using System.Collections.Generic;
using System;
using Sirenix.OdinInspector;

namespace SilverPillar.GOAP
{
    public class UndoableBrainHolder : SerializedMonoBehaviour, IUndoCommand
    {
        private enum WhenToChooseActions
        {
            DontAutoCall,
            OnStart,
            OnEnable,
            OnUpdate
        }

        private enum CloneSettings
        {
            CreateNewComponentAndAddItToChosen,
            CreateNewGameObject,
            CreateNewGameObjectAndParentItToChosenGameObject
        }

        [Serializable]
        public struct EventData
        {
            public UndoableBehaviorAction BehaviorAction;
            public BehaviorActionEvent BehaviorActionEvent;

        }

        [Title("Auto Calling")]
        private WhenToChooseActions m_WhenToChooseActions;

        [Title("Undo Settings")]
        private bool m_NullCurrentActionWhenUndoing;

        [Title("Clone Settings")]
        private CloneSettings m_CloneSettings;

        [Title("Brain")]
        [SerializeField]
        private SO_Ref<UndoableBrain> m_BrainRef = new();

        [Title("Events")]
        [SerializeField]
        private List<EventData> m_BehaviorActionEvents = null;
        private Dictionary<UndoableBehaviorAction, BehaviorActionEvent> m_BehaviorActionEventsDictionary = null;
        private UndoableBrainInstance m_BrainInstance = null;
        private UndoableBehaviorActionInstance m_CurrentActionInstance = null;
        private BehaviorActionEvent m_CurrentEvent;

        [Title("Debug")]
        [SerializeField]
        private BrainInstanceDebugSettings m_DebugSettings;
        [SerializeField]
        private bool m_PrintActionOnActionChange;

        private bool m_Initialized = false;
        private GameObject m_ChosenGameObject;

        public UndoableBrainHolder(UndoableBrainHolder other)
        {
            CopyFrom(other);
        }

        public void CopyFrom(UndoableBrainHolder other)
        {
            m_WhenToChooseActions = other.m_WhenToChooseActions;
            m_NullCurrentActionWhenUndoing = other.m_NullCurrentActionWhenUndoing;
            m_BrainRef = other.m_BrainRef;
            m_BehaviorActionEvents = new List<EventData>(other.m_BehaviorActionEvents);
            m_Initialized = false;
            Initialize();
            m_CurrentActionInstance = other.m_CurrentActionInstance;
            m_CurrentEvent = other.m_CurrentEvent;
        }

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (m_Initialized)
            {
                return;
            }

            m_BrainInstance = m_BrainRef.Get().CreateInstance(gameObject, m_DebugSettings);

            if (m_BehaviorActionEvents != null)
            {
                if (m_BehaviorActionEventsDictionary == null)
                {
                    m_BehaviorActionEventsDictionary = new();
                }
                foreach (var item in m_BehaviorActionEvents)
                {
                    m_BehaviorActionEventsDictionary.Add(item.BehaviorAction, item.BehaviorActionEvent);
                }
            }

            m_ChosenGameObject = gameObject;

            m_Initialized = true;
        }

        private void Start()
        {
            NullCurrent();
            if (m_WhenToChooseActions == WhenToChooseActions.OnStart)
            {
                ChooseAction();
            }
        }

        private void OnEnable()
        {
            NullCurrent();
            if (m_WhenToChooseActions == WhenToChooseActions.OnEnable)
            {
                ChooseAction();
            }
        }

        void Update() 
        {
            if (m_WhenToChooseActions == WhenToChooseActions.OnUpdate)
            {
                ChooseAction();
            }
            m_CurrentActionInstance?.UpdateAction();
        }

        public void ChooseAction()
        {
            Initialize();
            var newAction = m_BrainInstance.GetActionInstance();

            if (newAction != m_CurrentActionInstance)
            {
                m_CurrentActionInstance?.EndAction();

                if (m_BehaviorActionEvents != null && m_CurrentActionInstance != null && m_BehaviorActionEventsDictionary.TryGetValue(m_CurrentActionInstance.Action, out m_CurrentEvent))
                {
                    m_CurrentEvent.OnEnd?.Invoke();
                }

                m_CurrentActionInstance = newAction;

                m_CurrentActionInstance.StartAction();

                if (m_BehaviorActionEvents != null && m_BehaviorActionEventsDictionary.TryGetValue(m_CurrentActionInstance.Action, out m_CurrentEvent))
                {
                    m_CurrentEvent.OnStart?.Invoke();
                }

                if (m_PrintActionOnActionChange)
                {
                    Debug.Log($"{gameObject}'s Brain Holder NEW ACTION is {m_CurrentActionInstance.Action.name}");
                }
            }
        }

        public bool SetGameObject(GameObject gameObject)
        {
            Initialize();
            m_ChosenGameObject = gameObject;
            return m_BrainInstance.SetGameObject(gameObject);
        }

        public GameObject GetGameObject()
        {
            Initialize();
            return m_ChosenGameObject;
        }

        public void Execute()
        {
            ChooseAction();
        }

        public void Undo()
        {
            m_CurrentActionInstance?.Undo();

            if (m_NullCurrentActionWhenUndoing)
            {
                NullCurrent();
            }
        }

        public IUndoCommand Clone()
        {
            Initialize();
            UndoableBrainHolder newBrainHolder = null;
            GameObject chosen = null;
            switch (m_CloneSettings)
            {
                case CloneSettings.CreateNewComponentAndAddItToChosen:
                    
                    chosen = m_ChosenGameObject;
                    
                    break;
                case CloneSettings.CreateNewGameObject:

                    chosen = new();

                    break;
                case CloneSettings.CreateNewGameObjectAndParentItToChosenGameObject:
                    
                    chosen = new();
                    chosen.transform.SetParent(m_ChosenGameObject.transform);

                    break;
                default:
                    break;
            }

            newBrainHolder = m_ChosenGameObject.AddComponent<UndoableBrainHolder>();
            newBrainHolder?.CopyFrom(this);

            return newBrainHolder;
        }

        public bool CopyTo(IUndoCommand other)
        {
            var otherBrainHolder = other as UndoableBrainHolder;
            if (otherBrainHolder != null)
            {
                otherBrainHolder.CopyFrom(this);
                return true;
            }

            return false;
        }

        private void NullCurrent()
        {
            m_CurrentActionInstance = null;
            m_CurrentEvent = null;
        }
    }
}

