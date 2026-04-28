using SilverPillar.Core;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Animation
{
    [Serializable]
    public class SetAnimatorSpeed_CachedGameAction : ICachedGameAction
    {
        [OdinSerialize, ShowInInspector]
        private ICachedScore m_Speed;

        [SerializeField]
        private SelfType m_SelfTypeForSpeedCalculation;
        [SerializeField, ShowIf(nameof(m_SelfTypeForSpeedCalculation), SelfType.CustomGameObject)]
        private GameObject m_SpeedGameObject;

        [SerializeField]
        private SelfType m_SelfTypeForAnimator;
        [SerializeField, ShowIf(nameof(m_SelfTypeForAnimator), SelfType.CustomGameObject)]
        private Animator m_Animator;

        public SetAnimatorSpeed_CachedGameAction() { }
        public SetAnimatorSpeed_CachedGameAction(SetAnimatorSpeed_CachedGameAction other)
        {
            m_Speed = other.m_Speed?.Clone();

            m_SelfTypeForSpeedCalculation = other.m_SelfTypeForSpeedCalculation;
            m_SpeedGameObject = other.m_SpeedGameObject;

            m_SelfTypeForAnimator = other.m_SelfTypeForAnimator;
            m_Animator = other.m_Animator;
        }

        public ICachedGameAction Clone()
        {
            return new SetAnimatorSpeed_CachedGameAction(this);
        }

        public void Execute()
        {
            if (m_Animator != null)
            {
                m_Animator.speed = m_Speed.CalculateScore();
            }
        }

#nullable enable

        public GameObject? GetGameObject()
        {
            return m_Animator ? m_Animator.gameObject : null;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            bool speedSuccess = false;
            switch (m_SelfTypeForSpeedCalculation)
            {
                case SelfType.ThisGameObject:
                    speedSuccess = m_Speed.SetGameObject(gameObj);
                    break;
                case SelfType.CustomGameObject:
                    speedSuccess = m_Speed.SetGameObject(m_SpeedGameObject);
                    break;
                default:
                    break;
            }

            if (m_SelfTypeForAnimator == SelfType.ThisGameObject)
            {
                gameObj.TryGetComponent(out m_Animator);
            }
            return m_Animator != null && speedSuccess;
        }
    }
}
