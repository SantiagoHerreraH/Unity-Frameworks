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

    public enum PositiveConditionType
    {
        AllHaveToBeTrue,
        OneHasToBeTrue,
    }

    [Serializable]
    public class ConditionGroup : ICondition
    {
        public ConditionType ConditionType;
        public Conditions Conditions;

        public bool IsFulfilled(GameObject gameObj)
        {
            return Conditions.IsFulfilled(gameObj, ConditionType);
        }
    }

    [Serializable]
    public class PositiveConditionGroup : ICondition
    {

        public PositiveConditionType ConditionType;
        public Conditions Conditions;

        public bool IsFulfilled(GameObject gameObj)
        {
            return Conditions.IsFulfilled(gameObj, ConditionType);
        }
    }


    [Serializable]
    public class Conditions 
    {

        [OdinSerialize, ShowInInspector]
        public List<ICondition> ConditionList = new();

        public bool IsFulfilled(GameObject gameObj, ConditionType conditionType)
        {
            return IsFulfilled(gameObj, conditionType, ConditionList);
        }

        public bool IsFulfilled(GameObject gameObj, PositiveConditionType conditionType)
        {
            return IsFulfilled(gameObj, conditionType, ConditionList);
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

        public static bool IsFulfilled(GameObject gameObj, PositiveConditionType conditionType, IEnumerable<ICondition> conditions)
        {
            switch (conditionType)
            {
                case PositiveConditionType.AllHaveToBeTrue:
                    foreach (var condition in conditions)
                        if (!condition.IsFulfilled(gameObj)) return false;
                    return true;

                case PositiveConditionType.OneHasToBeTrue:
                    foreach (var condition in conditions)
                        if (condition.IsFulfilled(gameObj)) return true;
                    return false;
            }

            return false;
        }
    }
}
