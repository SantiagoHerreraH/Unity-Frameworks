using SilverPillar.Core;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Animation
{
    [Serializable]
    public class SetFloatAnimationVariable_CachedGameAction : ICachedGameAction
    {
        [Title("Data")]
        [OdinSerialize, ShowInInspector]
        private IString m_VariableName;

        [OdinSerialize, ShowInInspector]
        private ICachedScore m_ValueToSet; // Usamos ICachedScore para obtener el float

        [SerializeField]
        private WhereToGetAnimatorFrom m_WhereToGetAnimatorFrom;

        [SerializeField, ShowIf(nameof(m_WhereToGetAnimatorFrom), WhereToGetAnimatorFrom.FromCustom)]
        private Animator m_Animator;


        [Title("Debug")]
        [SerializeField]
        private bool m_PrintValue;

        public SetFloatAnimationVariable_CachedGameAction() { }
        public SetFloatAnimationVariable_CachedGameAction(SetFloatAnimationVariable_CachedGameAction other)
        {
            m_VariableName = other.m_VariableName.Clone();
            m_ValueToSet = other.m_ValueToSet.Clone();
            m_WhereToGetAnimatorFrom = other.m_WhereToGetAnimatorFrom;
            m_Animator = other.m_Animator;
        }

        public ICachedGameAction Clone() => new SetFloatAnimationVariable_CachedGameAction(this);

        public void Execute()
        {
            if (m_Animator)
            {
                var varName = m_VariableName.CalculateString();
                var value = m_ValueToSet.CalculateScore();

                m_Animator.SetFloat(varName, value);

                if (m_PrintValue)
                {
                    Debug.Log($"[AnimationAction] Setting {varName} to {value} on {m_Animator.gameObject.name}", m_Animator);
                }
            }
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
