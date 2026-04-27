using SilverPillar.Core;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Animation
{
    [Serializable]
    public class SetTriggerAnimationVariable_CachedGameAction : ICachedGameAction
    {
        public enum TriggerActionType
        {
            SetTrigger,
            ResetTrigger
        }

        [SerializeField]
        private TriggerActionType m_TriggerActionType;

        [OdinSerialize, ShowInInspector]
        private IString m_VariableName;

        [SerializeField]
        private WhereToGetAnimatorFrom m_WhereToGetAnimatorFrom;

        [SerializeField, ShowIf(nameof(m_WhereToGetAnimatorFrom), WhereToGetAnimatorFrom.FromCustom)]
        private Animator m_Animator;

        public SetTriggerAnimationVariable_CachedGameAction() { }
        public SetTriggerAnimationVariable_CachedGameAction(SetTriggerAnimationVariable_CachedGameAction other)
        {
            m_VariableName = other.m_VariableName.Clone();
            m_WhereToGetAnimatorFrom = other.m_WhereToGetAnimatorFrom;
            m_Animator = other.m_Animator;
            m_TriggerActionType = other.m_TriggerActionType;
        }

        public ICachedGameAction Clone() => new SetTriggerAnimationVariable_CachedGameAction(this);

        public void Execute()
        {
            if (m_Animator)
            {
                switch (m_TriggerActionType)
                {
                    case TriggerActionType.SetTrigger:
                        m_Animator.SetTrigger(m_VariableName.CalculateString());
                        break;
                    case TriggerActionType.ResetTrigger:
                        m_Animator.ResetTrigger(m_VariableName.CalculateString());
                        break;
                    default:
                        break;
                }
            }
        }

        public GameObject GetGameObject() => m_VariableName.GetGameObject();

        public bool SetGameObject(GameObject gameObj)
        {
            bool animatorIsGood = true;
            if (m_WhereToGetAnimatorFrom == WhereToGetAnimatorFrom.FromThisGameObject)
                animatorIsGood = gameObj.TryGetComponent(out m_Animator);

            return animatorIsGood && m_VariableName.SetGameObject(gameObj);
        }
    }
}
