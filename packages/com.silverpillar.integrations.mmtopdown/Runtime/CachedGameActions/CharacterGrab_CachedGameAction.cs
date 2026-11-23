using MoreMountains.TopDownEngine;
using SilverPillar.Core;
using Sirenix.OdinInspector;
using UnityEngine;

namespace SilverPillar.Integrations.MMTopDown
{
    public class CharacterGrab_CachedGameAction : ICachedGameAction
    {
        [SerializeField]
        private WhereToGetComponentFrom m_WhereToGetComponentFrom;
        [SerializeField, ShowIf(nameof(m_WhereToGetComponentFrom), WhereToGetComponentFrom.Custom)]
        private CharacterGrabCarryAndThrow m_CharacterGrab;
        private GameObject m_Self;

        public CharacterGrab_CachedGameAction() { }
        public CharacterGrab_CachedGameAction(CharacterGrab_CachedGameAction other)
        {
            m_WhereToGetComponentFrom = other.m_WhereToGetComponentFrom;
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

            switch (m_WhereToGetComponentFrom)
            {
                case WhereToGetComponentFrom.Self:
                    m_CharacterGrab = gameObj.GetComponent<CharacterGrabCarryAndThrow>();
                    break;
                case WhereToGetComponentFrom.SelfAndChildren:
                    m_CharacterGrab = gameObj.GetComponentInChildren<CharacterGrabCarryAndThrow>(true);
                    break;
                case WhereToGetComponentFrom.Parent:
                    m_CharacterGrab = gameObj.GetComponentInParent<CharacterGrabCarryAndThrow>();
                    break;
                case WhereToGetComponentFrom.Custom:
                    break;
                default:
                    break;
            }

            return m_Self != null && m_CharacterGrab != null;
        }
    }
}
