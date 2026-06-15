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

        private IAstarAI m_AStarAI = null;
        private GameObject m_Owner;

        public IAction Clone()
        {
            return new SetActiveRotationControl_Action
            {
                m_RotationActionOnStart = this.m_RotationActionOnStart,
                m_RotationActionOnEnd = this.m_RotationActionOnEnd,
                m_AStarAI = this.m_AStarAI,
                m_Owner = this.m_Owner
            };
        }

        public GameObject GetGameObject() => m_Owner;

        public bool SetGameObject(GameObject gameObj)
        {
            if (gameObj == null) return false;

            m_Owner = gameObj;

            gameObj.TryGetComponent(out m_AStarAI);

            return m_AStarAI != null ;
        }

        public void StartAction()
        {
            ApplyRotationSetting(m_RotationActionOnStart);
        }

        public void UpdateAction() { }

        public void EndAction()
        {
            ApplyRotationSetting(m_RotationActionOnEnd);
        }

        private void ApplyRotationSetting(RotationAction action)
        {
            if (m_Owner == null || !m_Owner.activeInHierarchy) return;

            bool shouldRotate = (action == RotationAction.ActivateRotationControl);

            if (m_AStarAI != null)
            {
                m_AStarAI.updateRotation = shouldRotate;
            }
        }
    }
}
