using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using SilverPillar.Core;
using System;
using UnityEngine;

namespace SilverPillar.Integrations.MMTopDown
{
    [Serializable]
    public class GameInputButton_CachedCondition : ICachedCondition
    {
        public enum Buttons
        {
            SuperButton,
            ThrowButton,
            GrabButton,
            JumpButton,
            RunButton,
            DashButton,
            CrouchButton,
            ShootButton,
            InteractButton,
            SecondaryShootButton,
            ReloadButton,
            PauseButton,
            TimeControlButton,
            SwitchCharacterButton,
            SwitchWeaponButton
        }

        [SerializeField]
        private Buttons m_Button;
        [SerializeField]
        private MMInput.ButtonStates m_ButtonState;

        private GameObject m_Self;
        private GameInputManager m_GameInputManager;

        public GameInputButton_CachedCondition()
        {

        }

        public GameInputButton_CachedCondition(GameInputButton_CachedCondition other)
        {
            m_Button = other.m_Button;
            m_ButtonState = other.m_ButtonState;
            m_Self = other.m_Self;
            m_GameInputManager = other.m_GameInputManager;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            m_Self = gameObj;
            m_GameInputManager = InputManager.Instance as GameInputManager;
            return m_Self != null && m_GameInputManager != null;
        }

        public GameObject GetGameObject()
        {
            return m_Self;
        }

        public bool IsFulfilled()
        {
            if (m_GameInputManager != null)
            {
                switch (m_Button)
                {
                    case Buttons.SuperButton:

                        return m_GameInputManager.SuperButton.State.CurrentState == m_ButtonState;

                    case Buttons.ThrowButton:

                        return m_GameInputManager.ThrowButton.State.CurrentState == m_ButtonState;

                    case Buttons.GrabButton:

                        return m_GameInputManager.GrabButton.State.CurrentState == m_ButtonState;

                    case Buttons.JumpButton:

                        return m_GameInputManager.JumpButton.State.CurrentState == m_ButtonState;

                    case Buttons.RunButton:

                        return m_GameInputManager.RunButton.State.CurrentState == m_ButtonState;

                    case Buttons.DashButton:

                        return m_GameInputManager.DashButton.State.CurrentState == m_ButtonState;

                    case Buttons.CrouchButton:

                        return m_GameInputManager.CrouchButton.State.CurrentState == m_ButtonState;

                    case Buttons.ShootButton:

                        return m_GameInputManager.ShootButton.State.CurrentState == m_ButtonState;

                    case Buttons.InteractButton:

                        return m_GameInputManager.InteractButton.State.CurrentState == m_ButtonState;

                    case Buttons.SecondaryShootButton:

                        return m_GameInputManager.SecondaryShootButton.State.CurrentState == m_ButtonState;

                    case Buttons.ReloadButton:

                        return m_GameInputManager.ReloadButton.State.CurrentState == m_ButtonState;

                    case Buttons.PauseButton:

                        return m_GameInputManager.PauseButton.State.CurrentState == m_ButtonState;

                    case Buttons.TimeControlButton:

                        return m_GameInputManager.TimeControlButton.State.CurrentState == m_ButtonState;

                    case Buttons.SwitchCharacterButton:

                        return m_GameInputManager.SwitchCharacterButton.State.CurrentState == m_ButtonState;

                    case Buttons.SwitchWeaponButton:

                        return m_GameInputManager.SwitchWeaponButton.State.CurrentState == m_ButtonState;

                    default:
                        break;
                }
            }

            return false;
        }

        public ICachedCondition Clone()
        {
            return new GameInputButton_CachedCondition(this);
        }

    }
}
