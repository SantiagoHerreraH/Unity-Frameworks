using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Core
{
    public class SimpleInteractionMachine : SerializedMonoBehaviour
    {
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
            Initialize();

            foreach (var interaction in m_Interactions)
            {
                interaction.Interact(other);
            }
        }
    }
}
