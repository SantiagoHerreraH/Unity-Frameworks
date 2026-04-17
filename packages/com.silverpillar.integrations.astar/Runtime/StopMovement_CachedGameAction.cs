using Pathfinding; 
using SilverPillar.Core;
using System;
using UnityEngine;

namespace SilverPillar.Integrations.AStar
{
    [Serializable]
    public class StopMovement_CachedGameAction : ICachedGameAction
    {
        private FollowerEntity m_Follower;

        public StopMovement_CachedGameAction()
        {
        }

        public StopMovement_CachedGameAction(StopMovement_CachedGameAction other)
        {
            m_Follower = other.m_Follower;
        }

        public ICachedGameAction Clone()
        {
            return new StopMovement_CachedGameAction(this);
        }

        public void Execute()
        {
            if (m_Follower != null)
            {
                m_Follower.isStopped = true;
            }
        }

#nullable enable

        public GameObject? GetGameObject()
        {
            return m_Follower == null ? null : m_Follower.gameObject;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            if (gameObj != null)
            {
                return gameObj.TryGetComponent<FollowerEntity>(out m_Follower);
            }
            return false;
        }
    }
}
