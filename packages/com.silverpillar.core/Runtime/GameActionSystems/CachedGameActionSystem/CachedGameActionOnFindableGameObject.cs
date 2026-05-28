using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Core
{
    public class SearchParams
    {
        public enum SearchOn
        {
            Self_ThenChildren,
            Self_ThenParents,
            Self_ThenChildren_ThenParents,
            Self_ThenParents_ThenChildren,
            Self_ThenParents_ThenChildren_ThenAllScene,
            Self_ThenChildren_ThenParents_ThenAllScene,
            AllScene
        }

        public enum WhatToDoIfDidntFindGameObject
        {
            SetToStart,
            SetToSelf,
            SetToCustom,
            SetToNull
        }

        public enum WhatToReturnInGetGameObject
        {
            SelfGameObject,
            StartGameObject,
            FoundGameObject
        }
    }

    [Serializable]
    public class CachedGameActionOnFindableGameObject : ICachedGameAction
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
        private ICachedGameAction m_CachedGameAction;

        private ICachedGameAction m_CloneCachedGameActionToTest;

        [Title("Debug Settings")]
        [SerializeField]
        private bool m_PrintWarningOnDidntFindTarget;

        private GameObject m_SelfGameObject;
        private GameObject m_StartGameObject;
        private GameObject m_FoundGameObject;

        public ICachedGameAction Clone()
        {
            return new CachedGameActionOnFindableGameObject
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
                m_PrintWarningOnDidntFindTarget = m_PrintWarningOnDidntFindTarget
            };
        }

        public void Execute()
        {
            if (m_CachedGameAction == null)
            {
                if (m_PrintWarningOnDidntFindTarget)
                    Debug.LogWarning($"{nameof(CachedGameActionOnFindableGameObject)} could not execute because CachedGameAction is null.", m_SelfGameObject);

                return;
            }

            m_StartGameObject = GetSearchStart();

            GameObject target = FindTargetGameObject();

            if (target == null)
            {
                if (m_PrintWarningOnDidntFindTarget)
                {
                    Debug.LogWarning(
                        $"{nameof(CachedGameActionOnFindableGameObject)} did not find a valid target. " +
                        $"SearchOn: {m_SearchOn}. " +
                        $"Start: {(m_StartGameObject != null ? m_StartGameObject.name : "null")}. " +
                        $"Self: {(m_SelfGameObject != null ? m_SelfGameObject.name : "null")}.",
                        m_SelfGameObject);
                }

                return;
            }

            m_FoundGameObject = target;

            if (!m_CachedGameAction.SetGameObject(target))
            {
                if (m_PrintWarningOnDidntFindTarget)
                {
                    Debug.LogWarning(
                        $"{nameof(CachedGameActionOnFindableGameObject)} found '{target.name}', " +
                        $"but the cached action rejected it in SetGameObject().",
                        target);
                }

                return;
            }

            m_CachedGameAction.Execute();
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
                case SearchParams.WhatToDoIfDidntFindGameObject.SetToStart:
                    return start;

                case SearchParams.WhatToDoIfDidntFindGameObject.SetToSelf:
                    return m_SelfGameObject;

                case SearchParams.WhatToDoIfDidntFindGameObject.SetToCustom:
                    return m_CustomReturnIfSearchFailed;

                case SearchParams.WhatToDoIfDidntFindGameObject.SetToNull:
                    return null;

                default:
                    return null;
            }
        }
    }
}