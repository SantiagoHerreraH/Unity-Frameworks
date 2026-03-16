using MoreMountains.CorgiEngine;
using UnityEngine;
using SilverPillar.Core;

namespace SilverPillar.Integrations.Corgi
{
    public class ChangeWeaponCachedAction : ICachedGameAction
    {
        [SerializeField]
        private Weapon m_Weapon;
        private CharacterHandleWeapon m_HandleWeapon;

        public ChangeWeaponCachedAction(ChangeWeaponCachedAction other)
        {
            m_Weapon = other.m_Weapon;
            m_HandleWeapon = other.m_HandleWeapon;
        }

        public ICachedGameAction Clone()
        {
            return new ChangeWeaponCachedAction(this);
        }

        public void Execute()
        {
            m_HandleWeapon?.ChangeWeapon(m_Weapon, null);
        }

        public GameObject GetGameObject()
        {
            if (m_HandleWeapon)
            {
                return m_HandleWeapon.gameObject;
            }

            return null;
        }

        public bool SetGameObject(GameObject gameObj)
        {   
            if (gameObj)
            {
                var characterHandleWeapon = gameObj?.GetComponent<CharacterHandleWeapon>();
                if (characterHandleWeapon)
                {
                    m_HandleWeapon = characterHandleWeapon;
                    return true;
                }
            }

            return false;
        }
    }
}

