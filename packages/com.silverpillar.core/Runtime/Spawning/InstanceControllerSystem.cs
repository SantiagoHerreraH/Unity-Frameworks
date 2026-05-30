using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SilverPillar.Core
{
    public enum WhatPrefabToUse
    {
        PrefabForAllInstances,
        ThisGameObject,
        Custom
    }

    public enum WhatHappensWhenLoopEnds
    {
        RecallStart,
        OnlyCallCachedActionStart,
        OnlyCallIActionStart,
        OnlyCallEventStart,
        Nothing,
        CreateInstancesAgain
    }

    public enum BehaviorOnChangeData
    {
        SetFirstAsCurrentInstance,
        EachDataRemembersTheirOwnCurrentInstanceAndDoesntReset
    }

    public enum BehaviorOnReachEndOfInstanceListInData
    {
        CreateFirstInstanceOfNextData,
        CreateFirstInstanceOfCurrentData
    }

    public class InstanceControllerSystem : SerializedMonoBehaviour
    {
        public enum ExecutionOrder
        {
            CachedAction_Action_Event,
            CachedAction_Event_Action,
            Event_CachedAction_Action,
            Event_Action_CachedAction,
            Action_CachedAction_Event,
            Action_Event_CachedAction,
        }

        [Serializable]
        public class InstanceData
        {
            public GameObject Instance { get; private set; }

            private List<IAction> m_Actions;
            private List<ICachedGameAction> m_StartActions;
            private List<ICachedGameAction> m_UpdateActions;
            private List<ICachedGameAction> m_EndActions;

            public InstanceData(
                GameObject instance,
                List<ICachedGameAction> startActions,
                List<ICachedGameAction> updateActions,
                List<ICachedGameAction> endActions,
                List<IAction> actions)
            {
                Instance = instance;

                m_StartActions = CloneCachedActions(startActions, instance);
                m_UpdateActions = CloneCachedActions(updateActions, instance);
                m_EndActions = CloneCachedActions(endActions, instance);
                m_Actions = CloneActions(actions, instance);
            }

            public void Start(ExecutionOrder executionOrder, UnityEvent<GameObject> unityEvent)
            {
                Execute(executionOrder, m_StartActions, ActionExecutionMoment.Start, unityEvent);
            }

            public void Update(ExecutionOrder executionOrder, UnityEvent<GameObject> unityEvent)
            {
                Execute(executionOrder, m_UpdateActions, ActionExecutionMoment.Update, unityEvent);
            }

            public void End(ExecutionOrder executionOrder, UnityEvent<GameObject> unityEvent)
            {
                Execute(executionOrder, m_EndActions, ActionExecutionMoment.End, unityEvent);
            }

            public void ExecuteOnlyCachedStart()
            {
                ExecuteCachedActions(m_StartActions);
            }

            public void ExecuteOnlyActionStart()
            {
                ExecuteActionsStart(m_Actions);
            }

            private enum ActionExecutionMoment
            {
                Start,
                Update,
                End
            }

            private void Execute(
                ExecutionOrder executionOrder,
                List<ICachedGameAction> cachedActions,
                ActionExecutionMoment actionExecutionMoment,
                UnityEvent<GameObject> unityEvent)
            {
                if (Instance == null)
                    return;

                switch (executionOrder)
                {
                    case ExecutionOrder.CachedAction_Action_Event:
                        ExecuteCachedActions(cachedActions);
                        ExecuteActions(actionExecutionMoment);
                        unityEvent?.Invoke(Instance);
                        break;

                    case ExecutionOrder.CachedAction_Event_Action:
                        ExecuteCachedActions(cachedActions);
                        unityEvent?.Invoke(Instance);
                        ExecuteActions(actionExecutionMoment);
                        break;

                    case ExecutionOrder.Event_CachedAction_Action:
                        unityEvent?.Invoke(Instance);
                        ExecuteCachedActions(cachedActions);
                        ExecuteActions(actionExecutionMoment);
                        break;

                    case ExecutionOrder.Event_Action_CachedAction:
                        unityEvent?.Invoke(Instance);
                        ExecuteActions(actionExecutionMoment);
                        ExecuteCachedActions(cachedActions);
                        break;

                    case ExecutionOrder.Action_CachedAction_Event:
                        ExecuteActions(actionExecutionMoment);
                        ExecuteCachedActions(cachedActions);
                        unityEvent?.Invoke(Instance);
                        break;

                    case ExecutionOrder.Action_Event_CachedAction:
                        ExecuteActions(actionExecutionMoment);
                        unityEvent?.Invoke(Instance);
                        ExecuteCachedActions(cachedActions);
                        break;
                }
            }

            private void ExecuteActions(ActionExecutionMoment actionExecutionMoment)
            {
                switch (actionExecutionMoment)
                {
                    case ActionExecutionMoment.Start:
                        ExecuteActionsStart(m_Actions);
                        break;

                    case ActionExecutionMoment.Update:
                        ExecuteActionsUpdate(m_Actions);
                        break;

                    case ActionExecutionMoment.End:
                        ExecuteActionsEnd(m_Actions);
                        break;
                }
            }

            private static List<ICachedGameAction> CloneCachedActions(
                List<ICachedGameAction> actions,
                GameObject target)
            {
                if (actions == null)
                    return null;

                List<ICachedGameAction> clonedActions = new(actions.Count);

                foreach (ICachedGameAction action in actions)
                {
                    if (action == null)
                        continue;

                    ICachedGameAction clone = action.Clone();
                    clone.SetGameObject(target);
                    clonedActions.Add(clone);
                }

                return clonedActions;
            }

            private static List<IAction> CloneActions(
                List<IAction> actions,
                GameObject target)
            {
                if (actions == null)
                    return null;

                List<IAction> clonedActions = new(actions.Count);

                foreach (IAction action in actions)
                {
                    if (action == null)
                        continue;

                    IAction clone = action.Clone();
                    clone.SetGameObject(target);
                    clonedActions.Add(clone);
                }

                return clonedActions;
            }

            private static void ExecuteCachedActions(List<ICachedGameAction> actions)
            {
                if (actions == null)
                    return;

                foreach (ICachedGameAction action in actions)
                    action?.Execute();
            }

            private static void ExecuteActionsStart(List<IAction> actions)
            {
                if (actions == null)
                    return;

                foreach (IAction action in actions)
                    action?.StartAction();
            }

            private static void ExecuteActionsUpdate(List<IAction> actions)
            {
                if (actions == null)
                    return;

                foreach (IAction action in actions)
                    action?.UpdateAction();
            }

            private static void ExecuteActionsEnd(List<IAction> actions)
            {
                if (actions == null)
                    return;

                foreach (IAction action in actions)
                    action?.EndAction();
            }
        }

        [Serializable]
        public class PrefabData
        {
            [FoldoutGroup("Data")]
            [SerializeField]
            private WhatPrefabToUse m_WhatPrefabToUse;

            [FoldoutGroup("Data")]
            [SerializeField, ShowIf(nameof(m_WhatPrefabToUse), WhatPrefabToUse.Custom)]
            private GameObject m_CustomPrefab;

            [FoldoutGroup("Data")]
            [SerializeField, Min(1)]
            private int m_NumberOfInstances = 1;

            [FoldoutGroup("Actions")]
            [OdinSerialize, ShowInInspector]
            private List<ICachedGameAction> m_StartActions;

            [FoldoutGroup("Actions")]
            [OdinSerialize, ShowInInspector]
            private List<ICachedGameAction> m_UpdateActions;

            [FoldoutGroup("Actions")]
            [OdinSerialize, ShowInInspector]
            private List<ICachedGameAction> m_EndActions;

            [FoldoutGroup("Actions")]
            [OdinSerialize, ShowInInspector]
            private List<IAction> m_Actions;

            [FoldoutGroup("Events")]
            [SerializeField]
            private UnityEvent<GameObject> m_OnStart;

            [FoldoutGroup("Events")]
            [SerializeField]
            private UnityEvent<GameObject> m_OnUpdate;

            [FoldoutGroup("Events")]
            [SerializeField]
            private UnityEvent<GameObject> m_OnEnd;

            [FoldoutGroup("Action Settings")]
            [SerializeField]
            private ExecutionOrder m_ExecutionOrderOnStart;

            [FoldoutGroup("Action Settings")]
            [SerializeField]
            private ExecutionOrder m_ExecutionOrderOnUpdate;

            [FoldoutGroup("Action Settings")]
            [SerializeField]
            private ExecutionOrder m_ExecutionOrderOnEnd;

            private List<InstanceData> m_CreatedInstances = new();

            private int m_CurrentInstanceIndex = -1;

            public WhatPrefabToUse WhatPrefabToUse => m_WhatPrefabToUse;
            public GameObject CustomPrefab => m_CustomPrefab;
            public int NumberOfInstances => m_NumberOfInstances;
            public int CreatedInstancesCount => m_CreatedInstances.Count;

            public GameObject CurrentInstance
            {
                get
                {
                    if (m_CreatedInstances.Count == 0)
                        return null;

                    if (m_CurrentInstanceIndex < 0 || m_CurrentInstanceIndex >= m_CreatedInstances.Count)
                        return null;

                    return m_CreatedInstances[m_CurrentInstanceIndex].Instance;
                }
            }

            public IReadOnlyList<InstanceData> CreatedInstances => m_CreatedInstances;

            public PrefabData CloneData()
            {
                return new PrefabData
                {
                    m_WhatPrefabToUse = m_WhatPrefabToUse,
                    m_CustomPrefab = m_CustomPrefab,
                    m_NumberOfInstances = m_NumberOfInstances,

                    m_StartActions = CloneCachedActionList(m_StartActions),
                    m_UpdateActions = CloneCachedActionList(m_UpdateActions),
                    m_EndActions = CloneCachedActionList(m_EndActions),
                    m_Actions = CloneActionList(m_Actions),

                    m_OnStart = m_OnStart,
                    m_OnUpdate = m_OnUpdate,
                    m_OnEnd = m_OnEnd,

                    m_ExecutionOrderOnStart = m_ExecutionOrderOnStart,
                    m_ExecutionOrderOnUpdate = m_ExecutionOrderOnUpdate,
                    m_ExecutionOrderOnEnd = m_ExecutionOrderOnEnd,
                };
            }

            private static List<ICachedGameAction> CloneCachedActionList(List<ICachedGameAction> original)
            {
                if (original == null)
                    return null;

                List<ICachedGameAction> clone = new(original.Count);

                foreach (ICachedGameAction action in original)
                    clone.Add(action?.Clone());

                return clone;
            }

            private static List<IAction> CloneActionList(List<IAction> original)
            {
                if (original == null)
                    return null;

                List<IAction> clone = new(original.Count);

                foreach (IAction action in original)
                    clone.Add(action?.Clone());

                return clone;
            }
            public void SetFirstAsCurrent()
            {
                if (m_CreatedInstances == null || m_CreatedInstances.Count == 0)
                {
                    return;
                }

                m_CurrentInstanceIndex = m_CreatedInstances.Count > 0 ? 0 : -1;
            }

            public void SetNextAsCurrent()
            {
                if (m_CreatedInstances.Count == 0)
                {
                    m_CurrentInstanceIndex = -1;
                    return;
                }

                m_CurrentInstanceIndex++;

                if (m_CurrentInstanceIndex >= m_CreatedInstances.Count)
                    m_CurrentInstanceIndex = m_CreatedInstances.Count - 1;
            }

            public void SetLastAsCurrent()
            {
                m_CurrentInstanceIndex = m_CreatedInstances.Count > 0 ? m_CreatedInstances.Count - 1 : -1;
            }

            public void CreateAll(InstanceControllerSystem controller, bool callStart)
            {
                if (m_CreatedInstances == null)
                {
                    m_CreatedInstances = new();
                }
                while (m_CreatedInstances.Count < m_NumberOfInstances)
                    CreateNext(controller, callStart);

                if (m_CurrentInstanceIndex < 0)
                    SetFirstAsCurrent();
            }

            public GameObject CreateNext(InstanceControllerSystem controller, bool callStart)
            {
                if (controller == null)
                    return null;

                GameObject prefab = controller.GetPrefabForData(this);

                if (prefab == null)
                {
                    Debug.LogError($"No prefab found on {controller.name}.", controller);
                    return null;
                }

                GameObject instance = Instantiate(
                    prefab,
                    controller.transform.position,
                    controller.transform.rotation);

                InstanceData instanceData = new(
                    instance,
                    m_StartActions,
                    m_UpdateActions,
                    m_EndActions,
                    m_Actions);

                m_CreatedInstances.Add(instanceData);
                m_CurrentInstanceIndex = m_CreatedInstances.Count - 1;

                if (callStart)
                    instanceData.Start(m_ExecutionOrderOnStart, m_OnStart);

                return instance;
            }

            public void StartAll()
            {
                foreach (InstanceData instance in m_CreatedInstances)
                    instance.Start(m_ExecutionOrderOnStart, m_OnStart);
            }

            public void UpdateAll()
            {
                foreach (InstanceData instance in m_CreatedInstances)
                    instance.Update(m_ExecutionOrderOnUpdate, m_OnUpdate);
            }

            public void EndAll()
            {
                foreach (InstanceData instance in m_CreatedInstances)
                    instance.End(m_ExecutionOrderOnEnd, m_OnEnd);
            }

            public void StartCurrent()
            {
                GetCurrentInstanceData()?.Start(m_ExecutionOrderOnStart, m_OnStart);
            }

            public void UpdateCurrent()
            {
                GetCurrentInstanceData()?.Update(m_ExecutionOrderOnUpdate, m_OnUpdate);
            }

            public void EndCurrent()
            {
                GetCurrentInstanceData()?.End(m_ExecutionOrderOnEnd, m_OnEnd);
            }

            public void StartAllOnlyCachedActions()
            {
                foreach (InstanceData instance in m_CreatedInstances)
                {
                    instance.ExecuteOnlyCachedStart();
                }
            }

            public void StartAllOnlyActions()
            {
                foreach (InstanceData instance in m_CreatedInstances)
                {
                    instance.ExecuteOnlyActionStart();
                }
            }

            public void StartAllOnlyEvents()
            {
                foreach (InstanceData instance in m_CreatedInstances)
                {
                    m_OnStart.Invoke(instance.Instance);
                }
            }

            private InstanceData GetCurrentInstanceData()
            {
                if (m_CurrentInstanceIndex < 0 || m_CurrentInstanceIndex >= m_CreatedInstances.Count)
                    return null;

                return m_CreatedInstances[m_CurrentInstanceIndex];
            }
        }

        [Title("Instance Data")]
        [SerializeField]
        private GameObject m_PrefabForAllInstances;

        [OdinSerialize, ShowInInspector]
        private List<PrefabData> m_InstanceData = new();


        [Title("Data Tools")]
        [Button(ButtonSizes.Medium)]
        private void CloneData(int dataIndex, int numberOfCopies)
        {
            if (m_InstanceData == null || m_InstanceData.Count == 0)
                return;

            if (dataIndex < 0 || dataIndex >= m_InstanceData.Count)
            {
                Debug.LogWarning($"{nameof(CloneData)} failed. Index {dataIndex} is outside the Instance Data list.", this);
                return;
            }

            if (numberOfCopies <= 0)
                return;

            PrefabData dataToClone = m_InstanceData[dataIndex];

            if (dataToClone == null)
                return;

            int insertIndex = dataIndex + 1;

            for (int i = 0; i < numberOfCopies; i++)
            {
                PrefabData clonedData = dataToClone.CloneData();
                m_InstanceData.Insert(insertIndex + i, clonedData);
            }

            m_RandomizedDataOrder.Clear();
            m_CurrentRandomizedIndex = -1;
        }

        [Title("Actions On All")]
        [OdinSerialize, ShowInInspector]
        private List<ICachedGameAction> m_StartActionsOnAll = new();

        [OdinSerialize, ShowInInspector]
        private List<ICachedGameAction> m_UpdateActionsOnAll = new();

        [OdinSerialize, ShowInInspector]
        private List<ICachedGameAction> m_EndActionsOnAll = new();

        [Title("Loop Settings")]
        [InfoBox("Loop as in called create next and already created all data instances.")]
        [SerializeField]
        private WhatHappensWhenLoopEnds m_WhatHappensWhenLoopEnds;

        [SerializeField]
        [ShowIf(nameof(m_WhatHappensWhenLoopEnds), WhatHappensWhenLoopEnds.CreateInstancesAgain), Tooltip("Negative numbers mean infinite times.")]
        private int m_NumberOfTimesThatCanCreateInstancesAgain = -1;

        [SerializeField]
        private BehaviorOnChangeData m_BehaviorOnChangeData;

        [SerializeField]
        private BehaviorOnReachEndOfInstanceListInData m_BehaviorOnReachEndOfInstanceListInData;

        public enum WhenToCallStart
        {
            AfterInstancing,
            OnEnable,
            Both,
            DontAutoCall
        }

        public enum WhenToCallUpdate
        {
            OnUpdate,
            OnFixedUpdate,
            OnLateUpdate,
            DontAutoCall
        }

        public enum WhenToCallEnd
        {
            OnDisable,
            OnDestroy,
            DontAutoCall
        }

        [Title("Auto Calling")]

        [SerializeField, Tooltip("Start is called on Instancing")]
        private WhenToCallStart m_WhenCallStart;

        [SerializeField]
        private WhenToCallUpdate m_WhenToCallUpdate;

        [SerializeField]
        private WhenToCallEnd m_WhenToCallEnd;

        private int m_CurrentDataIndex = -1;
        private int m_TimesCreatedInstancesAgain;

        private List<int> m_RandomizedDataOrder = new();
        private int m_CurrentRandomizedIndex = -1;

        private PrefabData CurrentData
        {
            get
            {
                if (!HasData())
                    return null;

                if (m_CurrentDataIndex < 0 || m_CurrentDataIndex >= m_InstanceData.Count)
                    return null;

                return m_InstanceData[m_CurrentDataIndex];
            }
        }

        public GameObject CurrentInstance => CurrentData?.CurrentInstance;

        public GameObject GetPrefabForData(PrefabData data)
        {
            if (data == null)
                return null;

            switch (data.WhatPrefabToUse)
            {
                case WhatPrefabToUse.PrefabForAllInstances:
                    return m_PrefabForAllInstances;

                case WhatPrefabToUse.ThisGameObject:
                    return gameObject;

                case WhatPrefabToUse.Custom:
                    return data.CustomPrefab;

                default:
                    return null;
            }
        }
        [FoldoutGroup("Functions")]
        [Button, BoxGroup("Functions/Data Creation")]
        public void CreateFirstData()
        {
            if (!HasData())
                return;

            SetCurrentData(0);
            CurrentData.CreateAll(this, CanCallStartOnCreate());
            CurrentData.SetFirstAsCurrent();
        }

        [Button, BoxGroup("Functions/Data Creation")]
        public void CreateNextData()
        {
            if (!HasData())
                return;

            int nextIndex = m_CurrentDataIndex + 1;

            if (nextIndex >= m_InstanceData.Count)
            {
                HandleLoopEnd();
                return;
            }

            SetCurrentData(nextIndex);
            CurrentData.CreateAll(this, CanCallStartOnCreate());
        }

        [Button, BoxGroup("Functions/Data Creation")]
        public void CreateLastData()
        {
            if (!HasData())
                return;

            SetCurrentData(m_InstanceData.Count - 1);
            CurrentData.CreateAll(this, CanCallStartOnCreate());
            CurrentData.SetLastAsCurrent();
        }

        [Button, BoxGroup("Functions/Data Creation")]
        public void CreateNextRandomData()
        {
            if (!HasData())
                return;

            if (ShouldGenerateRandomizedOrder())
                GenerateRandomizedOrder();

            int randomizedDataIndex = m_RandomizedDataOrder[m_CurrentRandomizedIndex];

            SetCurrentData(randomizedDataIndex);
            CurrentData.CreateAll(this , CanCallStartOnCreate());

            m_CurrentRandomizedIndex++;
        }

        [Button, BoxGroup("Functions/Data Creation")]
        public void CreateAllData()
        {
            if (!HasData())
                return;

            for (int i = 0; i < m_InstanceData.Count; i++)
            {
                SetCurrentData(i);
                m_InstanceData[i].CreateAll(this, CanCallStartOnCreate());
            }

            SetCurrentData(0);
        }

        [Button, BoxGroup("Functions/Instance Creation")]
        public void CreateFirstInstance()
        {
            if (!EnsureCurrentData())
                return;

            CurrentData.CreateNext(this, CanCallStartOnCreate());
            CurrentData.SetFirstAsCurrent();
        }

        [Button, BoxGroup("Functions/Instance Creation")]
        public void CreateNextInstance()
        {
            if (!EnsureCurrentData())
                return;

            if (CurrentData.CreatedInstancesCount < CurrentData.NumberOfInstances)
            {
                CurrentData.CreateNext(this, CanCallStartOnCreate());
                return;
            }

            HandleReachEndOfInstanceList();
        }

        [Button, BoxGroup("Functions/Instance Creation")]
        public void CreateLastInstance()
        {
            if (!EnsureCurrentData())
                return;

            while (CurrentData.CreatedInstancesCount < CurrentData.NumberOfInstances)
                CurrentData.CreateNext(this, CanCallStartOnCreate());

            CurrentData.SetLastAsCurrent();
        }


        [Button, BoxGroup("Functions/Execution")]
        public void StartAll()
        {
            ExecuteActionsOnAllInstances(m_StartActionsOnAll);

            foreach (PrefabData data in m_InstanceData)
                data?.StartAll();
        }

        [Button, BoxGroup("Functions/Execution")]
        public void UpdateAll()
        {
            ExecuteActionsOnAllInstances(m_UpdateActionsOnAll);

            foreach (PrefabData data in m_InstanceData)
                data?.UpdateAll();
        }

        [Button, BoxGroup("Functions/Execution")]
        public void EndAll()
        {
            ExecuteActionsOnAllInstances(m_EndActionsOnAll);

            foreach (PrefabData data in m_InstanceData)
                data?.EndAll();
        }

        private bool CanCallStartOnCreate()
        {
            return m_WhenCallStart == WhenToCallStart.AfterInstancing || m_WhenCallStart == WhenToCallStart.Both;
        }

        private void SetCurrentData(int index)
        {
            if (!HasData())
                return;

            m_CurrentDataIndex = Mathf.Clamp(index, 0, m_InstanceData.Count - 1);

            if (m_BehaviorOnChangeData == BehaviorOnChangeData.SetFirstAsCurrentInstance)
                CurrentData.SetFirstAsCurrent();
        }

        private void HandleReachEndOfInstanceList()
        {
            switch (m_BehaviorOnReachEndOfInstanceListInData)
            {
                case BehaviorOnReachEndOfInstanceListInData.CreateFirstInstanceOfNextData:
                    CreateNextData();
                    CreateFirstInstance();
                    break;

                case BehaviorOnReachEndOfInstanceListInData.CreateFirstInstanceOfCurrentData:
                    CurrentData.SetFirstAsCurrent();
                    break;
            }
        }

        private void HandleLoopEnd()
        {
            switch (m_WhatHappensWhenLoopEnds)
            {
                case WhatHappensWhenLoopEnds.RecallStart:
                    StartAll();
                    break;

                case WhatHappensWhenLoopEnds.OnlyCallCachedActionStart:
                    ExecuteActionsOnAllInstances(m_StartActionsOnAll);

                    foreach (PrefabData data in m_InstanceData)
                        data?.StartAllOnlyCachedActions();
                    break;

                case WhatHappensWhenLoopEnds.OnlyCallIActionStart:
                    foreach (PrefabData data in m_InstanceData)
                        data?.StartAllOnlyActions();
                    break;

                case WhatHappensWhenLoopEnds.OnlyCallEventStart:
                    foreach (PrefabData data in m_InstanceData)
                        data?.StartAllOnlyEvents();
                    break;
                case WhatHappensWhenLoopEnds.Nothing:
                    break;

                case WhatHappensWhenLoopEnds.CreateInstancesAgain:
                    if (CanCreateInstancesAgain())
                    {
                        m_TimesCreatedInstancesAgain++;
                        CreateAllData();
                    }
                    break;
            }
        }

        private bool CanCreateInstancesAgain()
        {
            return m_NumberOfTimesThatCanCreateInstancesAgain < 0 ||
                   m_TimesCreatedInstancesAgain < m_NumberOfTimesThatCanCreateInstancesAgain;
        }

        private bool HasData()
        {
            return m_InstanceData != null && m_InstanceData.Count > 0;
        }

        private bool EnsureCurrentData()
        {
            if (!HasData())
                return false;

            if (CurrentData == null)
                SetCurrentData(0);

            return CurrentData != null;
        }

        private bool ShouldGenerateRandomizedOrder()
        {
            return m_RandomizedDataOrder.Count != m_InstanceData.Count ||
                   m_CurrentRandomizedIndex < 0 ||
                   m_CurrentRandomizedIndex >= m_RandomizedDataOrder.Count;
        }

        private void GenerateRandomizedOrder()
        {
            m_RandomizedDataOrder.Clear();

            for (int i = 0; i < m_InstanceData.Count; i++)
                m_RandomizedDataOrder.Add(i);

            for (int i = 0; i < m_RandomizedDataOrder.Count; i++)
            {
                int randomIndex = UnityEngine.Random.Range(i, m_RandomizedDataOrder.Count);

                (m_RandomizedDataOrder[i], m_RandomizedDataOrder[randomIndex]) =
                    (m_RandomizedDataOrder[randomIndex], m_RandomizedDataOrder[i]);
            }

            m_CurrentRandomizedIndex = 0;
        }

        private void ExecuteActionsOnAllInstances(List<ICachedGameAction> actions)
        {
            if (actions == null)
                return;

            foreach (PrefabData data in m_InstanceData)
            {
                if (data == null)
                    continue;

                foreach (InstanceData instanceData in data.CreatedInstances)
                {
                    if (instanceData?.Instance == null)
                        continue;

                    ExecuteCachedActionsOnTarget(actions, instanceData.Instance);
                }
            }
        }

        private static void ExecuteCachedActionsOnTarget(
            List<ICachedGameAction> actions,
            GameObject target)
        {
            if (actions == null || target == null)
                return;

            foreach (ICachedGameAction action in actions)
            {
                if (action == null)
                    continue;

                ICachedGameAction clone = action.Clone();
                clone.SetGameObject(target);
                clone.Execute();
            }
        }
    }
}