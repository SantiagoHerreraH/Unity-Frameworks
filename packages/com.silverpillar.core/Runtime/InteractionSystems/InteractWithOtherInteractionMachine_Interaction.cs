using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace SilverPillar.Core
{
    public enum WhereToGetTargetComponentFrom
    {
        Target,
        TargetOrChildren,
        TargetParent
    }

    [Serializable]
    public class InteractWithOtherInteractionMachine_Interaction : IInteraction
    {
        private enum InteractionFunctionToCall
        {
            InteractWithDefault,
            InteractWithAllActive,
            InteractWithSpecificInteraction,
            ActivateInteraction,
            DeactivateInteraction
        }

        [SerializeField]
        private SelfType m_WhichGameObjectWillBeSelf;

        [SerializeField, ShowIf(
            nameof(m_WhichGameObjectWillBeSelf),
            SelfType.CustomGameObject)]
        private GameObject m_CustomSelf;

        [SerializeField]
        private WhereToGetTargetComponentFrom m_WhereToGetTargetComponentFrom;

        [SerializeField, ShowIf(nameof(m_ShowGetIfComponentInactive))]
        private bool m_GetIfComponentInactive = true;

        [SerializeField]
        private InteractionFunctionToCall m_InteractionFunctionToCall;

        private bool m_ShowGetIfComponentInactive =>
            m_WhereToGetTargetComponentFrom !=
            WhereToGetTargetComponentFrom.Target;

        private bool m_ShowInteractionName =>
            m_InteractionFunctionToCall !=
                InteractionFunctionToCall.InteractWithDefault &&
            m_InteractionFunctionToCall !=
                InteractionFunctionToCall.InteractWithAllActive;

        [SerializeField, ShowIf(nameof(m_ShowInteractionName))]
        private InteractionName m_InteractionName;

        private GameObject m_Self;

        public InteractWithOtherInteractionMachine_Interaction() { }

        public InteractWithOtherInteractionMachine_Interaction(
            InteractWithOtherInteractionMachine_Interaction other)
        {
            m_WhichGameObjectWillBeSelf =
                other.m_WhichGameObjectWillBeSelf;

            m_CustomSelf = other.m_CustomSelf;

            m_WhereToGetTargetComponentFrom =
                other.m_WhereToGetTargetComponentFrom;

            m_GetIfComponentInactive = other.m_GetIfComponentInactive;
            m_InteractionFunctionToCall = other.m_InteractionFunctionToCall;
            m_InteractionName = other.m_InteractionName;

            m_Self = other.m_Self;
        }

        public IInteraction Clone()
        {
            return new InteractWithOtherInteractionMachine_Interaction(this);
        }

        public GameObject GetSelf()
        {
            return m_Self;
        }

        public bool SetSelf(GameObject self)
        {
            if (self == null)
            {
                Debug.LogError(
                    $"{nameof(InteractWithOtherInteractionMachine_Interaction)} " +
                    "could not set self because the provided GameObject is null.",
                    m_Self);

                return false;
            }

            m_Self = self;
            return true;
        }

        public void Interact(GameObject target)
        {
            if (target == null)
            {
                Debug.LogWarning(
                    $"{nameof(InteractWithOtherInteractionMachine_Interaction)} " +
                    "could not execute because the target GameObject is null.",
                    m_Self);

                return;
            }

            if (m_ShowInteractionName && m_InteractionName == null)
            {
                Debug.LogError(
                    $"{nameof(InteractWithOtherInteractionMachine_Interaction)} " +
                    $"could not execute '{m_InteractionFunctionToCall}' " +
                    $"on '{target.name}' because no interaction name has been assigned.",
                    m_Self);

                return;
            }

            // Only resolve self when the selected function requires it.
            GameObject chosenSelf = null;

            if (m_InteractionFunctionToCall !=
                    InteractionFunctionToCall.ActivateInteraction &&
                m_InteractionFunctionToCall !=
                    InteractionFunctionToCall.DeactivateInteraction)
            {
                chosenSelf =
                    m_WhichGameObjectWillBeSelf == SelfType.CustomGameObject
                        ? m_CustomSelf
                        : m_Self;

                if (chosenSelf == null)
                {
                    Debug.LogError(
                        $"{nameof(InteractWithOtherInteractionMachine_Interaction)} " +
                        $"could not execute '{m_InteractionFunctionToCall}' " +
                        $"on '{target.name}' because the chosen self GameObject " +
                        $"is null. Self type: '{m_WhichGameObjectWillBeSelf}'.",
                        m_Self);

                    return;
                }
            }

            InteractionMachine machine;

            switch (m_WhereToGetTargetComponentFrom)
            {
                case WhereToGetTargetComponentFrom.Target:

                    machine = target.GetComponent<InteractionMachine>();

                    break;

                case WhereToGetTargetComponentFrom.TargetOrChildren:

                    machine = target.GetComponentInChildren<InteractionMachine>(
                        m_GetIfComponentInactive);

                    break;

                case WhereToGetTargetComponentFrom.TargetParent:

                    machine = target.GetComponentInParent<InteractionMachine>(
                        m_GetIfComponentInactive);

                    break;

                default:

                    Debug.LogError(
                        $"{nameof(InteractWithOtherInteractionMachine_Interaction)} " +
                        $"could not retrieve an {nameof(InteractionMachine)} " +
                        $"from '{target.name}' because the component retrieval " +
                        $"option '{m_WhereToGetTargetComponentFrom}' is invalid.",
                        m_Self);

                    return;
            }

            if (machine == null)
            {
                Debug.LogWarning(
                    $"{nameof(InteractWithOtherInteractionMachine_Interaction)} " +
                    $"could not execute '{m_InteractionFunctionToCall}' " +
                    $"on '{target.name}' because no {nameof(InteractionMachine)} " +
                    $"was found using '{m_WhereToGetTargetComponentFrom}'.",
                    m_Self);

                return;
            }

            switch (m_InteractionFunctionToCall)
            {
                case InteractionFunctionToCall.InteractWithDefault:

                    machine.InteractWithDefault(chosenSelf);

                    break;

                case InteractionFunctionToCall.InteractWithAllActive:

                    machine.InteractWithAllActive(chosenSelf);

                    break;

                case InteractionFunctionToCall.InteractWithSpecificInteraction:

                    machine.Interact(m_InteractionName, chosenSelf);

                    break;

                case InteractionFunctionToCall.ActivateInteraction:

                    machine.ActivateInteraction(m_InteractionName);

                    break;

                case InteractionFunctionToCall.DeactivateInteraction:

                    machine.DeactivateInteraction(m_InteractionName);

                    break;

                default:

                    Debug.LogError(
                        $"{nameof(InteractWithOtherInteractionMachine_Interaction)} " +
                        $"could not execute an interaction on '{target.name}' " +
                        $"because the interaction function " +
                        $"'{m_InteractionFunctionToCall}' is invalid.",
                        m_Self);

                    break;
            }
        }
    }
}