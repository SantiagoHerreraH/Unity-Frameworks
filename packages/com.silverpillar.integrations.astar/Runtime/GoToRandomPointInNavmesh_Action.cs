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

        public enum WhoIsTheCenterOfTheRadius
        {
            Self,
            CurrentTarget
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
        private FollowerEntity m_FollowerEntity = null;
        private TargetSystem m_TargetSystem = null;

        public GoToRandomPointInNavmesh_Action() { }

        public GoToRandomPointInNavmesh_Action(GoToRandomPointInNavmesh_Action other)
        {
            m_GraphIndexOfNavMesh = other.m_GraphIndexOfNavMesh;
            m_Speed = other.m_Speed.Clone();

            m_FollowerEntity = other.m_FollowerEntity;
            m_TargetSystem = other.m_TargetSystem;

            m_HowToCalculateOnReachDestination = other.m_HowToCalculateOnReachDestination;
            m_CustomDistanceParams = new(other.m_CustomDistanceParams);
        }

        public IAction Clone()
        {
            return new GoToRandomPointInNavmesh_Action(this);

        }

        public GameObject GetGameObject()
        {
            var go =
                m_FollowerEntity ? m_FollowerEntity.gameObject : null;
            return go;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            bool speedIsGood = m_Speed != null ? m_Speed.SetGameObject(gameObj) : false;

            gameObj.TryGetComponent<AIDestinationSetter>(out m_DestinationSetter); //not necessary since you will just disable it
            return
                speedIsGood &&
                gameObj.TryGetComponent<FollowerEntity>(out m_FollowerEntity);
        }


        public void StartAction()
        {
            SetRandomPointAsDestination();
        }

        public void UpdateAction()
        {
            if (m_FollowerEntity &&
                ReachedDestination())
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

                m_FollowerEntity.enabled = true;
                m_FollowerEntity.isStopped = false;

                if (m_DestinationSetter)
                {
                    m_DestinationSetter.enabled = false;
                }

                var graph = AstarPath.active.graphs[m_GraphIndexOfNavMesh];
                var sample = graph.RandomPointOnSurface(NearestNodeConstraint.Walkable);

                if (m_Speed != null)
                {
                    m_FollowerEntity.maxSpeed = m_Speed.CalculateScore();
                }

                m_FollowerEntity.destination = sample.position;

            }
        }

        private bool ReachedDestination()
        {
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
