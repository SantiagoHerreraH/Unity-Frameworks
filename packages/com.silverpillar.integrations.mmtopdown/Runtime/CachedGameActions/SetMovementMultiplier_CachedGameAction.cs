using MoreMountains.TopDownEngine;
using SilverPillar.Core;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Integrations.MMTopDown
{
    [Serializable]
    public class SetMovementMultiplier_CachedGameAction : ICachedGameAction
    {
        private enum OperationType
        {
            SetMovementMultiplier,
            ResetMovementMultiplier
        }

        [Title("References")]
        [SerializeField]
        private SelfType m_WhichCharacterMovement;
        [SerializeField, ShowIf(nameof(m_WhichCharacterMovement), SelfType.CustomGameObject)]
        private CharacterMovement m_CharacterMovement;

        [Title("Operation")]
        [SerializeField]
        private OperationType m_OperationType;
        [OdinSerialize, ShowIf(nameof(m_OperationType), OperationType.SetMovementMultiplier)]
        private CachedScoreData m_Multiplier;
        private GameObject m_Self;

        public SetMovementMultiplier_CachedGameAction() { }
        public SetMovementMultiplier_CachedGameAction(SetMovementMultiplier_CachedGameAction other)
        {
            m_WhichCharacterMovement = other.m_WhichCharacterMovement;
            m_CharacterMovement = other.m_CharacterMovement;
            m_OperationType = other.m_OperationType;
            m_Multiplier = other.m_Multiplier.CloneData();
            m_Self = other.m_Self;
        }

        public ICachedGameAction Clone()
        {
            return new SetMovementMultiplier_CachedGameAction(this);
        }

        public void Execute()
        {
            switch (m_OperationType)
            {
                case OperationType.SetMovementMultiplier:
                    m_CharacterMovement.MovementSpeedMultiplier = m_Multiplier.CalculateScore();
                    break;
                case OperationType.ResetMovementMultiplier:
                    m_CharacterMovement.MovementSpeedMultiplier = 1;
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

            m_Multiplier.SetGameObject(gameObj);

            if (m_WhichCharacterMovement == SelfType.ThisGameObject)
            {
                m_CharacterMovement = gameObj.GetComponent<CharacterMovement>();
            }

            return m_Self != null && m_CharacterMovement != null;
        }
    }
}
