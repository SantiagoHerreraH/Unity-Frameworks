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
        private FollowerEntity m_Follower;
        private AIDestinationSetter m_DestinationSetter;

        public DeactivateFollowMovement_Action() { }
        public DeactivateFollowMovement_Action(DeactivateFollowMovement_Action other)
        {
            m_DestinationSetter = other.m_DestinationSetter;
            m_Follower = other.m_Follower;
        }

        public IAction Clone()
        {
            return new DeactivateFollowMovement_Action(this);
        }

        public void EndAction()
        {
            if (m_ActivateMovementOnEnd)
            {
                if (m_Follower != null)
                {
                    m_Follower.enabled = true;
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
                gameObj.TryGetComponent(out m_Follower);
                gameObj.TryGetComponent(out m_DestinationSetter);

                return true;
            }

            return false;
        }

        public void StartAction()
        {
            if (m_Follower != null)
            {
                m_Follower.enabled = false;
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
