using System.Collections.Generic;
using SilverPillar.Core;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Animation
{

    [Serializable]
    public class PlayComplexAnimationSequence_CachedGameAction : ICachedGameAction
    {

        [Title("Settings")]
        [SerializeField]
        private WhereToGetClipPlayerFrom m_WhereToGetPlayerFrom;

        [SerializeField, ShowIf(nameof(m_WhereToGetPlayerFrom), WhereToGetClipPlayerFrom.FromCustom)]
        private AnimationClipPlayer m_AnimationClipPlayer;

        [Title("Sequence Settings")]
        [SerializeField]
        private AnimationSequenceType m_WhatToDoAfterFinishedSequence;

        [SerializeField, HideIf(nameof(m_WhatToDoAfterFinishedSequence), AnimationSequenceType.Stop)]
        [Tooltip("-1 for infinite, 1 for single sequence execution")]
        private int m_SequenceLoopCount = 1;

        [Title("Sequence")]
        [OdinSerialize, ShowInInspector]
        private List<AnimationClipData> m_ClipSequence = new();

        private GameObject m_Owner;

        public ICachedGameAction Clone()
        {
            // Standard shallow copy for the action settings
            return new PlayComplexAnimationSequence_CachedGameAction
            {
                m_WhereToGetPlayerFrom = this.m_WhereToGetPlayerFrom,
                m_AnimationClipPlayer = this.m_AnimationClipPlayer,
                m_WhatToDoAfterFinishedSequence = this.m_WhatToDoAfterFinishedSequence,
                m_SequenceLoopCount = this.m_SequenceLoopCount,
                m_ClipSequence = new List<AnimationClipData>(this.m_ClipSequence),
                m_Owner = this.m_Owner
            };
        }

        public void Execute()
        {
            AnimationClipPlayer player = GetTargetPlayer();

            if (player == null)
            {
                Debug.LogWarning($"[SequenceAction] No AnimationClipPlayer found on {m_Owner.name}");
                return;
            }

            if (m_ClipSequence == null || m_ClipSequence.Count == 0) return;

            // Prepare the actual list to play based on the ForwardThenBackward logic
            List<AnimationClipData> finalSequence = BuildSequence();

            // We call a Coroutine-based sequence runner on the player
            // You will need to add a 'PlaySequence' method to your AnimationClipPlayer
            player.PlaySequence(finalSequence, m_WhatToDoAfterFinishedSequence, m_SequenceLoopCount);
        }

        private List<AnimationClipData> BuildSequence()
        {
            List<AnimationClipData> result = new List<AnimationClipData>(m_ClipSequence);

            if (m_WhatToDoAfterFinishedSequence == AnimationSequenceType.ForwardThenBackward)
            {
                // Add the sequence in reverse, excluding the last and first to avoid duplicates at the pivots
                for (int i = m_ClipSequence.Count - 2; i >= 1; i--)
                {
                    result.Add(m_ClipSequence[i]);
                }
            }

            return result;
        }

        private AnimationClipPlayer GetTargetPlayer()
        {
            if (m_WhereToGetPlayerFrom == WhereToGetClipPlayerFrom.FromCustom)
                return m_AnimationClipPlayer;

            return m_Owner != null ? m_Owner.GetComponent<AnimationClipPlayer>() : null;
        }

        public GameObject GetGameObject() => m_Owner;

        public bool SetGameObject(GameObject gameObj)
        {
            m_Owner = gameObj;
            return m_Owner != null;
        }
    }
}
