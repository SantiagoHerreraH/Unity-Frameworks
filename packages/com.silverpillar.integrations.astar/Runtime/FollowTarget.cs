using UnityEngine;
using SilverPillar.Core;
using SilverPillar.Target;
using Pathfinding;
using Sirenix.OdinInspector;

namespace SilverPillar.Integrations.AStar
{
    public class FollowTarget : IAction
    {
        [BoxGroup("FollowIf")]
        [Header("Conditions On Self")]
        [SerializeField]
        private ICachedCondition m_FollowIfSelf = null;

        [BoxGroup("FollowIf")]
        [Header("Conditions On Target")]
        [SerializeField]
        private ICachedCondition m_FollowIfTarget = null;

        [BoxGroup("Movement")]
        [SerializeField, Min(0.001f)]
        private float m_MaxFollowSpeed = 1f;


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

            return
                m_FollowIfSelf.SetGameObject(gameObj) && 
                m_FollowIfTarget.SetGameObject(gameObj) &&
                gameObj.TryGetComponent<AIDestinationSetter>(out m_AIDestinationSetter) &&
                gameObj.TryGetComponent<TargetSystem>(out m_TargetSystem) &&
                gameObj.TryGetComponent<FollowerEntity>(out m_FollowerEntity);
        }

        public void StartAction()
        {

            if (m_AIDestinationSetter && m_TargetSystem && m_FollowerEntity)
            {
                if (m_TargetSystem.CurrentTarget != null)
                {
                    if (m_FollowIfSelf.IsFulfilled() && m_FollowIfTarget.IsFulfilled())
                    {
                        m_FollowerEntity.maxSpeed = m_MaxFollowSpeed;
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
