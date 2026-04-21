using SilverPillar.Core;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Animation
{
    public enum WhereToGetClipPlayerFrom
    {
        FromThisGameObject,
        FromCustom
    }

    [Serializable]
    public class PlayComplexAnimationClip_CachedGameAction : ICachedGameAction
    {
        [Title("Data")]
        [OdinSerialize, ShowInInspector]
        private AnimationClipData m_ClipData = new();

        [Title("Settings")]
        [SerializeField]
        private WhereToGetClipPlayerFrom m_WhereToGetPlayerFrom;

        [SerializeField, ShowIf(nameof(m_WhereToGetPlayerFrom), WhereToGetClipPlayerFrom.FromCustom)]
        private AnimationClipPlayer m_AnimationClipPlayer;
        private GameObject m_Owner;

        public ICachedGameAction Clone()
        {
            return new PlayComplexAnimationClip_CachedGameAction
            {
                m_ClipData = this.m_ClipData,
                m_AnimationClipPlayer = this.m_AnimationClipPlayer,
                m_Owner = this.m_Owner
            };
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

        public void Execute()
        {
            if (m_AnimationClipPlayer != null && m_ClipData != null)
            {
                m_AnimationClipPlayer.PlayOneShot(m_ClipData);
            }
        }

    }
}
