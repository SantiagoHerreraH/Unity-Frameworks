using SilverPillar.Core;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;
using UnityEngine.Video;

namespace SilverPillar.Animation
{
    [Serializable]
    public class SetBoolAnimationVariable_CachedGameAction : ICachedGameAction
    {

        [Title("Data")]
        [OdinSerialize, ShowInInspector]
        private IString m_VariableName;

        [OdinSerialize, ShowInInspector]
        private ICachedCondition m_CachedCondition;

        [SerializeField]
        private WhereToGetAnimatorFrom m_WhereToGetAnimatorFrom;

        [SerializeField, ShowIf(nameof(m_WhereToGetAnimatorFrom), WhereToGetAnimatorFrom.FromCustom)]
        private Animator m_Animator;

        [Title("Debug")]
        [SerializeField]
        private bool m_PrintValue;


        public SetBoolAnimationVariable_CachedGameAction() { }
        public SetBoolAnimationVariable_CachedGameAction(SetBoolAnimationVariable_CachedGameAction other)
        {
            m_VariableName = other.m_VariableName.Clone();
            m_CachedCondition = other.m_CachedCondition.Clone();
            m_WhereToGetAnimatorFrom = other.m_WhereToGetAnimatorFrom;
            m_Animator = other.m_Animator;
        }

        public ICachedGameAction Clone()
        {
            return new SetBoolAnimationVariable_CachedGameAction(this);
        }

        public void Execute()
        {
            if (m_Animator)
            {
                var varName = m_VariableName.CalculateString();
                var value = m_CachedCondition.IsFulfilled();
                m_Animator.SetBool(varName, value);

                if (m_PrintValue)
                {
                    Debug.Log($"[AnimationAction] Setting {varName} to {value} on {m_Animator.gameObject.name}", m_Animator);
                }
            }
        }

        public GameObject GetGameObject()
        {
            return m_VariableName.GetGameObject();
        }

        public bool SetGameObject(GameObject gameObj)
        {
            bool animatorIsGood = true;

            if (m_WhereToGetAnimatorFrom == WhereToGetAnimatorFrom.FromThisGameObject)
            {
                animatorIsGood =  gameObj.TryGetComponent(out m_Animator);
            }

            return animatorIsGood &&
                m_VariableName.SetGameObject(gameObj) &&
                m_CachedCondition.SetGameObject(gameObj);
        }
    }
}
