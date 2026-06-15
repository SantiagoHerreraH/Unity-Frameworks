using Pathfinding;
using SilverPillar.Core;
using System;
using UnityEngine;

namespace SilverPillar.Integrations.AStar
{
    [Serializable]
    public class DeactivateFollowMovement_Action : IAction
    {
        [SerializeField]
        private bool m_ActivateMovementOnEnd;

        private GameObject m_GameObj;
        private IAstarAI m_AI;
        private Seeker m_Seeker;
        private AIDestinationSetter m_DestinationSetter;

        public DeactivateFollowMovement_Action() { }
        public DeactivateFollowMovement_Action(DeactivateFollowMovement_Action other)
        {
            m_DestinationSetter = other.m_DestinationSetter;
            m_AI = other.m_AI;
            m_Seeker = other.m_Seeker;
        }

        public IAction Clone()
        {
            return new DeactivateFollowMovement_Action(this);
        }

        public void EndAction()
        {
            if (m_ActivateMovementOnEnd)
            {
                if (m_AI != null)
                {
                    var mono = m_AI as MonoBehaviour;
                    mono.enabled = true;
                }
                if (m_Seeker != null)
                {
                    m_Seeker.enabled = true;
                }
                if (m_DestinationSetter != null)
                {
                    m_DestinationSetter.enabled = true;
                }
            }
        }

#nullable enable

        public GameObject? GetGameObject()
        {
            return m_GameObj;
        }

        public bool SetGameObject(GameObject gameObj)
        {

            if (gameObj != null)
            {
                m_GameObj = gameObj;
                gameObj.TryGetComponent(out m_AI);
                gameObj.TryGetComponent(out m_Seeker);
                gameObj.TryGetComponent(out m_DestinationSetter);

                return true;
            }

            return false;
        }

        public void StartAction()
        {
            if (m_AI != null)
            {
                var mono = m_AI as MonoBehaviour;
                mono.enabled = false;
            }
            if (m_DestinationSetter != null)
            {
                m_DestinationSetter.enabled = false;
            }
        }

        public void UpdateAction()
        {
        }
    }
}
