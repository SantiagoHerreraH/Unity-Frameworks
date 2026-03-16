using UnityEngine;
using SilverPillar.Core;

namespace SilverPillar.GOAP
{
    public enum HowToChooseCurrentAction
    {
        MinPathCost,
        MaxScore,
        MaxScoreMinusMinPathCost,
        MaxScoreDividedByMinPathCost
    }

    public class GOAP_BrainHolder : MonoBehaviour
    {
        [SerializeField]
        private SO_Ref<Brain> m_BrainRef = new();
        private Brain m_Brain = null; //just to avoid one jump
        private Action m_CurrentAction = null;

        private void Awake()
        {
            m_Brain = m_BrainRef.Get();
        }

        void Update() 
        {
            var newAction = m_Brain.GetAction(gameObject);
            if (newAction != m_CurrentAction)
            {
                m_CurrentAction?.End(gameObject);
                m_CurrentAction = newAction;
                m_CurrentAction.Start(gameObject);
            }

            m_CurrentAction.Update(gameObject);

        }
    }
}

