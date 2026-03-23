using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class CachedConditionGroup : ICachedCondition
    {
        public ConditionType ConditionType;
        public CachedConditions Conditions = new();

        public bool SetGameObject(GameObject gameObj) => Conditions.SetGameObject(gameObj);
        public GameObject GetGameObject() => Conditions.GetGameObject();
        public bool IsFulfilled() => Conditions.IsFulfilled(ConditionType);

        public ICachedCondition Clone() => new CachedConditionGroup
        {
            ConditionType = this.ConditionType,
            Conditions = this.Conditions.Clone()
        };
    }

    [Serializable]
    public class CachedPositiveConditionGroup : ICachedCondition
    {
        public PositiveConditionType ConditionType;
        public CachedConditions Conditions = new();

        public bool SetGameObject(GameObject gameObj) => Conditions.SetGameObject(gameObj);
        public GameObject GetGameObject() => Conditions.GetGameObject();
        public bool IsFulfilled() => Conditions.IsFulfilled(ConditionType);

        public ICachedCondition Clone() => new CachedPositiveConditionGroup
        {
            ConditionType = this.ConditionType,
            Conditions = this.Conditions.Clone()
        };
    }

    [Serializable]
    public class CachedConditions
    {
        [OdinSerialize, ShowInInspector]
        public List<ICachedCondition> ConditionList = new();

        private GameObject _cachedGameObject;

        public bool SetGameObject(GameObject gameObj)
        {
            _cachedGameObject = gameObj;
            bool allSucceeded = true;
            foreach (var condition in ConditionList)
            {
                if (!condition.SetGameObject(gameObj)) allSucceeded = false;
            }
            return allSucceeded;
        }

        public GameObject GetGameObject() => _cachedGameObject;

        public bool IsFulfilled(ConditionType type) => IsFulfilled(type, ConditionList);
        public bool IsFulfilled(PositiveConditionType type) => IsFulfilled(type, ConditionList);

        public static bool IsFulfilled(ConditionType type, IEnumerable<ICachedCondition> conditions)
        {
            return type switch
            {
                ConditionType.AllHaveToBeTrue => conditions.All(c => c.IsFulfilled()),
                ConditionType.OneHasToBeTrue => conditions.Any(c => c.IsFulfilled()),
                ConditionType.AllHaveToBeFalse => conditions.All(c => !c.IsFulfilled()),
                ConditionType.OneHasToBeFalse => conditions.Any(c => !c.IsFulfilled()),
                _ => false
            };
        }
        public static bool IsFulfilled(PositiveConditionType type, IEnumerable<ICachedCondition> conditions)
        {
            return type switch
            {
                PositiveConditionType.AllHaveToBeTrue => conditions.All(c => c.IsFulfilled()),
                PositiveConditionType.OneHasToBeTrue => conditions.Any(c => c.IsFulfilled()),
                _ => false
            };
        }

        public CachedConditions Clone()
        {
            var clone = new CachedConditions();
            foreach (var cond in ConditionList) clone.ConditionList.Add(cond.Clone());
            clone.SetGameObject(_cachedGameObject);
            return clone;
        }
    }
}
