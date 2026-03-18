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
        private Brain m_Brain = null; //just to avoid one jump
        private ActionGroupExecutionData m_ActionGroupExecutionData = null;
        private Action m_CurrentAction = null;
        private ActionExecutionData m_CurrentActionExecutionData = null;

        private void Awake()
        {
            m_Brain = m_BrainRef.Get();
            m_ActionGroupExecutionData = m_Brain.GetData(gameObject);
        }

        void Update() 
        {
            var newAction = m_Brain.GetAction(gameObject);
            m_CurrentActionExecutionData =  m_ActionGroupExecutionData.GetActionExecutionData(newAction);
            
            if (newAction != m_CurrentAction)
            {
                m_CurrentActionExecutionData?.EndAction();
                m_CurrentAction = newAction;
                m_CurrentActionExecutionData.StartAction();
            }

            m_CurrentActionExecutionData.UpdateAction();

        }
    }
}

