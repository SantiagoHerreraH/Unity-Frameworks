using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class NameIs_CachedCondition : ICachedCondition
    {
        [OdinSerialize, ShowInInspector]
        private IString m_Name;

        [SerializeField]
        private SelfType m_WhoseName;

        [SerializeField, ShowIf(nameof(m_WhoseName), SelfType.CustomGameObject)]
        private GameObject m_CustomGameObject;

        [SerializeField]
        private SelfType m_WhatGameObjectToReturnOnGetGameObject;

        private GameObject m_SelfGameObject;

        public ICachedCondition Clone()
        {
            return new NameIs_CachedCondition
            {
                m_Name = m_Name.Clone(),
                m_WhoseName = m_WhoseName,
                m_CustomGameObject = m_CustomGameObject,
                m_SelfGameObject = m_SelfGameObject
            };
        }

        public GameObject GetGameObject()
        {
            switch (m_WhatGameObjectToReturnOnGetGameObject)
            {
                case SelfType.ThisGameObject:
                    return m_SelfGameObject;

                case SelfType.CustomGameObject:
                    return m_CustomGameObject;

                default:
                    return m_SelfGameObject;
            }
        }

        public bool IsFulfilled()
        {
            GameObject target = GetGameObject();

            if (target == null)
                return false;

            return target.name == m_Name.CalculateString();
        }

        public bool SetGameObject(GameObject gameObj)
        {
            m_SelfGameObject = gameObj;
            
            return m_Name.SetGameObject(gameObj) && GetGameObject() != null;
        }
    }
}