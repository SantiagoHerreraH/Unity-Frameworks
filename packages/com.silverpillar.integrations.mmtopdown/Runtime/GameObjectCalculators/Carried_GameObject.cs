using SilverPillar.Core;
using Sirenix.OdinInspector;
using UnityEngine;

namespace SilverPillar.Integrations.MMTopDown
{
    public class Carried_GameObject : IGameObjectCalculator
    {

        [SerializeField]
        private WhereToGetComponentFrom m_WhichCharacterGrabbing;

        [SerializeField, ShowIf(nameof(m_WhichCharacterGrabbing), WhereToGetComponentFrom.Custom)]
        private CharacterGrabCarryAndThrow m_CharacterGrabCarryAndThrow;

        private GameObject m_Self;

        public IGameObjectCalculator Clone()
        {
            return new Carried_GameObject
            {
                m_WhichCharacterGrabbing = this.m_WhichCharacterGrabbing,
                m_CharacterGrabCarryAndThrow = this.m_CharacterGrabCarryAndThrow
            };
        }

        public GameObject GetGameObject()
        {
            return m_Self;
        }

        public GameObject CalculateGameObject()
        {
            if (m_CharacterGrabCarryAndThrow == null)
            {
                return null;
            }
            return m_CharacterGrabCarryAndThrow.Carried;
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
