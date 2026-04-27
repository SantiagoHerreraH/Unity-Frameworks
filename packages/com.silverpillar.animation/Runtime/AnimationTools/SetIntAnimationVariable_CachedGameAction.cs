using SilverPillar.Core;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Animation
{
    [Serializable]
    public class SetIntAnimationVariable_CachedGameAction : ICachedGameAction
    {

        [OdinSerialize, ShowInInspector]
        private IString m_VariableName;

        [OdinSerialize, ShowInInspector]
        private IntCachedScore m_ValueToSet;

        [SerializeField]
        private WhereToGetAnimatorFrom m_WhereToGetAnimatorFrom;

        [SerializeField, ShowIf(nameof(m_WhereToGetAnimatorFrom), WhereToGetAnimatorFrom.FromCustom)]
        private Animator m_Animator;

        public SetIntAnimationVariable_CachedGameAction() { }
        public SetIntAnimationVariable_CachedGameAction(SetIntAnimationVariable_CachedGameAction other)
        {
            m_VariableName = other.m_VariableName.Clone();
            m_ValueToSet = other.m_ValueToSet.Clone() as IntCachedScore;
            m_WhereToGetAnimatorFrom = other.m_WhereToGetAnimatorFrom;
            m_Animator = other.m_Animator;
        }

        public ICachedGameAction Clone() => new SetIntAnimationVariable_CachedGameAction(this);

        public void Execute()
        {
            if (m_Animator)
                m_Animator.SetInteger(m_VariableName.CalculateString(), (int)m_ValueToSet.CalculateScore());
        }

        public GameObject GetGameObject() => m_VariableName.GetGameObject();

        public bool SetGameObject(GameObject gameObj)
        {
            bool animatorIsGood = true;
            if (m_WhereToGetAnimatorFrom == WhereToGetAnimatorFrom.FromThisGameObject)
                animatorIsGood = gameObj.TryGetComponent(out m_Animator);

            return animatorIsGood &&
                m_VariableName.SetGameObject(gameObj) &&
                m_ValueToSet.SetGameObject(gameObj);
        }
    }
}
