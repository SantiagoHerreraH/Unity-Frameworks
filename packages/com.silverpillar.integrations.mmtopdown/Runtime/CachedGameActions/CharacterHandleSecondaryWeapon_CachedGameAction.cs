using SilverPillar.Core;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace SilverPillar.Integrations.MMTopDown
{
    public enum ShootOperationType
    {
        ShootStart,
        ShootStop
    }
    [Serializable]
    public class CharacterHandleSecondaryWeapon_CachedGameAction : ICachedGameAction
    {
        [SerializeField]
        private SelfType m_WhichCharacterHandleSecondaryWeapon;
        [SerializeField, ShowIf(nameof(m_WhichCharacterHandleSecondaryWeapon), SelfType.CustomGameObject)]
        private EnhancedCharacterHandleSecondaryWeapon m_EnhancedCharacterHandleSecondaryWeapon;
        private GameObject m_Self;
        [SerializeField]
        private ShootOperationType m_OperationType;

        public CharacterHandleSecondaryWeapon_CachedGameAction() { }
        public CharacterHandleSecondaryWeapon_CachedGameAction(CharacterHandleSecondaryWeapon_CachedGameAction other)
        {
            m_WhichCharacterHandleSecondaryWeapon = other.m_WhichCharacterHandleSecondaryWeapon;
            m_EnhancedCharacterHandleSecondaryWeapon = other.m_EnhancedCharacterHandleSecondaryWeapon;
            m_Self = other.m_Self;
            m_OperationType = other.m_OperationType;
        }

        public ICachedGameAction Clone()
        {
            return new CharacterHandleSecondaryWeapon_CachedGameAction(this);
        }

        public void Execute()
        {
            switch (m_OperationType)
            {
                case ShootOperationType.ShootStart:
                    m_EnhancedCharacterHandleSecondaryWeapon.ShootStart();
                    break;
                case ShootOperationType.ShootStop:
                    m_EnhancedCharacterHandleSecondaryWeapon.ShootStop();
                    break;
                default:
                    break;
            }
        }

        public GameObject GetGameObject()
        {
            return m_Self;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            m_Self = gameObj;

            if (m_WhichCharacterHandleSecondaryWeapon == SelfType.ThisGameObject)
            {
                gameObj.TryGetComponent(out m_EnhancedCharacterHandleSecondaryWeapon);
            }

            return m_Self != null && m_EnhancedCharacterHandleSecondaryWeapon != null;
        }
    }
}
