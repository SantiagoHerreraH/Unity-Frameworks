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
            [OdinSerialize, ShowInInspector]
            private ICachedCondition m_Condition;

            [OdinSerialize, ShowInInspector]
            private List<ICachedGameAction> m_Actions = new();

            [SerializeField]
            private UnityEvent m_OnExecute = new();

            public void SetGameObject(GameObject gameObject)
            {
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

                m_OnExecute?.Invoke();
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

            m_IsInitialized = true;

            m_ConditionToCheckAllActions?.SetGameObject(gameObject);

            if (m_ActionData == null)
                return;

            foreach (var item in m_ActionData)
            {
                item?.SetGameObject(gameObject);
            }
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