using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Core
{
    public class SimpleInteractionMachine : SerializedMonoBehaviour
    {
        [SerializeField]
        private bool m_DontInteractIfComponentDisabled;
        [SerializeField, ShowIf(nameof(m_DontInteractIfComponentDisabled))]
        private bool m_PrintMessageIfCalledInteractWhileComponentDisabled;
        [OdinSerialize, ShowInInspector]
        private List<IInteraction> m_Interactions = new();
        private bool m_Initialized = false;

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (!m_Initialized)
            {
                foreach (var interaction in m_Interactions)
                {
                    interaction.SetSelf(gameObject);
                }
                m_Initialized = true;
            }
        }

        public void Interact(GameObject other)
        {
            if (m_DontInteractIfComponentDisabled && !enabled)
            {
                if (m_PrintMessageIfCalledInteractWhileComponentDisabled)
                {
                    Debug.Log($"Couldn't Interact because {nameof(SimpleInteractionMachine)} component in {gameObject.name} is disabled.");
                }

                return;
            }

            Initialize();

            foreach (var interaction in m_Interactions)
            {
                interaction.Interact(other);
            }
        }
    }
}
