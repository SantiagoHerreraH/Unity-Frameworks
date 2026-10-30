
using MoreMountains.TopDownEngine;
using SilverPillar.Core;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace SilverPillar.Integrations.MMTopDown
{
    [Serializable]
    public class ChangeRotationMode_CachedGameAction : ICachedGameAction
    {
        [SerializeField]
        private SelfType m_WhichCharacterRotation;

        [SerializeField, ShowIf(nameof(m_WhichCharacterRotation), SelfType.CustomGameObject)]
        private EnhancedCharacterRotation3D m_EnhancedCharacterRotation3D;

        [SerializeField]
        private EnhancedCharacterRotation3D.RotationModes m_RotationMode;

        private GameObject m_Self;

        public ICachedGameAction Clone()
        {
            return new ChangeRotationMode_CachedGameAction
            {
                m_WhichCharacterRotation = m_WhichCharacterRotation,
                m_EnhancedCharacterRotation3D = m_EnhancedCharacterRotation3D,
                m_RotationMode = m_RotationMode
            };
        }

        public void Execute()
        {
            if (m_EnhancedCharacterRotation3D == null)
            {
                Debug.LogError(
                    $"{nameof(ChangeRotationMode_CachedGameAction)}: " +
                    "No EnhancedCharacterRotation3D is cached. " +
                    "Make sure SetGameObject was called successfully."
                );
                return;
            }

            m_EnhancedCharacterRotation3D.ChangeRotationMode(m_RotationMode);
        }

        public GameObject GetGameObject()
        {
            return m_Self;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            m_Self = gameObj;

            if (m_WhichCharacterRotation == SelfType.ThisGameObject)
            {
                m_EnhancedCharacterRotation3D =
                    m_Self.GetComponent<EnhancedCharacterRotation3D>();
            }

            if (m_EnhancedCharacterRotation3D == null)
            {
                Debug.LogError(
                    $"{nameof(ChangeRotationMode_CachedGameAction)}: " +
                    "Could not find an EnhancedCharacterRotation3D component."
                );
                return false;
            }

            return true;
        }
    }
}
