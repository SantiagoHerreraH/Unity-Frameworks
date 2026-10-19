using MoreMountains.TopDownEngine;
using SilverPillar.Core;
using Sirenix.OdinInspector;
using UnityEngine;

namespace SilverPillar.Integrations.MMTopDown
{
    public class CharacterGrab_CachedGameAction : ICachedGameAction
    {
        [SerializeField]
        private SelfType m_WhichCharacterGrab;
        [SerializeField, ShowIf(nameof(m_WhichCharacterGrab), SelfType.CustomGameObject)]
        private CharacterGrabCarryAndThrow m_CharacterGrab;
        private GameObject m_Self;

        public CharacterGrab_CachedGameAction() { }
        public CharacterGrab_CachedGameAction(CharacterGrab_CachedGameAction other)
        {
            m_WhichCharacterGrab = other.m_WhichCharacterGrab;
            m_CharacterGrab = other.m_CharacterGrab;
            m_Self = other.m_Self;
        }

        public ICachedGameAction Clone()
        {
            return new CharacterGrab_CachedGameAction(this);
        }

        public void Execute()
        {
            m_CharacterGrab.TryGrab();
        }

        public GameObject GetGameObject()
        {
            return m_Self;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            m_Self = gameObj;

            if (m_WhichCharacterGrab == SelfType.ThisGameObject)
            {
                m_CharacterGrab = gameObj.GetComponent<CharacterGrabCarryAndThrow>();
            }

            return m_Self != null && m_CharacterGrab != null;
        }
    }
}
