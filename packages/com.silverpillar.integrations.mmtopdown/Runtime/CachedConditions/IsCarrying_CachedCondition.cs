using SilverPillar.Core;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace SilverPillar.Integrations.MMTopDown
{
    [Serializable]
    public class IsCarrying_CachedCondition : ICachedCondition
    {
        [SerializeField]
        private WhereToGetComponentFrom m_WhichCharacterGrabbing;

        [SerializeField, ShowIf(nameof(m_WhichCharacterGrabbing), SelfType.CustomGameObject)]
        private CharacterGrabCarryAndThrow m_CharacterGrabCarryAndThrow;

        private GameObject m_Self;

        public ICachedCondition Clone()
        {
            return new IsCarrying_CachedCondition
            {
                m_WhichCharacterGrabbing = this.m_WhichCharacterGrabbing,
                m_CharacterGrabCarryAndThrow = this.m_CharacterGrabCarryAndThrow
            };
        }

        public GameObject GetGameObject()
        {
            return m_Self;
        }

        public bool IsFulfilled()
        {
            return m_CharacterGrabCarryAndThrow != null && m_CharacterGrabCarryAndThrow.IsCarrying();
        }

        public bool SetGameObject(GameObject gameObj)
        {
            m_Self = gameObj;

            switch (m_WhichCharacterGrabbing)
            {
                case WhereToGetComponentFrom.Self:
                    gameObj.TryGetComponent(out m_CharacterGrabCarryAndThrow);
                    break;
                case WhereToGetComponentFrom.SelfAndChildren:

                    m_CharacterGrabCarryAndThrow = gameObj.GetComponentInChildren<CharacterGrabCarryAndThrow>();
                    break;
                case WhereToGetComponentFrom.Parent:
                    m_CharacterGrabCarryAndThrow = gameObj.GetComponentInParent<CharacterGrabCarryAndThrow>();
                    break;
                default:
                    break;
            }

            return m_Self != null && m_CharacterGrabCarryAndThrow != null;
        }
    }
}
