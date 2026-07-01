using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Core
{
    public class GroupCachedGameActionExecuter : SerializedMonoBehaviour
    {
        public enum ExecuteOnWho
        {
            Children,
            SelfAndChildren,
            SelfAndParent,
            SelfChildrenAndParent,
            Custom,
            AllGameObjectsThatMeetCondition
        }

        public enum WhenToRetrieveTargetGameObjects
        {
            OnAwake,
            OnStart,
            OnEnable,
            EveryTimeBeforeCalling
        }

        public enum ActionProtocol
        {
            CloneActions,
            UseTheSameAction
        }

        [Serializable]
        public struct InstanceData
        {
            private GameObject m_Instance;
            public GameObject Instance => m_Instance;

            private List<ActionData> m_ActionData;

            private ActionProtocol m_ActionProtocol;

            private ICachedScore m_Interval;
            private float m_CurrentInterval;
            private float m_CurrentTime;

            private bool m_Initialized;
            private bool m_CallOnTimeZero;

            private Dictionary<ICachedGameAction, ICachedGameAction> m_OriginalToClone;

            public InstanceData(
                GameObject instance,
                List<ActionData> actionData,
                ActionProtocol actionProtocol,
                ICachedScore interval,
                bool callOnTimeZero)
            {
                m_Instance = instance;
                m_ActionProtocol = actionProtocol;
                m_CallOnTimeZero = callOnTimeZero;

                m_Interval = interval?.Clone();
                m_CurrentInterval = 0f;
                m_CurrentTime = 0f;

                if (m_Interval != null)
                {
                    m_Interval.SetGameObject(instance);
                    m_CurrentInterval = Mathf.Max(0f, m_Interval.CalculateScore());

                    if (m_CallOnTimeZero)
                    {
                        m_CurrentTime = m_CurrentInterval;
                    }
                }

                m_Initialized = false;
                m_OriginalToClone = new Dictionary<ICachedGameAction, ICachedGameAction>();

                switch (actionProtocol)
                {
                    case ActionProtocol.CloneActions:
                        m_ActionData = new List<ActionData>();

                        if (actionData != null)
                        {
                            for (int i = 0; i < actionData.Count; ++i)
                            {
                                ActionData clone = actionData[i].Clone();

                                if (clone.GameAction != null)
                                {
                                    clone.GameAction.SetGameObject(m_Instance);
                                }

                                m_ActionData.Add(clone);
                            }
                        }

                        break;

                    case ActionProtocol.UseTheSameAction:
                        m_ActionData = actionData;
                        break;

                    default:
                        m_ActionData = new List<ActionData>();
                        break;
                }

                m_ActionData ??= new List<ActionData>();
            }

            public void Execute()
            {
                if (m_Interval != null)
                {
                    Initialize();

                    m_CurrentTime += Time.deltaTime;

                    if (m_CurrentTime >= m_CurrentInterval)
                    {
                        m_CurrentInterval = Mathf.Max(0f, m_Interval.CalculateScore());
                        m_CurrentTime = 0f;
                        InternalExecute();
                    }

                    return;
                }

                InternalExecute();
            }

            public bool SetGameObject(GameObject gameObject)
            {
                m_Instance = gameObject;
                m_ActionData ??= new List<ActionData>();

                bool allGood = true;

                for (int i = 0; i < m_ActionData.Count; ++i)
                {
                    if (m_ActionData[i].GameAction == null)
                    {
                        allGood = false;
                        continue;
                    }

                    allGood &= m_ActionData[i].GameAction.SetGameObject(m_Instance);
                }

                if (m_Interval != null)
                {
                    m_Interval.SetGameObject(m_Instance);
                }

                return allGood;
            }

            public void SortActionsByPriority()
            {
                m_ActionData ??= new List<ActionData>();
                m_ActionData.Sort((a, b) => a.PriorityNumber.CompareTo(b.PriorityNumber));
            }

            public ICachedGameAction CloneGameAction(ICachedGameAction gameAction, int priorityNumber = 0)
            {
                if (gameAction == null)
                    return null;

                m_ActionData ??= new List<ActionData>();
                m_OriginalToClone ??= new Dictionary<ICachedGameAction, ICachedGameAction>();

                if (m_OriginalToClone.TryGetValue(gameAction, out ICachedGameAction existingClone))
                {
                    return existingClone;
                }

                ICachedGameAction clone = gameAction.Clone();

                if (clone == null)
                    return null;

                m_OriginalToClone.Add(gameAction, clone);

                clone.SetGameObject(m_Instance);

                m_ActionData.Add(new ActionData(clone, priorityNumber));
                SortActionsByPriority();

                return clone;
            }

            public void RemoveGameAction(ICachedGameAction originalGameAction)
            {
                if (originalGameAction == null || m_OriginalToClone == null)
                    return;

                if (!m_OriginalToClone.TryGetValue(originalGameAction, out ICachedGameAction cloned))
                    return;

                m_ActionData.RemoveAll(x => x.GameAction == cloned);
                m_OriginalToClone.Remove(originalGameAction);
            }

            public void ClearGameActions()
            {
                m_ActionData?.Clear();
                m_OriginalToClone?.Clear();
            }

            public void DebugDraw()
            {
                if (m_ActionData == null)
                    return;

                foreach (ActionData item in m_ActionData)
                {
                    if (item.GameAction is not IDebugDraw debugDraw)
                        continue;

                    if (item.GameAction.SetGameObject(m_Instance))
                    {
                        debugDraw.DebugDraw(WhereToDraw.OnDrawGizmos);
                    }
                }
            }

            private void InternalExecute()
            {
                if (m_ActionData == null)
                    return;

                if (m_ActionProtocol == ActionProtocol.UseTheSameAction)
                {
                    for (int i = 0; i < m_ActionData.Count; ++i)
                    {
                        m_ActionData[i].GameAction?.SetGameObject(m_Instance);
                    }
                }

                for (int i = 0; i < m_ActionData.Count; ++i)
                {
                    m_ActionData[i].GameAction?.Execute();
                }
            }

            private void Initialize()
            {
                if (m_Initialized)
                    return;

                SortActionsByPriority();

                if (m_Interval != null)
                {
                    m_Interval.SetGameObject(m_Instance);
                    m_CurrentInterval = Mathf.Max(0f, m_Interval.CalculateScore());

                    if (m_CallOnTimeZero)
                    {
                        m_CurrentTime = m_CurrentInterval;
                    }
                }

                m_Initialized = true;
            }
        }

        [Serializable]
        public struct ActionData
        {
            [OdinSerialize, ShowInInspector]
            public ICachedGameAction GameAction;

            [HideInInspector]
            public int PriorityNumber;

            public ActionData(ICachedGameAction action, int priority = 0)
            {
                GameAction = action;
                PriorityNumber = priority;
            }

            public ActionData Clone()
            {
                return new ActionData(GameAction?.Clone(), PriorityNumber);
            }
        }

        public enum WhenToAutoCallActions
        {
            DontAutoCall,
            OnAwake,
            OnStart,
            OnEnable,
            OnUpdate,
            OnFixedUpdate,
            OnLateUpdate,
            OnDisable,
            OnDestroy
        }

        [Title("Auto Call Settings")]
        [SerializeField]
        private WhenToAutoCallActions m_WhenToAutoCallActions = WhenToAutoCallActions.DontAutoCall;

        private bool m_ShowUseIntervals =>
            m_WhenToAutoCallActions == WhenToAutoCallActions.OnUpdate ||
            m_WhenToAutoCallActions == WhenToAutoCallActions.OnFixedUpdate ||
            m_WhenToAutoCallActions == WhenToAutoCallActions.OnLateUpdate;

        private bool m_ShowInterval => m_ShowUseIntervals && m_UseIntervals;

        [SerializeField, ShowIf(nameof(m_ShowUseIntervals))]
        private bool m_UseIntervals = false;

        [OdinSerialize, ShowInInspector, ShowIf(nameof(m_ShowInterval))]
        private ICachedScore m_Interval;

        [SerializeField, Tooltip("Whether to call when timer is zero, or wait for current time to reach the interval."), ShowIf(nameof(m_ShowInterval))]
        private bool m_CallOnTimeZero = true;

        [Title("Debug")]
        [SerializeField]
        private bool m_DrawDebug = false;

        [Title("Action Targets")]
        [SerializeField]
        [OnValueChanged(nameof(OnExecuteOnWhoChanged) + "($value, $oldValue)")]
        private ExecuteOnWho m_ExecuteOnWho;

        [OdinSerialize, ShowIf(nameof(m_ExecuteOnWho), ExecuteOnWho.AllGameObjectsThatMeetCondition), ShowInInspector]
        private IInteractionCondition m_FilterCondition;

        [SerializeField, ShowIf(nameof(m_ExecuteOnWho), ExecuteOnWho.Custom), Tooltip("If this is null or empty will get self")]
        [OnValueChanged(nameof(OnGameObjectsChanged) + "($value, $oldValue)")]
        private List<GameObject> m_ChosenGameObjects = new List<GameObject>();

        [SerializeField]
        private WhenToRetrieveTargetGameObjects m_WhenToRetrieveTargetGameObjects;

        [SerializeField]
        private ActionProtocol m_ActionProtocol = ActionProtocol.CloneActions;

        [Title("Actions")]
        [OdinSerialize, ShowInInspector]
        private List<ActionData> m_Data = new List<ActionData>();

        public List<ActionData> Data => m_Data;

        private HashSet<GameObject> m_Instances = new HashSet<GameObject>();
        private List<InstanceData> m_InstanceData = new List<InstanceData>();

        // Runtime buffers to avoid repeated heap allocations in RetrieveGameObjects.
        private List<GameObject> m_RetrievedGameObjectsBuffer = new List<GameObject>();
        private List<InstanceData> m_InstanceDataBuffer = new List<InstanceData>();
        private Dictionary<GameObject, InstanceData> m_OldInstanceDataByGameObject = new Dictionary<GameObject, InstanceData>();
        private List<Transform> m_TransformBuffer = new List<Transform>();

        private bool m_Initialized = false;
        private bool m_InitializedForDebugDraw = false;

        private void Awake()
        {
            Initialize();

            if (m_WhenToRetrieveTargetGameObjects == WhenToRetrieveTargetGameObjects.OnAwake)
            {
                RetrieveGameObjects();
            }

            if (m_WhenToAutoCallActions == WhenToAutoCallActions.OnAwake)
            {
                Execute();
            }
        }

        private void Start()
        {
            if (m_WhenToRetrieveTargetGameObjects == WhenToRetrieveTargetGameObjects.OnStart)
            {
                RetrieveGameObjects();
            }

            if (m_WhenToAutoCallActions == WhenToAutoCallActions.OnStart)
            {
                Execute();
            }
        }

        private void OnEnable()
        {
            if (m_WhenToRetrieveTargetGameObjects == WhenToRetrieveTargetGameObjects.OnEnable)
            {
                RetrieveGameObjects();
            }

            if (m_WhenToAutoCallActions == WhenToAutoCallActions.OnEnable)
            {
                Execute();
            }
        }

        private void OnDisable()
        {
            if (m_WhenToAutoCallActions == WhenToAutoCallActions.OnDisable)
            {
                Execute();
            }
        }

        private void OnDestroy()
        {
            if (m_WhenToAutoCallActions == WhenToAutoCallActions.OnDestroy)
            {
                Execute();
            }
        }

        private void Update()
        {
            if (m_WhenToAutoCallActions == WhenToAutoCallActions.OnUpdate)
            {
                Execute();
            }
        }

        private void FixedUpdate()
        {
            if (m_WhenToAutoCallActions == WhenToAutoCallActions.OnFixedUpdate)
            {
                Execute();
            }
        }

        private void LateUpdate()
        {
            if (m_WhenToAutoCallActions == WhenToAutoCallActions.OnLateUpdate)
            {
                Execute();
            }
        }

        private void OnDrawGizmos()
        {
            if (!m_DrawDebug)
                return;

            if (m_Data == null || m_Data.Count == 0)
                return;

            if (!m_InitializedForDebugDraw)
            {
                RetrieveGameObjects();
                m_InitializedForDebugDraw = true;
            }

            for (int i = 0; i < m_InstanceData.Count; i++)
            {
                m_InstanceData[i].DebugDraw();
            }
        }

        private void OnGameObjectsChanged(List<GameObject> value, List<GameObject> oldValue)
        {
            if (m_DrawDebug)
            {
                m_InitializedForDebugDraw = false;
            }
        }

        private void OnExecuteOnWhoChanged(ExecuteOnWho value, ExecuteOnWho oldValue)
        {
            if (m_DrawDebug)
            {
                m_InitializedForDebugDraw = false;
            }
        }

        private void Initialize()
        {
            if (m_Initialized)
                return;

            m_Data ??= new List<ActionData>();
            m_ChosenGameObjects ??= new List<GameObject>();
            m_Instances ??= new HashSet<GameObject>();
            m_InstanceData ??= new List<InstanceData>();

            m_RetrievedGameObjectsBuffer ??= new List<GameObject>();
            m_InstanceDataBuffer ??= new List<InstanceData>();
            m_OldInstanceDataByGameObject ??= new Dictionary<GameObject, InstanceData>();
            m_TransformBuffer ??= new List<Transform>();

            m_Initialized = true;
        }

        [Title("Buttons")]
        [Button]
        public void RetrieveGameObjects()
        {
            Initialize();

            m_RetrievedGameObjectsBuffer.Clear();
            m_Instances.Clear();

            switch (m_ExecuteOnWho)
            {
                case ExecuteOnWho.Children:
                    AddChildren(m_RetrievedGameObjectsBuffer, m_Instances, false);
                    break;

                case ExecuteOnWho.SelfAndChildren:
                    AddGameObject(gameObject, m_RetrievedGameObjectsBuffer, m_Instances);
                    AddChildren(m_RetrievedGameObjectsBuffer, m_Instances, false);
                    break;

                case ExecuteOnWho.SelfAndParent:
                    AddGameObject(gameObject, m_RetrievedGameObjectsBuffer, m_Instances);
                    AddParent(m_RetrievedGameObjectsBuffer, m_Instances);
                    break;

                case ExecuteOnWho.SelfChildrenAndParent:
                    AddGameObject(gameObject, m_RetrievedGameObjectsBuffer, m_Instances);
                    AddChildren(m_RetrievedGameObjectsBuffer, m_Instances, false);
                    AddParent(m_RetrievedGameObjectsBuffer, m_Instances);
                    break;

                case ExecuteOnWho.Custom:
                    AddCustomGameObjects(m_RetrievedGameObjectsBuffer, m_Instances);
                    break;

                case ExecuteOnWho.AllGameObjectsThatMeetCondition:
                    AddAllGameObjectsThatMeetCondition(m_RetrievedGameObjectsBuffer, m_Instances);
                    break;
            }

            if (m_ExecuteOnWho != ExecuteOnWho.Custom)
            {
                CopyRetrievedGameObjectsToChosenGameObjects();
            }

            RefreshInstanceData(m_RetrievedGameObjectsBuffer);
        }

        private void CopyRetrievedGameObjectsToChosenGameObjects()
        {
            m_ChosenGameObjects ??= new List<GameObject>();
            m_ChosenGameObjects.Clear();

            if (m_ChosenGameObjects.Capacity < m_RetrievedGameObjectsBuffer.Count)
            {
                m_ChosenGameObjects.Capacity = m_RetrievedGameObjectsBuffer.Count;
            }

            for (int i = 0; i < m_RetrievedGameObjectsBuffer.Count; i++)
            {
                m_ChosenGameObjects.Add(m_RetrievedGameObjectsBuffer[i]);
            }
        }

        private void RefreshInstanceData(List<GameObject> targetGameObjects)
        {
            m_OldInstanceDataByGameObject.Clear();
            m_InstanceDataBuffer.Clear();

            for (int i = 0; i < m_InstanceData.Count; i++)
            {
                GameObject instance = m_InstanceData[i].Instance;

                if (instance == null)
                    continue;

                if (!m_Instances.Contains(instance))
                    continue;

                if (!m_OldInstanceDataByGameObject.ContainsKey(instance))
                {
                    m_OldInstanceDataByGameObject.Add(instance, m_InstanceData[i]);
                }
            }

            if (m_InstanceDataBuffer.Capacity < targetGameObjects.Count)
            {
                m_InstanceDataBuffer.Capacity = targetGameObjects.Count;
            }

            ICachedScore intervalToUse = m_UseIntervals ? m_Interval : null;

            for (int i = 0; i < targetGameObjects.Count; i++)
            {
                GameObject instance = targetGameObjects[i];

                if (instance == null)
                    continue;

                if (m_OldInstanceDataByGameObject.TryGetValue(instance, out InstanceData existingInstanceData))
                {
                    existingInstanceData.SetGameObject(instance);
                    m_InstanceDataBuffer.Add(existingInstanceData);
                    continue;
                }

                InstanceData createdInstanceData = new InstanceData(
                    instance,
                    m_Data,
                    m_ActionProtocol,
                    intervalToUse,
                    m_CallOnTimeZero
                );

                m_InstanceDataBuffer.Add(createdInstanceData);
            }

            List<InstanceData> oldInstanceDataList = m_InstanceData;
            m_InstanceData = m_InstanceDataBuffer;
            m_InstanceDataBuffer = oldInstanceDataList;

            m_InstanceDataBuffer.Clear();
            m_OldInstanceDataByGameObject.Clear();
        }

        private void AddGameObject(
            GameObject objectToAdd,
            List<GameObject> gameObjects,
            HashSet<GameObject> instances)
        {
            if (objectToAdd == null)
                return;

            if (!instances.Add(objectToAdd))
                return;

            gameObjects.Add(objectToAdd);
        }

        private void AddChildren(
            List<GameObject> gameObjects,
            HashSet<GameObject> instances,
            bool includeSelf)
        {
            m_TransformBuffer.Clear();

            GetComponentsInChildren(true, m_TransformBuffer);

            for (int i = 0; i < m_TransformBuffer.Count; i++)
            {
                Transform child = m_TransformBuffer[i];

                if (child == null)
                    continue;

                if (!includeSelf && child == transform)
                    continue;

                AddGameObject(child.gameObject, gameObjects, instances);
            }

            m_TransformBuffer.Clear();
        }

        private void AddParent(
            List<GameObject> gameObjects,
            HashSet<GameObject> instances)
        {
            if (transform.parent == null)
                return;

            AddGameObject(transform.parent.gameObject, gameObjects, instances);
        }

        private void AddCustomGameObjects(
            List<GameObject> gameObjects,
            HashSet<GameObject> instances)
        {
            m_ChosenGameObjects ??= new List<GameObject>();

            if (m_ChosenGameObjects.Count == 0)
            {
                AddGameObject(gameObject, gameObjects, instances);
                return;
            }

            for (int i = 0; i < m_ChosenGameObjects.Count; i++)
            {
                GameObject chosenGameObject = m_ChosenGameObjects[i];

                if (chosenGameObject == null)
                {
                    chosenGameObject = gameObject;
                }

                AddGameObject(chosenGameObject, gameObjects, instances);
            }
        }

        private void AddAllGameObjectsThatMeetCondition(
            List<GameObject> gameObjects,
            HashSet<GameObject> instances)
        {
            if (m_FilterCondition == null)
            {
                Debug.LogWarning(
                    nameof(GroupCachedGameActionExecuter) +
                    " on GameObject " +
                    gameObject.name +
                    " is set to " +
                    nameof(ExecuteOnWho.AllGameObjectsThatMeetCondition) +
                    " but has no filter condition."
                );

                return;
            }

#if UNITY_6000_0_OR_NEWER || UNITY_2023_1_OR_NEWER
            Transform[] sceneTransforms = FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );
#else
    Transform[] sceneTransforms = FindObjectsOfType<Transform>(true);
#endif

            for (int i = 0; i < sceneTransforms.Length; i++)
            {
                GameObject candidate = sceneTransforms[i].gameObject;

                if (candidate == null)
                    continue;

                if (!m_FilterCondition.IsFulfilled(candidate))
                    continue;

                AddGameObject(candidate, gameObjects, instances);
            }
        }

        public void AddGameAction(ICachedGameAction gameAction, int priorityNumber = 0)
        {
            if (gameAction == null)
                return;

            Initialize();

            gameAction.SetGameObject(gameObject);

            m_Data.Add(new ActionData(gameAction, priorityNumber));
            SortActionsByPriority();

            if (m_ActionProtocol == ActionProtocol.CloneActions)
            {
                for (int i = 0; i < m_InstanceData.Count; i++)
                {
                    InstanceData instanceData = m_InstanceData[i];
                    instanceData.CloneGameAction(gameAction, priorityNumber);
                    m_InstanceData[i] = instanceData;
                }
            }
        }

        public void RemoveGameAction(ICachedGameAction gameAction)
        {
            if (gameAction == null)
                return;

            m_Data.RemoveAll(x => x.GameAction == gameAction);

            for (int i = 0; i < m_InstanceData.Count; i++)
            {
                InstanceData instanceData = m_InstanceData[i];
                instanceData.RemoveGameAction(gameAction);
                m_InstanceData[i] = instanceData;
            }
        }

        public void ClearGameActions()
        {
            m_Data.Clear();

            for (int i = 0; i < m_InstanceData.Count; i++)
            {
                InstanceData instanceData = m_InstanceData[i];
                instanceData.ClearGameActions();
                m_InstanceData[i] = instanceData;
            }
        }

        public ICachedGameAction CloneGameAction(ICachedGameAction gameAction, int priorityNumber = 0)
        {
            if (gameAction == null)
                return null;

            Initialize();

            ICachedGameAction clone = gameAction.Clone();

            if (clone == null)
                return null;

            clone.SetGameObject(gameObject);

            m_Data.Add(new ActionData(clone, priorityNumber));
            SortActionsByPriority();

            return clone;
        }

        [Button, Title("Manual Execution")]
        public void Execute()
        {
            Initialize();

            if (m_WhenToRetrieveTargetGameObjects == WhenToRetrieveTargetGameObjects.EveryTimeBeforeCalling ||
                m_InstanceData == null ||
                m_InstanceData.Count == 0)
            {
                RetrieveGameObjects();
            }

            SortActionsByPriority();

            for (int i = 0; i < m_InstanceData.Count; i++)
            {
                InstanceData instanceData = m_InstanceData[i];
                instanceData.Execute();
                m_InstanceData[i] = instanceData;
            }
        }

        public void Execute(List<GameObject> targetGameObjects)
        {
            if (targetGameObjects == null)
            {
                return;
            }

            m_ExecuteOnWho = ExecuteOnWho.Custom;

            m_ChosenGameObjects ??= new List<GameObject>();
            m_ChosenGameObjects.Clear();

            m_ChosenGameObjects.AddRange(targetGameObjects);

            RetrieveGameObjects();
            Execute();
        }

        private void SortActionsByPriority()
        {
            m_Data ??= new List<ActionData>();
            m_Data.Sort((a, b) => a.PriorityNumber.CompareTo(b.PriorityNumber));


        }
    }
}