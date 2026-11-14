using MoreMountains.TopDownEngine;
using SilverPillar.Core;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace SilverPillar.Integrations.MMTopDown
{
    [Serializable]
    public class ChangeCharacterState_CachedGameAction : ICachedGameAction
    {
        public enum CharacterStateType
        {
            CharacterConditions,
            MovementState
        }

        [SerializeField] private SelfType m_WhichCharacter;
        [SerializeField, ShowIf(nameof(m_WhichCharacter), SelfType.CustomGameObject)] private Character m_Character;
        [SerializeField] private CharacterStateType m_CharacterStateType;

        [SerializeField, ShowIf(nameof(m_CharacterStateType), CharacterStateType.CharacterConditions)]
        private CharacterStates.CharacterConditions m_CharacterConditions;

        [SerializeField, ShowIf(nameof(m_CharacterStateType), CharacterStateType.MovementState)]
        private CharacterStates.MovementStates m_MovementStates;

        private GameObject m_Self;

        public ICachedGameAction Clone()
        {
            return new ChangeCharacterState_CachedGameAction
            {
                m_WhichCharacter = m_WhichCharacter,
                m_Character = m_Character,
                m_CharacterStateType = m_CharacterStateType,
                m_CharacterConditions = m_CharacterConditions,
                m_MovementStates = m_MovementStates,
                m_Self = m_Self
            };
        }

        public void Execute()
        {
            if (m_Character == null)
            {
                Debug.LogError($"[{nameof(ChangeCharacterState_CachedGameAction)}] Target Character is null.");
                return;
            }

            switch (m_CharacterStateType)
            {
                case CharacterStateType.CharacterConditions:
                    m_Character.ConditionState?.ChangeState(m_CharacterConditions);
                    break;
                case CharacterStateType.MovementState:
                    m_Character.MovementState?.ChangeState(m_MovementStates);
                    break;
            }
        }

        public GameObject GetGameObject()
        {
            return m_Self;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            if (gameObj == null)
            {
                Debug.LogError($"Trying to set null gameobject in {nameof(ChangeCharacterState_CachedGameAction)}");
                return false;
            }
            m_Self = gameObj;

            if (m_WhichCharacter == SelfType.ThisGameObject)
            {
                m_Self.TryGetComponent(out m_Character);
            }

            return m_Character != null;
        }
    }
}
