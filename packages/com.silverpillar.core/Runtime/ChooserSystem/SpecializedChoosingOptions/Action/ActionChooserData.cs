using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace SilverPillar.Core
{
    [CreateAssetMenu(fileName = "ActionChooserData", menuName = "SilverPillar/Core/ChoosingSystem/ActionChooserData")]
    public class ActionChooserData : SaveableScriptableObject, IAction, IChoose
    {
        [OdinSerialize, ShowInInspector]
        private ChooseAction m_ChooseAction;
        public void Choose()
        {
            m_ChooseAction.Choose();
        }

        public IAction Clone()
        {
            return m_ChooseAction.Clone();
        }

        public void EndAction()
        {
            m_ChooseAction.EndAction();
        }

        public GameObject GetGameObject()
        {
            return m_ChooseAction.GetGameObject();
        }

        public bool SetGameObject(GameObject gameObj)
        {
            return m_ChooseAction.SetGameObject(gameObj);   
        }

        public void StartAction()
        {
            m_ChooseAction.StartAction();
        }

        public void UpdateAction()
        {
            m_ChooseAction.UpdateAction();
        }
    }
}
