using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Core
{
    public class SimpleInteractionMachine : SerializedMonoBehaviour
    {
        [Title("Block Call")]
        [SerializeField]
        private bool m_DontInteractIfComponentDisabled;
        [SerializeField]
        private bool m_DontInteractIfGameObjectDisabled;

        private bool m_ShowPrint => m_DontInteractIfComponentDisabled || m_DontInteractIfGameObjectDisabled;
        [SerializeField, ShowIf(nameof(m_ShowPrint))]
        private bool m_PrintMessageIfCalledInteractWhileDisabled;


        [Title("Interactions")]
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
            if ((m_DontInteractIfComponentDisabled && !enabled) || (m_DontInteractIfGameObjectDisabled && !gameObject.activeInHierarchy))
            {
                if (m_PrintMessageIfCalledInteractWhileDisabled)
                {
                    Debug.Log($"Couldn't Interact because {nameof(SimpleInteractionMachine)} component in {gameObject.name} is disabled or game object is deactivated.");
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
