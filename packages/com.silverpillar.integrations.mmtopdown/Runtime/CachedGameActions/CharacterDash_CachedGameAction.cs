using MoreMountains.TopDownEngine;
using SilverPillar.Core;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace SilverPillar.Integrations.MMTopDown
{
    public enum DashDirectionOperation
    {
        DontSet,
        ResetToForwardVector,
        SetDirection
    }
    public enum DashOperation
    {
        DashStart,
        DashStop
    }

    [Serializable]
    public struct DashStartDirectionParams
    {
        public DashDirectionOperation DashDirectionOperation;

        [ShowIf(nameof(DashDirectionOperation), DashDirectionOperation.SetDirection)]
        public Vector3 DashDirection;
    }

    [Serializable]
    public class CharacterDash_CachedGameAction : ICachedGameAction
    {

        [Title("References")]
        [SerializeField]
        private SelfType m_WhichCharacterDash;

        [SerializeField, ShowIf(nameof(m_WhichCharacterDash), SelfType.CustomGameObject)]
        private CharacterDash3D m_CharacterDash;


        [Title("Dash")]
        [SerializeField]
        private DashOperation m_DashOperation;

        [SerializeField, ShowIf(nameof(m_DashOperation), DashOperation.DashStart), Tooltip("Will reset direction after dash.")]
        private DashStartDirectionParams m_DashParameters;

        private GameObject m_Self;
        private  Vector3 m_PreviousDashDirection;
        public CharacterDash_CachedGameAction() { }

        public CharacterDash_CachedGameAction(CharacterDash_CachedGameAction other)
        {
            m_WhichCharacterDash = other.m_WhichCharacterDash;
            m_CharacterDash = other.m_CharacterDash;
            m_DashOperation = other.m_DashOperation;
            m_Self = other.m_Self;
            m_DashParameters = other.m_DashParameters;
        }

        public ICachedGameAction Clone()
        {
            return new CharacterDash_CachedGameAction(this);
        }

        public void Execute()
        {
            if (m_CharacterDash == null) return;

            if (m_DashOperation == DashOperation.DashStart)
            {
                SetDirectionBefore();

                m_CharacterDash.DashStart();

                SetDirectionAfter();
            }
            else if (m_DashOperation == DashOperation.DashStop)
            {
                m_CharacterDash.DashStop();
            }
        }

        private void SetDirectionBefore()
        {
            m_PreviousDashDirection = m_CharacterDash.DashDirection;
            switch (m_DashParameters.DashDirectionOperation)
            {
                case DashDirectionOperation.DontSet:
                    break;
                case DashDirectionOperation.ResetToForwardVector:
                    m_CharacterDash.DashDirection = Vector3.forward;
                    break;
                case DashDirectionOperation.SetDirection:
                    m_CharacterDash.DashDirection = m_DashParameters.DashDirection;
                    break;
            }
        }

        private void SetDirectionAfter()
        {
            m_CharacterDash.DashDirection = m_PreviousDashDirection;
        }

        public GameObject GetGameObject()
        {
            return m_Self;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            m_Self = gameObj;

            if (m_WhichCharacterDash == SelfType.ThisGameObject && gameObj != null)
            {
                m_CharacterDash = gameObj.GetComponent<CharacterDash3D>();
            }

            return m_Self != null && m_CharacterDash != null;
        }
    }
}
