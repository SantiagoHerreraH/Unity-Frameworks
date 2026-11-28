using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SilverPillar.Core
{
    public class InteractionMachine : SerializedMonoBehaviour
    {
        [Serializable]
        private class InteractionData
        {
            [Title("Settings")]
            public bool IsActive = true;

            [Title("Interactions")]
            public List<IInteraction> Interactions;
        }
        [SerializeField]
        private bool m_DontInteractIfComponentDisabled;

        [SerializeField, ShowIf(nameof(m_DontInteractIfComponentDisabled))]
        private bool m_PrintMessageIfCalledInteractWhileComponentDisabled;

        [SerializeField]
        private InteractionName m_DefaultInteraction;

        [OdinSerialize, ShowInInspector, DictionaryDrawerSettings(
            DisplayMode = DictionaryDisplayOptions.CollapsedFoldout,
            KeyLabel = nameof(InteractionName),
            ValueLabel = "Interactions"), PropertySpace(SpaceAfter = 10)]
        private Dictionary<InteractionName, InteractionData> m_Interactions = new();

        private bool m_Initialized = false;

        private void Awake()
        {
            Initialize();
        }

        private bool Initialize()
        {
            if (!m_Initialized)
            {
                if (m_Interactions == null)
                {
                    Debug.LogError(
                        $"{nameof(InteractionMachine)} on '{gameObject.name}' " +
                        "could not initialize because its interactions dictionary is null.",
                        this);

                    return false;
                }

                if (m_DefaultInteraction != null &&
                    !m_Interactions.ContainsKey(m_DefaultInteraction))
                {
                    Debug.LogError(
                        $"{nameof(InteractionMachine)} on '{gameObject.name}' " +
                        $"could not initialize because the default interaction " +
                        $"'{m_DefaultInteraction}' is null or is not registered " +
                        "in the interactions dictionary.",
                        this);

                    m_DefaultInteraction = null;
                    return false;
                }

                bool initialized = true;

                foreach (var pair in m_Interactions)
                {
                    if (pair.Value == null)
                    {
                        Debug.LogError(
                            $"{nameof(InteractionMachine)} on '{gameObject.name}' " +
                            $"could not initialize interaction '{pair.Key}' " +
                            "because its interaction list is null.",
                            this);

                        initialized = false;
                        continue;
                    }

                    if (pair.Value.Interactions.Count == 0)
                    {
                        Debug.LogError(
                            $"{nameof(InteractionMachine)} on '{gameObject.name}' " +
                            $"could not initialize interaction '{pair.Key}' " +
                            "because its interaction list is empty.",
                            this);

                        initialized = false;
                        continue;
                    }

                    for (int i = 0; i < pair.Value.Interactions.Count; i++)
                    {
                        var interaction = pair.Value.Interactions[i];

                        if (interaction == null)
                        {
                            Debug.LogError(
                                $"{nameof(InteractionMachine)} on '{gameObject.name}' " +
                                $"contains a null interaction at index {i} " +
                                $"in the interaction list for '{pair.Key}'.",
                                this);

                            initialized = false;
                            continue;
                        }

                        interaction.SetSelf(gameObject);
                    }
                }

                m_Initialized = initialized;
            }

            return m_Initialized;
        }

        public void SetDefaultInteraction(InteractionName name)
        {
            if (!Initialize())
            {
                Debug.LogError(
                    $"{nameof(InteractionMachine)} on '{gameObject.name}' " +
                    $"could not set the default interaction to '{name}' " +
                    "because the component failed to initialize.",
                    this);

                return;
            }

            if (name != null && m_Interactions.ContainsKey(name))
            {
                m_DefaultInteraction = name;
            }
            else
            {
                Debug.LogWarning(
                    $"{nameof(InteractionMachine)} on '{gameObject.name}' " +
                    $"could not set the default interaction to '{name}' " +
                    "because the interaction name is null or is not registered " +
                    "in the interactions dictionary.",
                    this);
            }
        }

        public void InteractWithDefault(GameObject other)
        {
            if (m_DefaultInteraction != null)
            {
                Interact(m_DefaultInteraction, other);
            }
        }

        public void DeactivateInteraction(InteractionName interactionName)
        {
            if (!Initialize())
            {
                Debug.LogError(
                    $"{nameof(InteractionMachine)} on '{gameObject.name}' " +
                    $"could not deactivate interaction '{interactionName}' " +
                    "because the component failed to initialize.",
                    this);

                return;
            }

            if (interactionName == null ||
                !m_Interactions.ContainsKey(interactionName))
            {
                Debug.LogWarning(
                    $"{nameof(InteractionMachine)} on '{gameObject.name}' " +
                    $"could not deactivate interaction '{interactionName}' " +
                    "because the interaction name is null or is not registered " +
                    "in the interactions dictionary.",
                    this);

                return;
            }

            m_Interactions[interactionName].IsActive = false;
        }

        public void ActivateInteraction(InteractionName interactionName)
        {
            if (!Initialize())
            {
                Debug.LogError(
                    $"{nameof(InteractionMachine)} on '{gameObject.name}' " +
                    $"could not activate interaction '{interactionName}' " +
                    "because the component failed to initialize.",
                    this);

                return;
            }

            if (interactionName == null ||
                !m_Interactions.ContainsKey(interactionName))
            {
                Debug.LogWarning(
                    $"{nameof(InteractionMachine)} on '{gameObject.name}' " +
                    $"could not activate interaction '{interactionName}' " +
                    "because the interaction name is null or is not registered " +
                    "in the interactions dictionary.",
                    this);

                return;
            }

            m_Interactions[interactionName].IsActive = true;
        }

        public void Interact(InteractionName interactionName, GameObject other)
        {
            if (m_DontInteractIfComponentDisabled && !enabled)
            {
                if (m_PrintMessageIfCalledInteractWhileComponentDisabled)
                {
                    Debug.Log(
                        $"{nameof(InteractionMachine)} on '{gameObject.name}' " +
                        $"could not execute interaction '{interactionName}' " +
                        "because the component is disabled.",
                        this);
                }

                return;
            }

            if (!Initialize())
            {
                Debug.LogError(
                    $"{nameof(InteractionMachine)} on '{gameObject.name}' " +
                    $"could not execute interaction '{interactionName}' " +
                    "because the component failed to initialize.",
                    this);

                return;
            }

            if (interactionName == null ||
                !m_Interactions.TryGetValue(interactionName, out var interactions))
            {
                Debug.LogWarning(
                    $"{nameof(InteractionMachine)} on '{gameObject.name}' " +
                    $"could not execute interaction '{interactionName}' " +
                    "because the interaction name is null or is not registered " +
                    "in the interactions dictionary.",
                    this);

                return;
            }

            if (!m_Interactions[interactionName].IsActive)
            {
                return;
            }

            foreach (var interaction in interactions.Interactions)
            {
                interaction.Interact(other);
            }
        }
        public void InteractWithAllActive(GameObject other)
        {
            if (m_DontInteractIfComponentDisabled && !enabled)
            {
                if (m_PrintMessageIfCalledInteractWhileComponentDisabled)
                {
                    Debug.Log(
                        $"{nameof(InteractionMachine)} on '{gameObject.name}' " +
                        "could not execute all active interactions " +
                        "because the component is disabled.",
                        this);
                }

                return;
            }

            if (!Initialize())
            {
                Debug.LogError(
                    $"{nameof(InteractionMachine)} on '{gameObject.name}' " +
                    "could not execute all active interactions " +
                    "because the component failed to initialize.",
                    this);

                return;
            }

            foreach (var pair in m_Interactions)
            {
                var interactionsData = pair.Value;

                if (!interactionsData.IsActive)
                {
                    continue;
                }

                var interactions = interactionsData.Interactions;

                for (int i = 0; i < interactions.Count; i++)
                {
                    interactions[i].Interact(other);
                }
            }
        }
    }
}