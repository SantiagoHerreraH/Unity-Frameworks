using Pathfinding;
using SilverPillar.Core;
using System;
using UnityEngine;

namespace SilverPillar.Integrations.AStar
{
    [Serializable]
    public class SetActiveRotationControl_Action : IAction
    {
        public enum RotationAction
        {
            ActivateRotationControl,
            DeactivateRotationControl
        }

        [SerializeField]
        private RotationAction m_RotationActionOnStart;
        [SerializeField]
        private RotationAction m_RotationActionOnEnd;

        private FollowerEntity m_FollowerEntity;
        private GameObject m_Owner;

        public IAction Clone()
        {
            return new SetActiveRotationControl_Action
            {
                m_RotationActionOnStart = this.m_RotationActionOnStart,
                m_RotationActionOnEnd = this.m_RotationActionOnEnd,
                m_FollowerEntity = this.m_FollowerEntity,
                m_Owner = this.m_Owner
            };
        }

        public GameObject GetGameObject() => m_Owner;

        public bool SetGameObject(GameObject gameObj)
        {
            if (gameObj == null) return false;

            m_Owner = gameObj;

            return gameObj.TryGetComponent(out m_FollowerEntity);
        }

        public void StartAction()
        {
            ApplyRotationSetting(m_RotationActionOnStart);
        }

        public void UpdateAction()
        {
        }

        public void EndAction()
        {
            ApplyRotationSetting(m_RotationActionOnEnd);
        }

        private void ApplyRotationSetting(RotationAction action)
        {
            if (m_FollowerEntity == null) return;
            if (!m_FollowerEntity.gameObject.activeInHierarchy)
            {
                return;
            }

            bool shouldRotate = (action == RotationAction.ActivateRotationControl);
            m_FollowerEntity.updateRotation = shouldRotate;
        }
    }
}
