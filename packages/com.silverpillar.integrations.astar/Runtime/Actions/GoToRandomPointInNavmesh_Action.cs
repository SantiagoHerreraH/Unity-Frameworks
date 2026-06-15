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
    public class GoToRandomPointInNavmesh_Action : IAction
    {
        public enum HowToCalculateOnReachDestination
        {
            ReachDestination,
            ReachEndOfPath,
            ReachEndOfCrowdedPath,
            ReachCustomDistance
        }

        [SerializeField]
        private HowToCalculateOnReachDestination m_HowToCalculateOnReachDestination;

        [OdinSerialize, ShowInInspector, ShowIf(nameof(m_HowToCalculateOnReachDestination), HowToCalculateOnReachDestination.ReachCustomDistance)]
        private IsDistanceLessThan m_CustomDistanceParams = new();

        [SerializeField]
        private int m_GraphIndexOfNavMesh = 0;

        [OdinSerialize, ShowInInspector]
        private ICachedScore m_Speed;

        private AIDestinationSetter m_DestinationSetter = null;
        private IAstarAI m_AStarAI = null;
        private TargetSystem m_TargetSystem = null;

        public GoToRandomPointInNavmesh_Action() { }

        public GoToRandomPointInNavmesh_Action(GoToRandomPointInNavmesh_Action other)
        {
            m_GraphIndexOfNavMesh = other.m_GraphIndexOfNavMesh;
            m_Speed = other.m_Speed?.Clone();

            m_AStarAI = other.m_AStarAI;
            m_TargetSystem = other.m_TargetSystem;

            m_HowToCalculateOnReachDestination = other.m_HowToCalculateOnReachDestination;
            m_CustomDistanceParams = new(other.m_CustomDistanceParams);
        }

        public IAction Clone() => new GoToRandomPointInNavmesh_Action(this);

        public GameObject GetGameObject()
        {
            return m_DestinationSetter.gameObject;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            bool speedIsGood = m_Speed?.SetGameObject(gameObj) ?? false;
            m_CustomDistanceParams.Initialize(gameObj);

            gameObj.TryGetComponent(out m_DestinationSetter);
            gameObj.TryGetComponent(out m_AStarAI);

            bool hasMovementComponent = m_AStarAI != null;

            return speedIsGood && hasMovementComponent;
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

            // Obtener punto aleatorio del NavMesh/Recast Graph
            var graph = AstarPath.active.graphs[m_GraphIndexOfNavMesh];
            var sample = graph.RandomPointOnSurface(NearestNodeConstraint.Walkable);

            float speed = m_Speed?.CalculateScore() ?? 1f;

            if (m_AStarAI != null)
            {
                MonoBehaviour mono = m_AStarAI as MonoBehaviour;
                mono.enabled = true;
                m_AStarAI.isStopped = false;
                m_AStarAI.maxSpeed = speed;
                m_AStarAI.destination = sample.position;
            }
        }

        private bool ReachedDestination()
        {
            GameObject agentGo = GetGameObject();
            if (agentGo == null || !agentGo.activeInHierarchy) return false;

            bool reachedDest    = m_AStarAI.reachedDestination;
            bool reachedEnd     = m_AStarAI.reachedEndOfPath;
            Vector3 currentDest = m_AStarAI.destination;

            switch (m_HowToCalculateOnReachDestination)
            {
                case HowToCalculateOnReachDestination.ReachDestination:
                    return reachedDest;
                case HowToCalculateOnReachDestination.ReachEndOfPath:
                    return reachedEnd;
                case HowToCalculateOnReachDestination.ReachEndOfCrowdedPath:

                    FollowerEntity entity = m_AStarAI as FollowerEntity;

                    return entity ? entity.reachedCrowdedEndOfPath : reachedEnd;
                case HowToCalculateOnReachDestination.ReachCustomDistance:
                    return m_CustomDistanceParams.IsFulfilled(agentGo.transform.position, currentDest);
            }

            return false;
        }
    }
}
