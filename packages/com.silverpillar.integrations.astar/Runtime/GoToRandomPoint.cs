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
    public class IsDistanceLessThan
    {
        public IsDistanceLessThan()
        {

        }

        public IsDistanceLessThan(IsDistanceLessThan other)
        {
            m_WhatAxisToCompare = other.m_WhatAxisToCompare;
            m_ThanDistanceToCompare = other.m_ThanDistanceToCompare.Clone();
            m_ValueIfNullScore = other.m_ValueIfNullScore;
        }

        [Title("What Distance Axis to compare")]
        [SerializeField]
        private AxisFilter m_WhatAxisToCompare = AxisFilter.xyz;

        [Title("Is less or equal than ")]
        [OdinSerialize, ShowInInspector] private ICachedScore m_ThanDistanceToCompare;
        [SerializeField]
        private float m_ValueIfNullScore = 0f;


        public bool Initialize(GameObject gameObj)
        {
            return m_ThanDistanceToCompare.SetGameObject(gameObj);
        }

        public bool IsFulfilled(Vector3 firstPosition, Vector3 secondPosition)
        {
            float distanceSqr = AxisFilterTools.CalculateSquareDistance(m_WhatAxisToCompare, firstPosition, secondPosition);
            float score = m_ThanDistanceToCompare == null ? m_ValueIfNullScore : m_ThanDistanceToCompare.CalculateScore();
            return FloatComparison.Compare(distanceSqr, FloatComparison.OperationType.LessOrEqual, score * score);
        }
    }

    [Serializable]
    public class GoToRandomPoint : IAction
    {
        public enum HowToCalculateOnReachDestination
        {
            ReachDestination,
            ReachEndOfPath,
            ReachEndOfCrowdedPath,
            ReachCustomDistance
        }

        public enum WhoIsTheCenterOfTheRadius
        {
            Self, 
            CurrentTarget
        }

        [SerializeField]
        private HowToCalculateOnReachDestination m_HowToCalculateOnReachDestination;

        [OdinSerialize, ShowInInspector, ShowIf(nameof(m_HowToCalculateOnReachDestination), HowToCalculateOnReachDestination.ReachCustomDistance)]
        private IsDistanceLessThan m_CustomDistanceParams = new();

        [SerializeField, Tooltip("If you choose CurrentTarget and self doesn't have the TargetSystem component or CurrentTarget is null, it will default to self")]
        private WhoIsTheCenterOfTheRadius m_WhoIsTheCenterOfTheRadius;
        [OdinSerialize, ShowInInspector]
        private ICachedScore m_Radius;
        [OdinSerialize, ShowInInspector]
        private ICachedScore m_Speed;

        private FollowerEntity      m_FollowerEntity        = null;
        private TargetSystem        m_TargetSystem          = null;

        private AIDestinationSetter m_DestinationSetter = null;
        public GoToRandomPoint() { }

        public GoToRandomPoint(GoToRandomPoint other)
        {
            m_WhoIsTheCenterOfTheRadius = other.m_WhoIsTheCenterOfTheRadius;
            m_Radius = other.m_Radius.Clone();
            m_Speed = other.m_Speed.Clone();

            m_FollowerEntity        = other.m_FollowerEntity;
            m_TargetSystem          = other.m_TargetSystem;

            m_HowToCalculateOnReachDestination = other.m_HowToCalculateOnReachDestination;
            m_CustomDistanceParams = new(other.m_CustomDistanceParams);


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
            bool radiusIsGood = m_Radius != null ? m_Radius.SetGameObject(gameObj) : false;
            bool paramsGood = m_CustomDistanceParams.Initialize(gameObj);

            gameObj.TryGetComponent<AIDestinationSetter>(out m_DestinationSetter);

            return
                paramsGood &&
                speedIsGood &&
                radiusIsGood &&
                targetSystemGood &&
                gameObj.TryGetComponent<FollowerEntity>(out m_FollowerEntity);
        }


        public void StartAction()
        {
            SetRandomPointAsDestination();
        }

        public void UpdateAction()
        {
            if (ReachedDestination())
            {
                SetRandomPointAsDestination();
            }
        }
        public void EndAction()
        {
        }

        private void SetRandomPointAsDestination()
        {
            if (m_FollowerEntity && m_FollowerEntity.gameObject.activeInHierarchy)
            {
                m_FollowerEntity.enabled = true;

                if (m_DestinationSetter)
                {
                    m_DestinationSetter.enabled = false;
                }

                GameObject centerOfRadius = m_FollowerEntity.gameObject;

                if (m_WhoIsTheCenterOfTheRadius == WhoIsTheCenterOfTheRadius.CurrentTarget && m_TargetSystem && m_TargetSystem.CurrentTarget)
                {
                    centerOfRadius = m_TargetSystem.CurrentTarget;
                }

                m_FollowerEntity.enabled = true;
                m_FollowerEntity.isStopped = false;

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

        private bool ReachedDestination()
        {
            if (!m_FollowerEntity.gameObject.activeInHierarchy)
            {
                return false;
            }

            switch (m_HowToCalculateOnReachDestination)
            {
                case HowToCalculateOnReachDestination.ReachDestination:
                    return m_FollowerEntity.reachedDestination;
                case HowToCalculateOnReachDestination.ReachEndOfPath:
                    return m_FollowerEntity.reachedEndOfPath;
                case HowToCalculateOnReachDestination.ReachEndOfCrowdedPath:
                    return m_FollowerEntity.reachedCrowdedEndOfPath;
                case HowToCalculateOnReachDestination.ReachCustomDistance:
                    return m_CustomDistanceParams.IsFulfilled(m_FollowerEntity.gameObject.transform.position, m_FollowerEntity.destination);
                default:
                    break;
            }

            return false;
        }
    }
}
