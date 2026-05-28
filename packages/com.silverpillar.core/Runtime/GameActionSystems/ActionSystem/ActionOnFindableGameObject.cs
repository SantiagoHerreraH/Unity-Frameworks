using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class ActionOnFindableGameObject : IAction
    {
        [Title("Search Params")]
        [SerializeField]
        private SelfType m_WhereToStartSearch;

        [SerializeField, ShowIf(nameof(m_WhereToStartSearch), SelfType.CustomGameObject)]
        private GameObject m_CustomSearchStart;

        [SerializeField]
        private SearchParams.SearchOn m_SearchOn;

        [Title("Side Cases")]
        [SerializeField]
        private SearchParams.WhatToDoIfDidntFindGameObject m_WhatToDoIfDidntFindGameObject;

        [SerializeField, ShowIf(nameof(m_WhatToDoIfDidntFindGameObject), SearchParams.WhatToDoIfDidntFindGameObject.SetToCustom)]
        private GameObject m_CustomReturnIfSearchFailed;

        [SerializeField]
        private SearchParams.WhatToReturnInGetGameObject m_WhatToReturnInGetGameObject;

        [Title("Find Condition")]
        [OdinSerialize, ShowInInspector]
        private ICachedCondition m_Condition;

        [Title("Action")]
        [OdinSerialize, ShowInInspector]
        private IAction m_CachedGameAction;

        private IAction m_CloneCachedGameActionToTest;


        [Title("Debug Settings")]
        [SerializeField]
        private bool m_PrintWarningOnDidntFindTarget;

        private GameObject m_SelfGameObject;
        private GameObject m_StartGameObject;
        private GameObject m_FoundGameObject;

        private bool m_CanProceedToExecute = false;

        public IAction Clone()
        {
            return new ActionOnFindableGameObject
            {
                m_WhereToStartSearch = m_WhereToStartSearch,
                m_CustomSearchStart = m_CustomSearchStart,
                m_SearchOn = m_SearchOn,
                m_WhatToDoIfDidntFindGameObject = m_WhatToDoIfDidntFindGameObject,
                m_CustomReturnIfSearchFailed = m_CustomReturnIfSearchFailed,
                m_WhatToReturnInGetGameObject = m_WhatToReturnInGetGameObject,
                m_Condition = m_Condition?.Clone(),
                m_CachedGameAction = m_CachedGameAction?.Clone(),
                m_SelfGameObject = m_SelfGameObject,
                m_StartGameObject = m_StartGameObject,
                m_FoundGameObject = m_FoundGameObject,
                m_PrintWarningOnDidntFindTarget = m_PrintWarningOnDidntFindTarget,
                m_CanProceedToExecute = m_CanProceedToExecute
            };
        }

        public void StartAction()
        {
            FindTarget();
            ExecuteOnFoundTarget(ActionMoment.Start);
        }

        public void UpdateAction()
        {
            ExecuteOnFoundTarget(ActionMoment.Update);
        }

        public void EndAction()
        {
            ExecuteOnFoundTarget(ActionMoment.End);
        }

        public GameObject GetGameObject()
        {
            switch (m_WhatToReturnInGetGameObject)
            {
                case SearchParams.WhatToReturnInGetGameObject.SelfGameObject:
                    return m_SelfGameObject;

                case SearchParams.WhatToReturnInGetGameObject.StartGameObject:
                    return m_StartGameObject != null ? m_StartGameObject : GetSearchStart();

                case SearchParams.WhatToReturnInGetGameObject.FoundGameObject:
                    return m_FoundGameObject;

                default:
                    return m_FoundGameObject;
            }
        }

        public bool SetGameObject(GameObject gameObj)
        {
            m_SelfGameObject = gameObj;
            m_StartGameObject = GetSearchStart();
            m_FoundGameObject = null;
            m_CloneCachedGameActionToTest = null;

            return gameObj != null;
        }

        private void FindTarget()
        {
            m_CanProceedToExecute = false;

            if (m_CachedGameAction == null)
            {
                PrintWarning(
                    $"{nameof(ActionOnFindableGameObject)} could not find target because action is null.",
                    m_SelfGameObject);

                return;
            }

            m_StartGameObject = GetSearchStart();

            GameObject target = FindTargetGameObject();

            if (target == null)
            {
                PrintWarning(
                    $"{nameof(ActionOnFindableGameObject)} did not find a valid target. " +
                    $"SearchOn: {m_SearchOn}. " +
                    $"Start: {(m_StartGameObject != null ? m_StartGameObject.name : "null")}. " +
                    $"Self: {(m_SelfGameObject != null ? m_SelfGameObject.name : "null")}.",
                    m_SelfGameObject);

                return;
            }

            m_FoundGameObject = target;

            m_CanProceedToExecute = m_CachedGameAction.SetGameObject(target);

            if (!m_CanProceedToExecute)
            {
                PrintWarning(
                    $"{nameof(ActionOnFindableGameObject)} found '{target.name}', " +
                    $"but the action rejected it in SetGameObject().",
                    target);
            }
        }

        private enum ActionMoment
        {
            Start,
            Update,
            End
        }

        private void ExecuteOnFoundTarget(ActionMoment actionMoment)
        {
            if (!m_CanProceedToExecute)
            {
                PrintWarning(
                    $"{nameof(ActionOnFindableGameObject)} could not execute {actionMoment} because no valid target has been found. " +
                    $"Call StartAction first, or check the search settings.",
                    m_SelfGameObject);

                return;
            }

            switch (actionMoment)
            {
                case ActionMoment.Start:
                    m_CachedGameAction.StartAction();
                    break;

                case ActionMoment.Update:
                    m_CachedGameAction.UpdateAction();
                    break;

                case ActionMoment.End:
                    m_CachedGameAction.EndAction();
                    break;
            }
        }

        private GameObject FindTargetGameObject()
        {
            GameObject start = GetSearchStart();
            m_StartGameObject = start;

            switch (m_SearchOn)
            {
                case SearchParams.SearchOn.Self_ThenChildren:
                    return FindInSelf(start)
                           ?? FindInChildren(start)
                           ?? GetFallback(start);

                case SearchParams.SearchOn.Self_ThenParents:
                    return FindInSelf(start)
                           ?? FindInParents(start)
                           ?? GetFallback(start);

                case SearchParams.SearchOn.Self_ThenChildren_ThenParents:
                    return FindInSelf(start)
                           ?? FindInChildren(start)
                           ?? FindInParents(start)
                           ?? GetFallback(start);

                case SearchParams.SearchOn.Self_ThenParents_ThenChildren:
                    return FindInSelf(start)
                           ?? FindInParents(start)
                           ?? FindInChildren(start)
                           ?? GetFallback(start);

                case SearchParams.SearchOn.Self_ThenParents_ThenChildren_ThenAllScene:
                    return FindInSelf(start)
                           ?? FindInParents(start)
                           ?? FindInChildren(start)
                           ?? FindInAllScene()
                           ?? GetFallback(start);

                case SearchParams.SearchOn.Self_ThenChildren_ThenParents_ThenAllScene:
                    return FindInSelf(start)
                           ?? FindInChildren(start)
                           ?? FindInParents(start)
                           ?? FindInAllScene()
                           ?? GetFallback(start);

                case SearchParams.SearchOn.AllScene:
                    return FindInAllScene()
                           ?? GetFallback(start);

                default:
                    return GetFallback(start);
            }
        }

        private GameObject GetSearchStart()
        {
            switch (m_WhereToStartSearch)
            {
                case SelfType.ThisGameObject:
                    return m_SelfGameObject;

                case SelfType.CustomGameObject:
                    return m_CustomSearchStart;

                default:
                    return m_SelfGameObject;
            }
        }

        private GameObject FindInSelf(GameObject target)
        {
            return IsValidTarget(target) ? target : null;
        }

        private GameObject FindInChildren(GameObject start)
        {
            if (start == null)
                return null;

            Transform[] children = start.GetComponentsInChildren<Transform>(true);

            foreach (Transform child in children)
            {
                if (child == null)
                    continue;

                if (child.gameObject == start)
                    continue;

                if (IsValidTarget(child.gameObject))
                    return child.gameObject;
            }

            return null;
        }

        private GameObject FindInParents(GameObject start)
        {
            if (start == null)
                return null;

            Transform current = start.transform.parent;

            while (current != null)
            {
                if (IsValidTarget(current.gameObject))
                    return current.gameObject;

                current = current.parent;
            }

            return null;
        }

        private GameObject FindInAllScene()
        {
            GameObject[] allObjects = UnityEngine.Object.FindObjectsByType<GameObject>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (GameObject obj in allObjects)
            {
                if (IsValidTarget(obj))
                    return obj;
            }

            return null;
        }

        private bool IsValidTarget(GameObject target)
        {
            if (target == null)
                return false;

            if (!CanActionUseTarget(target))
                return false;

            if (m_Condition == null)
                return true;

            if (!m_Condition.SetGameObject(target))
                return false;

            return m_Condition.IsFulfilled();
        }

        private bool CanActionUseTarget(GameObject target)
        {
            if (m_CachedGameAction == null || target == null)
                return false;

            if (m_CloneCachedGameActionToTest == null)
                m_CloneCachedGameActionToTest = m_CachedGameAction.Clone();

            return m_CloneCachedGameActionToTest != null &&
                   m_CloneCachedGameActionToTest.SetGameObject(target);
        }

        private GameObject GetFallback(GameObject start)
        {
            switch (m_WhatToDoIfDidntFindGameObject)
            {
                case SearchParams.WhatToDoIfDidntFindGameObject.SetToSelf:
                    return m_SelfGameObject;

                case SearchParams.WhatToDoIfDidntFindGameObject.SetToStart:
                    return start;

                case SearchParams.WhatToDoIfDidntFindGameObject.SetToCustom:
                    return m_CustomReturnIfSearchFailed;

                case SearchParams.WhatToDoIfDidntFindGameObject.SetToNull:
                    return null;

                default:
                    return null;
            }
        }

        private void PrintWarning(string message, UnityEngine.Object context)
        {
            if (!m_PrintWarningOnDidntFindTarget)
                return;

            Debug.LogWarning(message, context);
        }
    }
}