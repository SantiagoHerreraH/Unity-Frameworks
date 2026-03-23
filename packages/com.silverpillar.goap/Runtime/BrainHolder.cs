using UnityEngine;
using SilverPillar.Core;
using System.Collections.Generic;

namespace SilverPillar.GOAP
{
    public enum HowToChooseCurrentAction
    {
        MinPathCost,
        MaxScore,
        MaxScoreMinusMinPathCost,
        MaxScoreDividedByMinPathCost
    }

    public class BrainHolder : MonoBehaviour
    {
        [SerializeField]
        private SO_Ref<Brain> m_BrainRef = new();
        private BrainInstance m_BrainInstance = null;
        private ActionInstance m_CurrentActionInstance = null;

        private void Awake()
        {
            m_BrainInstance = m_BrainRef.Get().CreateInstance(gameObject);
        }

        void Update() 
        {
            var newAction = m_BrainInstance.GetActionInstance();
            
            if (newAction != m_CurrentActionInstance)
            {
                m_CurrentActionInstance?.EndAction();
                m_CurrentActionInstance = newAction;
                m_CurrentActionInstance.StartAction();
            }

            m_CurrentActionInstance.UpdateAction();

        }
    }
}

