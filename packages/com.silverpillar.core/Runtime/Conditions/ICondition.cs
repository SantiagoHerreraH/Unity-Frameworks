using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Core
{
    public interface ICondition
    {
        public bool IsFulfilled(GameObject gameObj);
    }

    public interface ICachedCondition
    {
        public bool SetGameObject(GameObject gameObj);
        public GameObject GetGameObject();
        public bool IsFulfilled();

        public ICachedCondition Clone();
    }

    public abstract class SaveableCondition : SaveableScriptableObject, ICondition
    {
        public abstract bool IsFulfilled(GameObject gameObj);
    }

    [CreateAssetMenu(fileName = "SaveableScriptableObject", menuName = "Conditions/CachedCondition")]
    public class CachedCondition : SaveableScriptableObject
    {
        [OdinSerialize]
        private ICachedCondition m_CachedCondition;

        public ICachedCondition Clone(GameObject gameObj)
        {
            var result = m_CachedCondition.Clone();
            result.SetGameObject(gameObj);
            return result;
        }
    }

    [Serializable]
    public class CachedConditionGroup
    {

        [SerializeField]
        private List<SO_Ref<CachedCondition>> m_ConditionsToExecute = new();
        private List<ICachedCondition> m_ConditionsToExecute_Instances = new();

        private GameObject m_GameObject = null;

        public void SetGameObject(GameObject gameObject)
        {
            m_GameObject = gameObject;
            if (m_ConditionsToExecute_Instances.Count == 0)
            {
                foreach (var item in m_ConditionsToExecute)
                {
                    m_ConditionsToExecute_Instances.Add(item.Get().Clone(gameObject));
                }
            }
            else
            {
                foreach (var item in m_ConditionsToExecute_Instances)
                {
                    item.SetGameObject(gameObject);
                }
            }

            
        }

        public bool IsFulfilled()
        {
            foreach (var item in m_ConditionsToExecute_Instances)
            {
                if (!item.IsFulfilled())
                {
                    return false;
                }
            }

            return true;
        }
        public void AddCondition(SO_Ref<CachedCondition> cachedCondition)
        {
            if (!m_ConditionsToExecute.Contains(cachedCondition))
            {
                m_ConditionsToExecute.Add(cachedCondition);
                m_ConditionsToExecute_Instances.Add(cachedCondition.Get().Clone(m_GameObject));
            }
        }

        public void RemoveCondition(SO_Ref<CachedCondition> cachedCondition)
        {
            int index = m_ConditionsToExecute.IndexOf(cachedCondition);
            if (index >= 0)
            {
                m_ConditionsToExecute.RemoveAt(index);
                m_ConditionsToExecute_Instances.RemoveAt(index);
            }
        }
    }

    public enum ConditionType
    {
        AllHaveToBeTrue,
        OneHasToBeTrue,
        AllHaveToBeFalse,
        OneHasToBeFalse
    }

    [CreateAssetMenu(fileName = "SaveableScriptableObject", menuName = "Conditions/ConditionGroup")]
    public class ConditionGroup : SaveableScriptableObject, ICondition
    {
        public ConditionType ConditionType;

        [OdinSerialize]
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
    }
}

