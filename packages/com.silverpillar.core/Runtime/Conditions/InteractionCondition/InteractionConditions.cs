using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class InteractionConditionGroup : IInteractionCondition
    {
        public ConditionType ConditionType;
        public InteractionConditions Conditions = new();

        public bool SetGameObject(GameObject self) => Conditions.SetGameObject(self);
        public GameObject? GetGameObject() => Conditions.GetGameObject();
        public bool IsFulfilled(GameObject target) => Conditions.IsFulfilled(ConditionType, target);

        public IInteractionCondition Clone() => new InteractionConditionGroup
        {
            ConditionType = this.ConditionType,
            Conditions = this.Conditions.Clone()
        };

        public void Add(InteractionConditionGroup other)
        {
            other.Conditions.Add(other.Conditions);
        }
    }

    [Serializable]
    public class InteractionPositiveConditionGroup : IInteractionCondition
    {
        public PositiveConditionType ConditionType;
        public InteractionConditions Conditions = new();

        public bool SetGameObject(GameObject self) => Conditions.SetGameObject(self);
        public GameObject? GetGameObject() => Conditions.GetGameObject();
        public bool IsFulfilled(GameObject target) => Conditions.IsFulfilled(ConditionType, target);

        public IInteractionCondition Clone() => new InteractionPositiveConditionGroup
        {
            ConditionType = this.ConditionType,
            Conditions = this.Conditions.Clone()
        };

        public void Add(InteractionPositiveConditionGroup other)
        {
            other.Conditions.Add(other.Conditions);
        }
    }

    [Serializable]
    public class InteractionConditions
    {
        [OdinSerialize, ShowInInspector]
        public List<IInteractionCondition> ConditionList = new();

        private GameObject? _cachedSelf;

        public bool SetGameObject(GameObject self)
        {
            _cachedSelf = self;
            bool allSucceeded = true;
            foreach (var condition in ConditionList)
            {
                if (!condition.SetGameObject(self)) allSucceeded = false;
            }
            return allSucceeded;
        }

        public GameObject? GetGameObject() => _cachedSelf;

        public bool IsFulfilled(ConditionType type, GameObject target) => IsFulfilled(type, ConditionList, target);
        public bool IsFulfilled(PositiveConditionType type, GameObject target) => IsFulfilled(type, ConditionList, target);

        public static bool IsFulfilled(ConditionType type, IEnumerable<IInteractionCondition> conditions, GameObject target)
        {
            if (conditions.Count() == 0)
            {
                return true;
            }
            return type switch
            {
                ConditionType.AllHaveToBeTrue => conditions.All(c => c.IsFulfilled(target)),
                ConditionType.OneHasToBeTrue => conditions.Any(c => c.IsFulfilled(target)),
                ConditionType.AllHaveToBeFalse => conditions.All(c => !c.IsFulfilled(target)),
                ConditionType.OneHasToBeFalse => conditions.Any(c => !c.IsFulfilled(target)),
                _ => false
            };
        }

        public static bool IsFulfilled(PositiveConditionType type, IEnumerable<IInteractionCondition> conditions, GameObject target)
        {
            if (conditions.Count() == 0)
            {
                return true;
            }
            return type switch
            {
                PositiveConditionType.AllHaveToBeTrue => conditions.All(c => c.IsFulfilled(target)),
                PositiveConditionType.OneHasToBeTrue => conditions.Any(c => c.IsFulfilled(target)),
                _ => false
            };
        }

        public InteractionConditions Clone()
        {
            var clone = new InteractionConditions();
            foreach (var cond in ConditionList) clone.ConditionList.Add(cond.Clone());
            if (_cachedSelf != null) clone.SetGameObject(_cachedSelf);
            return clone;
        }

        public void Add(InteractionConditions other)
        {
            foreach (var item in other.ConditionList)
            {
                ConditionList.Add(item.Clone());
            }
        }
    }
}
