using SilverPillar.Core;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace SilverPillar.Animation
{
    [Serializable]
    public class PlayAnimationClip_CachedGameAction : ICachedGameAction
    {
        [Title("Data")]
        [SerializeField]
        private AnimationClip m_Clip;
        [SerializeField]
        private float m_TransitionTime;
        [SerializeField, Tooltip("negative is infinite. 1 == played once")]
        private int m_LoopCount;

        [Title("Settings")]
        [SerializeField]
        private WhereToGetClipPlayerFrom m_WhereToGetPlayerFrom;

        [SerializeField, ShowIf(nameof(m_WhereToGetPlayerFrom), WhereToGetClipPlayerFrom.FromCustom)]
        private AnimationClipPlayer m_AnimationClipPlayer;
        private GameObject m_Owner;

        public ICachedGameAction Clone()
        {
            return new PlayAnimationClip_CachedGameAction
            {
                m_Clip = this.m_Clip,
                m_AnimationClipPlayer = this.m_AnimationClipPlayer,
                m_Owner = this.m_Owner
            };
        }

        public void Execute()
        {
            if (m_AnimationClipPlayer != null && m_Clip != null)
            {
                m_AnimationClipPlayer.PlayClip(m_Clip, m_TransitionTime, m_LoopCount);
            }
        }

        public GameObject GetGameObject() => m_Owner;

        public bool SetGameObject(GameObject gameObj)
        {
            if (gameObj == null) return false;

            m_Owner = gameObj;

            if (m_WhereToGetPlayerFrom == WhereToGetClipPlayerFrom.FromThisGameObject)
            {
                gameObj.TryGetComponent(out m_AnimationClipPlayer);
            }

            return m_AnimationClipPlayer != null;
        }
    }
}
