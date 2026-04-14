using Pathfinding;
using SilverPillar.Core;
using UnityEngine;
using SilverPillar.Target;
using Sirenix.Serialization;
using Sirenix.OdinInspector;
using System;

namespace SilverPillar.Integrations.AStar
{
    [Serializable]
    public class GoToRandomPoint : IAction
    {
        public enum WhoIsTheCenterOfTheRadius
        {
            Self, 
            CurrentTarget
        }
        [SerializeField, Tooltip("If you choose CurrentTarget and self doesn't have the TargetSystem component or CurrentTarget is null, it will default to self")]
        private WhoIsTheCenterOfTheRadius m_WhoIsTheCenterOfTheRadius;
        [OdinSerialize, ShowInInspector]
        private ICachedScore m_Radius;
        [OdinSerialize, ShowInInspector]
        private ICachedScore m_Speed;

        private FollowerEntity      m_FollowerEntity        = null;
        private TargetSystem        m_TargetSystem          = null;

        public GoToRandomPoint() { }

        public GoToRandomPoint(GoToRandomPoint other)
        {
            m_WhoIsTheCenterOfTheRadius = other.m_WhoIsTheCenterOfTheRadius;
            m_Radius = other.m_Radius.Clone();

            m_FollowerEntity        = other.m_FollowerEntity;
            m_TargetSystem          = other.m_TargetSystem;

        }

        public IAction Clone()
        {
            return new GoToRandomPoint(this);

        }

        public GameObject GetGameObject()
        {
            var go = 
                m_FollowerEntity ? m_FollowerEntity.gameObject : null;
            return go;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            bool targetSystemGood = m_WhoIsTheCenterOfTheRadius == WhoIsTheCenterOfTheRadius.Self ||
                gameObj.TryGetComponent(out m_TargetSystem);

            bool speedIsGood = m_Speed != null ? m_Speed.SetGameObject(gameObj) : false;

            return
                speedIsGood &&
                targetSystemGood &&
                gameObj.TryGetComponent<FollowerEntity>(out m_FollowerEntity);
        }


        public void StartAction()
        {
            SetRandomPointAsDestination();
        }

        public void UpdateAction()
        {
            if (m_FollowerEntity && m_FollowerEntity.reachedDestination)
            {
                SetRandomPointAsDestination();
            }
        }
        public void EndAction()
        {
        }

        private void SetRandomPointAsDestination()
        {
            if (m_FollowerEntity)
            {
                GameObject centerOfRadius = m_FollowerEntity.gameObject;

                if (m_WhoIsTheCenterOfTheRadius == WhoIsTheCenterOfTheRadius.CurrentTarget && m_TargetSystem && m_TargetSystem.CurrentTarget)
                {
                    centerOfRadius = m_TargetSystem.CurrentTarget;
                }

                m_FollowerEntity.enabled = true;

                Vector3 randomPoint = centerOfRadius.transform.position + UnityEngine.Random.insideUnitSphere * m_Radius.CalculateScore();
                randomPoint.y = centerOfRadius.transform.position.y;

                var nearest = AstarPath.active.GetNearest(randomPoint);
                Vector3 destination = (Vector3)nearest.position;

                if (m_Speed != null)
                {
                    m_FollowerEntity.maxSpeed = m_Speed.CalculateScore();
                }

                m_FollowerEntity.destination = destination;
            }
        }
    }
}
