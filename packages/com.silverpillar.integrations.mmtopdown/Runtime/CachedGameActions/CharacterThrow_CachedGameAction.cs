using SilverPillar.Core;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace SilverPillar.Integrations.MMTopDown
{
    [Serializable]
    public class CharacterThrow_CachedGameAction : ICachedGameAction
    {
        [SerializeField]
        private WhereToGetComponentFrom m_WhichCharacterGrabCarryAndThrow;
        [SerializeField, ShowIf(nameof(m_WhichCharacterGrabCarryAndThrow), WhereToGetComponentFrom.Custom)]
        private CharacterGrabCarryAndThrow m_CharacterGrabCarryAndThrow;
        private GameObject m_Self;

        public CharacterThrow_CachedGameAction() { }
        public CharacterThrow_CachedGameAction(CharacterThrow_CachedGameAction other)
        {
            m_WhichCharacterGrabCarryAndThrow = other.m_WhichCharacterGrabCarryAndThrow;
            m_CharacterGrabCarryAndThrow = other.m_CharacterGrabCarryAndThrow;
            m_Self = other.m_Self;
        }

        public ICachedGameAction Clone()
        {
            return new CharacterThrow_CachedGameAction(this);
        }

        public void Execute()
        {
            m_CharacterGrabCarryAndThrow.TryThrow();
        }

        public GameObject GetGameObject()
        {
            return m_Self;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            m_Self = gameObj;

            switch (m_WhichCharacterGrabCarryAndThrow)
            {
                case WhereToGetComponentFrom.Self:
                    m_CharacterGrabCarryAndThrow = gameObj.GetComponent<CharacterGrabCarryAndThrow>();
                    break;
                case WhereToGetComponentFrom.SelfAndChildren:
                    m_CharacterGrabCarryAndThrow = gameObj.GetComponentInChildren<CharacterGrabCarryAndThrow>(true);
                    break;
                case WhereToGetComponentFrom.Parent:
                    m_CharacterGrabCarryAndThrow = gameObj.GetComponentInParent<CharacterGrabCarryAndThrow>(true);
                    break;
                case WhereToGetComponentFrom.Custom:
                    break;
                default:
                    break;
            }

            return m_Self != null && m_CharacterGrabCarryAndThrow != null;
        }
    }
}
