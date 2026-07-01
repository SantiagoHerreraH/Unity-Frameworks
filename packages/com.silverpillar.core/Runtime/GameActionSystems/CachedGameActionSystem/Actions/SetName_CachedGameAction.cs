using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class SetName_CachedGameAction : ICachedGameAction
    {
        [OdinSerialize, ShowInInspector]
        private IString m_Name;

        [SerializeField]
        private SelfType m_SetNameOfWho;

        [SerializeField, ShowIf(nameof(m_SetNameOfWho), SelfType.CustomGameObject)]
        private GameObject m_CustomGameObject;

        private GameObject m_Self;

        public SetName_CachedGameAction()
        {

        }

        public SetName_CachedGameAction(SetName_CachedGameAction other)
        {
            m_Name = other.m_Name.Clone();
            m_SetNameOfWho = other.m_SetNameOfWho;
            m_CustomGameObject = other.m_CustomGameObject;
            m_Self = other.m_Self;
        }
        public ICachedGameAction Clone()
        {
            return new SetName_CachedGameAction(this);
        }

        public void Execute()
        {
            var chosen = GetChosen();

            if (chosen != null)
            {
                chosen.name = m_Name.CalculateString();
            }
        }

        public GameObject GetGameObject()
        {
            return m_Self;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            m_Self = gameObj;
            return m_Self != null;
        }

        private GameObject GetChosen()
        {
            switch (m_SetNameOfWho)
            {
                case SelfType.ThisGameObject:
                    return m_Self;
                case SelfType.CustomGameObject:
                    return m_CustomGameObject;
                default:
                    break;
            }

            return m_Self;
        }
    }
}
