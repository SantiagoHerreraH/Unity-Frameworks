using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Core
{
    public enum ConditionType
    {
        AllHaveToBeTrue,
        OneHasToBeTrue,
        AllHaveToBeFalse,
        OneHasToBeFalse
    }

    public enum ReducedConditionType
    {
        AllHaveToBeTrue,
        OneHasToBeTrue,
    }

    [Serializable]
    public class ConditionGroupData : ICondition
    {
        public ConditionType ConditionType;

        [OdinSerialize, ShowInInspector]
        public List<ICondition> Conditions = new();

        public bool IsFulfilled(GameObject gameObj)
        {
            return IsFulfilled(gameObj, ConditionType, Conditions);
        }

        public static bool IsFulfilled(GameObject gameObj, ConditionType conditionType, IEnumerable<ICondition> conditions)
        {
            switch (conditionType)
            {
                case ConditionType.AllHaveToBeTrue:
                    foreach (var condition in conditions)
                        if (!condition.IsFulfilled(gameObj)) return false;
                    return true;

                case ConditionType.OneHasToBeTrue:
                    foreach (var condition in conditions)
                        if (condition.IsFulfilled(gameObj)) return true;
                    return false;

                case ConditionType.AllHaveToBeFalse:
                    foreach (var condition in conditions)
                        if (condition.IsFulfilled(gameObj)) return false;
                    return true;

                case ConditionType.OneHasToBeFalse:
                    foreach (var condition in conditions)
                        if (!condition.IsFulfilled(gameObj)) return true;
                    return false;
            }

            return false;
        }

        public static bool IsFulfilled(GameObject gameObj, ReducedConditionType conditionType, IEnumerable<ICondition> conditions)
        {
            switch (conditionType)
            {
                case ReducedConditionType.AllHaveToBeTrue:
                    foreach (var condition in conditions)
                        if (!condition.IsFulfilled(gameObj)) return false;
                    return true;

                case ReducedConditionType.OneHasToBeTrue:
                    foreach (var condition in conditions)
                        if (condition.IsFulfilled(gameObj)) return true;
                    return false;
            }

            return false;
        }
    }

    [CreateAssetMenu(fileName = "ConditionGroup", menuName = "SilverPillar/Core/Conditions/ConditionGroup")]
    public class ConditionGroup : SaveableScriptableObject, ICondition
    {
        [SerializeField]
        private ConditionGroupData m_ConditionGroupData = new();
        public bool IsFulfilled(GameObject gameObj)
        {
            return m_ConditionGroupData.IsFulfilled(gameObj);
        }
    }
}
