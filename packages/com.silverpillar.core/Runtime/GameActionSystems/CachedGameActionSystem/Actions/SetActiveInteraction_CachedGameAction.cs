using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class SetActiveInteraction_CachedGameAction : ICachedGameAction
    {
        private enum OperationType
        {
            ActivateInteraction,
            DeactivateInteraction
        }

        [SerializeField]
        private SelfType m_WhichInteractionMachine;

        [SerializeField, ShowIf(nameof(m_WhichInteractionMachine), SelfType.CustomGameObject)]
        private InteractionMachine m_InteractionMachine;

        [SerializeField]
        private OperationType m_OperationType;

        [SerializeField]
        private InteractionName m_InteractionName;

        private GameObject m_Self;

        public ICachedGameAction Clone()
        {
            return new SetActiveInteraction_CachedGameAction
            {
                m_WhichInteractionMachine = m_WhichInteractionMachine,
                m_InteractionMachine = m_InteractionMachine,
                m_OperationType = m_OperationType,
                m_InteractionName = m_InteractionName,
                m_Self = m_Self
            };
        }

        public void Execute()
        {
            if (m_InteractionMachine == null)
            {
                Debug.LogError(
                    $"{nameof(SetActiveInteraction_CachedGameAction)} " +
                    $"could not execute '{m_OperationType}' for interaction " +
                    $"'{m_InteractionName}' because no valid " +
                    $"{nameof(InteractionMachine)} has been assigned or cached.",
                    m_Self);

                return;
            }

            if (m_InteractionName == null)
            {
                Debug.LogError(
                    $"{nameof(SetActiveInteraction_CachedGameAction)} " +
                    $"could not execute '{m_OperationType}' on " +
                    $"'{m_InteractionMachine.gameObject.name}' because " +
                    "no interaction name has been assigned.",
                    m_InteractionMachine);

                return;
            }

            switch (m_OperationType)
            {
                case OperationType.ActivateInteraction:

                    m_InteractionMachine.ActivateInteraction(m_InteractionName);

                    break;

                case OperationType.DeactivateInteraction:

                    m_InteractionMachine.DeactivateInteraction(m_InteractionName);

                    break;

                default:

                    Debug.LogError(
                        $"{nameof(SetActiveInteraction_CachedGameAction)} " +
                        $"could not execute interaction '{m_InteractionName}' " +
                        $"on '{m_InteractionMachine.gameObject.name}' because " +
                        $"the operation type '{m_OperationType}' is invalid.",
                        m_InteractionMachine);

                    break;
            }
        }

        public GameObject GetGameObject()
        {
            return m_Self;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            if (gameObj == null)
            {
                Debug.LogError(
                    $"{nameof(SetActiveInteraction_CachedGameAction)} " +
                    "could not set its GameObject because the provided " +
                    "GameObject is null.",
                    m_Self);

                return false;
            }

            m_Self = gameObj;

            if (m_WhichInteractionMachine == SelfType.ThisGameObject)
            {
                m_Self.TryGetComponent(out m_InteractionMachine);
            }

            if (m_InteractionMachine == null)
            {
                Debug.LogError(
                    $"{nameof(SetActiveInteraction_CachedGameAction)} " +
                    $"on '{m_Self.name}' could not cache an " +
                    $"{nameof(InteractionMachine)} because " +
                    (m_WhichInteractionMachine == SelfType.ThisGameObject
                        ? $"the GameObject '{m_Self.name}' does not contain one."
                        : "no valid custom InteractionMachine has been assigned."),
                    m_Self);

                return false;
            }

            return true;
        }
    }
}