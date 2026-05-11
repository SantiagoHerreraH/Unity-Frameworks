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
        private ICachedScore m_MaxFollowSpeed ;


        AIDestinationSetter m_AIDestinationSetter = null;
        TargetSystem m_TargetSystem = null;
        FollowerEntity m_FollowerEntity = null;

        public FollowTarget()
        {

        }

        public FollowTarget(FollowTarget other)
        {
            m_FollowIfSelf = other.m_FollowIfSelf;
            m_FollowIfTarget = other.m_FollowIfTarget;
            m_MaxFollowSpeed = other.m_MaxFollowSpeed;

            m_AIDestinationSetter   = other.m_AIDestinationSetter;
            m_TargetSystem          = other.m_TargetSystem        ;
            m_FollowerEntity        = other.m_FollowerEntity      ; 
        }

        public IAction Clone()
        {
            return new FollowTarget(this);
        }

        public GameObject GetGameObject()
        {
            var go = 
                m_AIDestinationSetter ? m_AIDestinationSetter.gameObject :
                m_TargetSystem ? m_TargetSystem.gameObject :
                m_FollowerEntity ? m_FollowerEntity.gameObject : null;
            return go;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            bool followIfSelf   = m_FollowIfSelf != null ? m_FollowIfSelf.SetGameObject(gameObj) : true;
            bool maxFollowSpeed = m_MaxFollowSpeed != null ? m_MaxFollowSpeed.SetGameObject(gameObj) : true;
            bool followIfTarget = m_FollowIfTarget != null ? m_FollowIfTarget.SetGameObject(gameObj) : true;

            return
                followIfSelf &&
                maxFollowSpeed && 
                followIfTarget &&
                gameObj.TryGetComponent<AIDestinationSetter>(out m_AIDestinationSetter) &&
                gameObj.TryGetComponent<TargetSystem>(out m_TargetSystem) &&
                gameObj.TryGetComponent<FollowerEntity>(out m_FollowerEntity);
        }

        public void StartAction()
        {
            if (m_AIDestinationSetter.gameObject.activeInHierarchy && 
                m_AIDestinationSetter && m_TargetSystem && m_FollowerEntity)
            {
                if (m_TargetSystem.CurrentTarget != null)
                {
                    bool followIfSelfIsFulfilled = m_FollowIfSelf != null ? m_FollowIfSelf.IsFulfilled() : true;
                    bool followIfTargetIsFulfilled = m_FollowIfTarget != null ? m_FollowIfTarget.IsFulfilled() : true;

                    if (followIfSelfIsFulfilled && followIfTargetIsFulfilled)
                    {
                        m_AIDestinationSetter.enabled = true;
                        m_FollowerEntity.enabled = true;
                        m_FollowerEntity.isStopped = false;
                        m_FollowerEntity.maxSpeed = m_MaxFollowSpeed != null ? m_MaxFollowSpeed.CalculateScore() : 1f;
                        m_AIDestinationSetter.target = m_TargetSystem.CurrentTarget.transform;
                    }
                }
            }
        }
        public void UpdateAction()
        {
        }
        public void EndAction()
        {
        }

    }
}
