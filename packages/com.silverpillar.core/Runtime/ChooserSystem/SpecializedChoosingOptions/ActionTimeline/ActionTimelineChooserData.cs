using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace SilverPillar.Core
{
    [CreateAssetMenu(fileName = "ActionTimelineChooserData", menuName = "SilverPillar/Core/ChoosingSystem/ActionTimelineChooserData")]
    public class ActionTimelineChooserData : SaveableScriptableObject, IAction, IChoose
    {
        [OdinSerialize, ShowInInspector]
        private ChooseActionTimeline m_ChooseActionTimeline;
        public void Choose()
        {
            m_ChooseActionTimeline.Choose();
        }

        public IAction Clone()
        {
            return m_ChooseActionTimeline.Clone();
        }

        public void EndAction()
        {
            m_ChooseActionTimeline.EndAction();
        }

        public GameObject GetGameObject()
        {
            return m_ChooseActionTimeline.GetGameObject();
        }

        public bool SetGameObject(GameObject gameObj)
        {
            return m_ChooseActionTimeline.SetGameObject(gameObj);   
        }

        public void StartAction()
        {
            m_ChooseActionTimeline.StartAction();
        }

        public void UpdateAction()
        {
            m_ChooseActionTimeline.UpdateAction();
        }
    }
}
