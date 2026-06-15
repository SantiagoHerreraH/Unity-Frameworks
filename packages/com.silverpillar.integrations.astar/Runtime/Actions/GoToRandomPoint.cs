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
            ReachEndOfFollowerEntityCrowdedPath, // only relevant for FollowerEntity (RVO)
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

        private IAstarAI m_AI = null;
        private TargetSystem m_TargetSystem = null;
        private AIDestinationSetter m_DestinationSetter = null;

        public GoToRandomPoint() { }

        public GoToRandomPoint(GoToRandomPoint other)
        {
            m_WhoIsTheCenterOfTheRadius = other.m_WhoIsTheCenterOfTheRadius;
            m_Radius = other.m_Radius?.Clone();
            m_Speed = other.m_Speed?.Clone();

            m_AI = other.m_AI;
            m_TargetSystem = other.m_TargetSystem;

            m_HowToCalculateOnReachDestination = other.m_HowToCalculateOnReachDestination;
            m_CustomDistanceParams = new(other.m_CustomDistanceParams);
        }

        public IAction Clone() => new GoToRandomPoint(this);

        public GameObject GetGameObject()
        {
            return m_DestinationSetter.gameObject;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            bool targetSystemGood = m_WhoIsTheCenterOfTheRadius == WhoIsTheCenterOfTheRadius.Self ||
                gameObj.TryGetComponent(out m_TargetSystem);

            bool speedIsGood = m_Speed?.SetGameObject(gameObj) ?? false;
            bool radiusIsGood = m_Radius?.SetGameObject(gameObj) ?? false;
            bool paramsGood = m_CustomDistanceParams.Initialize(gameObj);

            gameObj.TryGetComponent(out m_DestinationSetter);
            gameObj.TryGetComponent(out m_AI);

            bool hasMovementComponent = m_AI != null;

            return paramsGood && speedIsGood && radiusIsGood && targetSystemGood && hasMovementComponent;
        }

        public void StartAction() => SetRandomPointAsDestination();

        public void UpdateAction()
        {
            if (ReachedDestination())
            {
                SetRandomPointAsDestination();
            }
        }

        public void EndAction() { }

        private void SetRandomPointAsDestination()
        {
            GameObject agentGo = GetGameObject();
            if (agentGo == null || !agentGo.activeInHierarchy) return;

            if (m_DestinationSetter) m_DestinationSetter.enabled = false;

            GameObject centerOfRadius = agentGo;
            if (m_WhoIsTheCenterOfTheRadius == WhoIsTheCenterOfTheRadius.CurrentTarget && m_TargetSystem && m_TargetSystem.CurrentTarget)
            {
                centerOfRadius = m_TargetSystem.CurrentTarget;
            }

            Vector3 randomPoint = centerOfRadius.transform.position + UnityEngine.Random.insideUnitSphere * m_Radius.CalculateScore();
            randomPoint.y = centerOfRadius.transform.position.y;

            var nearest = AstarPath.active.GetNearest(randomPoint);
            Vector3 destination = (Vector3)nearest.position;

            float speed = m_Speed?.CalculateScore() ?? 1f;

            if (m_AI != null)
            {
                MonoBehaviour mono = m_AI as MonoBehaviour;
                mono.enabled = true;
                m_AI.isStopped = false;
                m_AI.maxSpeed = speed;
                m_AI.destination = destination;
            }
        }

        private bool ReachedDestination()
        {
            GameObject agentGo = GetGameObject();
            if (agentGo == null || !agentGo.activeInHierarchy) return false;

            // Extraer flags de forma genérica
            bool reachedDest =  m_AI.reachedDestination;
            bool reachedEnd = m_AI.reachedEndOfPath;
            Vector3 currentDest = m_AI.destination;

            switch (m_HowToCalculateOnReachDestination)
            {
                case HowToCalculateOnReachDestination.ReachDestination:
                    return reachedDest;
                case HowToCalculateOnReachDestination.ReachEndOfPath:
                    return reachedEnd;
                case HowToCalculateOnReachDestination.ReachEndOfFollowerEntityCrowdedPath:

                    var entity = m_AI as FollowerEntity;

                    return entity ? entity.reachedCrowdedEndOfPath : reachedEnd;
                case HowToCalculateOnReachDestination.ReachCustomDistance:
                    return m_CustomDistanceParams.IsFulfilled(agentGo.transform.position, currentDest);
            }

            return false;
        }
    }
}
