using Sirenix.Serialization;
using UnityEngine;

namespace SilverPillar.Core
{
    [CreateAssetMenu(fileName = "ScriptableAction", menuName = "SilverPillar/Core/ActionSystem/ScriptableAction")]
    public class ScriptableAction : SaveableScriptableObject, IAction
    {
        [OdinSerialize]
        private IAction m_Action;
        public IAction Clone()
        {
            return m_Action.Clone();   
        }

        public void EndAction()
        {
            m_Action.EndAction();
        }

        public GameObject GetGameObject()
        {
            return m_Action.GetGameObject();
        }

        public bool SetGameObject(GameObject gameObj)
        {
            return m_Action.SetGameObject(gameObj);
        }

        public void StartAction()
        {
            m_Action.StartAction();
        }

        public void UpdateAction()
        {
            m_Action.UpdateAction();
        }
    }
}
