using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using SilverPillar.Core;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace SilverPillar.Integrations.MMTopDown
{
    [Serializable]
    public class SetCharacterMovement_CachedGameAction : ICachedGameAction
    {
        [SerializeField]
        private SelfType m_WhichCharacterMovement;

        [SerializeField, ShowIf(nameof(m_WhichCharacterMovement), SelfType.CustomGameObject)]
        private CharacterMovement m_CharacterMovement;

        [SerializeField]
        private Vector2 m_Movement = Vector2.zero;

        private GameObject m_Self;
        private CharacterMovement m_CachedCharacterMovement;

        public ICachedGameAction Clone()
        {
            return new SetCharacterMovement_CachedGameAction
            {
                m_WhichCharacterMovement = m_WhichCharacterMovement,
                m_CharacterMovement = m_CharacterMovement,
                m_Movement = m_Movement
            };
        }

        public void Execute()
        {
            if (m_CachedCharacterMovement == null)
            {
                Debug.LogError(
                    $"{nameof(SetMovement_CachedGameAction)}: " +
                    "No TopDownController is cached. " +
                    "Make sure SetGameObject was called successfully."
                );
                return;
            }

            m_CachedCharacterMovement.SetMovement(m_Movement);
        }

        public GameObject GetGameObject()
        {
            return m_Self;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            m_Self = gameObj;
            m_CachedCharacterMovement = null;

            if (m_WhichCharacterMovement == SelfType.CustomGameObject)
            {
                m_CachedCharacterMovement = m_CharacterMovement;
            }
            else if (m_Self != null)
            {
                m_Self.TryGetComponent(
                    out m_CachedCharacterMovement
                );
            }

            if (m_CachedCharacterMovement == null)
            {
                Debug.LogError(
                    $"{nameof(SetMovement_CachedGameAction)}: " +
                    "Could not find a TopDownController component."
                );
                return false;
            }

            return true;
        }
    }
}
