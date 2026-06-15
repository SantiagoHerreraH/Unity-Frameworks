using UnityEngine;
using SilverPillar.Core;
using SilverPillar.Target;
using Pathfinding;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

namespace SilverPillar.Integrations.AStar
{
    public class FollowTarget : IAction
    {
        [BoxGroup("FollowIf")]
        [Header("Conditions On Self")]
        [OdinSerialize, ShowInInspector]
        private ICachedCondition m_FollowIfSelf = null;

        [BoxGroup("FollowIf")]
        [Header("Conditions On Target")]
        [OdinSerialize, ShowInInspector]
        private ICachedCondition m_FollowIfTarget = null;

        [BoxGroup("Movement")]
        [OdinSerialize, ShowInInspector, Tooltip("If null will default to 1")]
        private ICachedScore m_MaxFollowSpeed;

        private AIDestinationSetter m_AIDestinationSetter = null;
        private TargetSystem m_TargetSystem = null;
        private IAstarAI m_AIPath = null;

        public FollowTarget() { }

        public FollowTarget(FollowTarget other)
        {
            m_FollowIfSelf = other.m_FollowIfSelf;
            m_FollowIfTarget = other.m_FollowIfTarget;
            m_MaxFollowSpeed = other.m_MaxFollowSpeed;

            m_AIDestinationSetter = other.m_AIDestinationSetter;
            m_TargetSystem = other.m_TargetSystem;
            m_AIPath = other.m_AIPath;
        }

        public IAction Clone() => new FollowTarget(this);

        public GameObject GetGameObject()
        {
            return m_AIDestinationSetter.gameObject;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            bool followIfSelf = m_FollowIfSelf?.SetGameObject(gameObj) ?? true;
            bool maxFollowSpeed = m_MaxFollowSpeed?.SetGameObject(gameObj) ?? true;
            bool followIfTarget = m_FollowIfTarget?.SetGameObject(gameObj) ?? true;


            bool hasMovementComponent =
            gameObj.TryGetComponent(out m_AIPath);

            return followIfSelf &&
                   maxFollowSpeed &&
                   followIfTarget &&
                   gameObj.TryGetComponent(out m_AIDestinationSetter) &&
                   gameObj.TryGetComponent(out m_TargetSystem) &&
                   hasMovementComponent;
        }

        public void StartAction()
        {
            if (m_AIDestinationSetter == null || !m_AIDestinationSetter.gameObject.activeInHierarchy) return;

            if (m_TargetSystem != null && m_TargetSystem.CurrentTarget != null)
            {
                bool followIfSelfIsFulfilled = m_FollowIfSelf?.IsFulfilled() ?? true;
                bool followIfTargetIsFulfilled = m_FollowIfTarget?.IsFulfilled() ?? true;

                if (followIfSelfIsFulfilled && followIfTargetIsFulfilled)
                {
                    float speed = m_MaxFollowSpeed != null ? m_MaxFollowSpeed.CalculateScore() : 1f;
                    
                    m_AIDestinationSetter.enabled = true;
                    m_AIDestinationSetter.target = m_TargetSystem.CurrentTarget.transform;

                    if (m_AIPath != null)
                    {
                        MonoBehaviour mono = m_AIPath as MonoBehaviour;
                        mono.enabled = true;
                        m_AIPath.isStopped = false;
                        m_AIPath.maxSpeed = speed;
                    }
                }
            }
        }

        public void UpdateAction() { }

        public void EndAction()
        {
        }
    }
}
