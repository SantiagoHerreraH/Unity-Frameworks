using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace SilverPillar.Core
{
    public class CachedGameActionMachine : SerializedMonoBehaviour
    {
        [Serializable]
        public class ActionData
        {
            [Title("Condition")]
            [OdinSerialize, ShowInInspector]
            private ICachedCondition m_Condition;

            [Title("Actions")]
            [OdinSerialize, ShowInInspector]
            private List<ICachedGameAction> m_Actions = new();

            [Title("Event")]
            [SerializeField]
            private UnityEvent<GameObject> m_OnExecute = new();

            private GameObject m_GameObject;

            public void SetGameObject(GameObject gameObject)
            {
                m_GameObject = gameObject;
                m_Condition?.SetGameObject(gameObject);

                if (m_Actions == null)
                    return;

                foreach (var action in m_Actions)
                {
                    action?.SetGameObject(gameObject);
                }
            }

            public bool Execute()
            {
                if (m_Condition != null && !m_Condition.IsFulfilled())
                    return false;

                if (m_Actions != null)
                {
                    foreach (var action in m_Actions)
                    {
                        action?.Execute();
                    }
                }

                m_OnExecute?.Invoke(m_GameObject);
                return true;
            }
        }

        public enum WhenToAutoCall
        {
            None,
            Awake,
            Start,
            OnEnable,
            OnSceneChange,
            Update,
            FixedUpdate,
            LateUpdate,
            OnDisable,
            OnDestroy,
            OnApplicationQuit
        }

        public enum ExecutionMode
        {
            AllExecuteIfConditionFulfilled,
            OnlyFirstToFulfillConditionExecutes
        }

        [Title("AutoCall Settings")]
        [SerializeField]
        private WhenToAutoCall m_WhenToAutoCall = WhenToAutoCall.Update;


        [Title("Who To Execute On")]
        [SerializeField]
        private SelfType m_WhoToExecuteOn = SelfType.ThisGameObject;
        [SerializeField, ShowIf(nameof(m_WhoToExecuteOn), SelfType.CustomGameObject)]
        private GameObject m_CustomGameObject;

        [Title("Conditions")]
        [SerializeField]
        private ExecutionMode m_ExecutionMode = ExecutionMode.AllExecuteIfConditionFulfilled;

        [OdinSerialize, ShowInInspector]
        private ICachedCondition m_ConditionToCheckAllActions;

        [Title("Actions")]
        [OdinSerialize, ShowInInspector]
        private List<ActionData> m_ActionData = new();

        private bool m_IsInitialized;

        private void Awake()
        {
            Initialize();

            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            if (m_WhenToAutoCall == WhenToAutoCall.Awake)
                Execute();
        }

        private void OnEnable()
        {
            if (m_WhenToAutoCall == WhenToAutoCall.OnEnable)
                Execute();
        }

        private void Start()
        {
            if (m_WhenToAutoCall == WhenToAutoCall.Start)
                Execute();
        }

        private void Update()
        {
            if (m_WhenToAutoCall == WhenToAutoCall.Update)
                Execute();
        }

        private void FixedUpdate()
        {
            if (m_WhenToAutoCall == WhenToAutoCall.FixedUpdate)
                Execute();
        }

        private void LateUpdate()
        {
            if (m_WhenToAutoCall == WhenToAutoCall.LateUpdate)
                Execute();
        }

        private void OnDisable()
        {

            if (m_WhenToAutoCall == WhenToAutoCall.OnDisable)
                Execute();
        }

        private void OnDestroy()
        {
            if (m_WhenToAutoCall == WhenToAutoCall.OnDestroy)
                Execute();

            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        }

        private void OnApplicationQuit()
        {
            if (m_WhenToAutoCall == WhenToAutoCall.OnApplicationQuit)
                Execute();
        }

        private void OnActiveSceneChanged(Scene previousScene, Scene newScene)
        {
            if (m_WhenToAutoCall == WhenToAutoCall.OnSceneChange)
                Execute();
        }

        private void Initialize()
        {
            if (m_IsInitialized)
                return;

            GameObject chosen = gameObject;
            switch (m_WhoToExecuteOn)
            {
                case SelfType.ThisGameObject:
                    break;
                case SelfType.CustomGameObject:
                    if (m_CustomGameObject == null)
                    {
                        Debug.LogError($"Custom game object is null in {nameof(CachedGameActionMachine)} in gameobject {gameObject.name}. Falling back to this game object.");
                        break;
                    }
                    chosen = m_CustomGameObject;
                    break;
                default:
                    break;
            }

            m_ConditionToCheckAllActions?.SetGameObject(chosen);

            if (m_ActionData == null)
                return;

            foreach (var item in m_ActionData)
            {
                item?.SetGameObject(chosen);
            }

            m_IsInitialized = true;
        }

        [Button(ButtonSizes.Medium)]
        public void Execute()
        {
            Initialize();

            if (m_ConditionToCheckAllActions != null &&
                !m_ConditionToCheckAllActions.IsFulfilled())
            {
                return;
            }

            if (m_ActionData == null)
                return;

            switch (m_ExecutionMode)
            {
                case ExecutionMode.AllExecuteIfConditionFulfilled:
                    ExecuteAllFulfilled();
                    break;

                case ExecutionMode.OnlyFirstToFulfillConditionExecutes:
                    ExecuteOnlyFirstFulfilled();
                    break;
            }
        }

        [Button(ButtonSizes.Medium)]
        public void Execute(GameObject gameObject)
        {
            m_WhoToExecuteOn = SelfType.CustomGameObject;
            m_CustomGameObject = gameObject;
            m_IsInitialized = false;

            Initialize();

            if (m_ConditionToCheckAllActions != null &&
                !m_ConditionToCheckAllActions.IsFulfilled())
            {
                return;
            }

            if (m_ActionData == null)
                return;

            switch (m_ExecutionMode)
            {
                case ExecutionMode.AllExecuteIfConditionFulfilled:
                    ExecuteAllFulfilled();
                    break;

                case ExecutionMode.OnlyFirstToFulfillConditionExecutes:
                    ExecuteOnlyFirstFulfilled();
                    break;
            }
        }

        private void ExecuteAllFulfilled()
        {
            foreach (var item in m_ActionData)
            {
                item?.Execute();
            }
        }

        private void ExecuteOnlyFirstFulfilled()
        {
            foreach (var item in m_ActionData)
            {
                if (item != null && item.Execute())
                    return;
            }
        }
    }
}