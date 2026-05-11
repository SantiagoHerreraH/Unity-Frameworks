using UnityEngine;
using SilverPillar.Core;
using System;
using Sirenix.OdinInspector;

namespace SilverPillar.Animation
{
    [Serializable]
    public class CancelAnimationClip_CachedGameAction : ICachedGameAction
    {
        public enum CancelType
        {
            CancelClipCurrentOrQueued,
            OnlyCancelCurrent,
            OnlyCancelQueued
        }

        [SerializeField]
        private CancelType m_CancelType;

        [SerializeField, HideIf(nameof(m_CancelType), CancelType.OnlyCancelQueued)]
        private float m_OutTransitionTime = 0.2f;

        [SerializeField, Tooltip("If null, will cancel the current clip independently of what it is")]
        private AnimationClip m_ClipToCancel;

        [SerializeField]
        private SelfType m_FromWho;

        [SerializeField, ShowIf(nameof(m_FromWho), SelfType.CustomGameObject)]
        private AnimationClipPlayer m_Player;

        private GameObject m_GameObject;

        public CancelAnimationClip_CachedGameAction() { }

        public CancelAnimationClip_CachedGameAction(CancelAnimationClip_CachedGameAction other)
        {
            m_OutTransitionTime = other.m_OutTransitionTime;
            m_CancelType = other.m_CancelType;
            m_ClipToCancel = other.m_ClipToCancel;
            m_FromWho = other.m_FromWho;
            m_Player = other.m_Player;
            m_GameObject = other.m_GameObject;
        }

        public ICachedGameAction Clone()
        {
            return new CancelAnimationClip_CachedGameAction(this);
        }

        public void Execute()
        {

            AnimationClipPlayer player = GetPlayer();

            if (player == null)
                return;

            if (m_ClipToCancel != null)
            {
                switch (m_CancelType)
                {
                    case CancelType.CancelClipCurrentOrQueued:
                        player.CancelClip(m_ClipToCancel, m_OutTransitionTime, true);
                        break;
                    case CancelType.OnlyCancelCurrent:
                        player.CancelClip(m_ClipToCancel, m_OutTransitionTime, false);
                        break;
                    case CancelType.OnlyCancelQueued:
                        player.CancelQueuedClip(m_ClipToCancel);
                        break;
                    default:
                        break;
                }
            }
            else
            {
                switch (m_CancelType)
                {
                    case CancelType.CancelClipCurrentOrQueued:
                        player.CancelCurrentClip(m_OutTransitionTime, true);
                        break;
                    case CancelType.OnlyCancelCurrent:
                        player.CancelCurrentClip(m_OutTransitionTime, false);
                        break;
                    case CancelType.OnlyCancelQueued:
                        player.CancelCurrentQueuedClip();
                        break;
                    default:
                        break;
                }
            }

           

        }

        public GameObject GetGameObject()
        {
            return m_GameObject;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            m_GameObject = gameObj;

            if (m_FromWho == SelfType.ThisGameObject)
            {
                return gameObj != null && gameObj.TryGetComponent(out m_Player);
            }

            return m_Player != null;
        }

        private AnimationClipPlayer GetPlayer()
        {
            if (m_FromWho == SelfType.ThisGameObject)
            {
                if (m_Player == null && m_GameObject != null)
                {
                    m_GameObject.TryGetComponent(out m_Player);
                }
            }

            return m_Player;
        }
    }
}