using SilverPillar.Core;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace SilverPillar.Integrations.MMTopDown
{
    [Serializable]
    public class CharacterHandleWeapon_CachedGameAction : ICachedGameAction
    {
        [SerializeField]
        private SelfType m_WhichCharacterHandleSecondaryWeapon;
        [SerializeField, ShowIf(nameof(m_WhichCharacterHandleSecondaryWeapon), SelfType.CustomGameObject)]
        private EnhancedCharacterHandleWeapon m_EnhancedCharacterHandleWeapon;
        private GameObject m_Self;
        [SerializeField]
        private ShootOperationType m_OperationType;

        public CharacterHandleWeapon_CachedGameAction() { }
        public CharacterHandleWeapon_CachedGameAction(CharacterHandleWeapon_CachedGameAction other)
        {
            m_WhichCharacterHandleSecondaryWeapon = other.m_WhichCharacterHandleSecondaryWeapon;
            m_EnhancedCharacterHandleWeapon = other.m_EnhancedCharacterHandleWeapon;
            m_Self = other.m_Self;
            m_OperationType = other.m_OperationType;
        }

        public ICachedGameAction Clone()
        {
            return new CharacterHandleWeapon_CachedGameAction(this);
        }

        public void Execute()
        {
            switch (m_OperationType)
            {
                case ShootOperationType.ShootStart:
                    m_EnhancedCharacterHandleWeapon.ShootStart();
                    break;
                case ShootOperationType.ShootStop:
                    m_EnhancedCharacterHandleWeapon.ShootStop();
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
                gameObj.TryGetComponent(out m_EnhancedCharacterHandleWeapon);
            }

            return m_Self != null && m_EnhancedCharacterHandleWeapon != null;
        }
    }
}
