using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace SilverPillar.Core
{
    [CreateAssetMenu(fileName = "CachedGameActionChooserData", menuName = "SilverPillar/Core/ChoosingSystem/CachedGameActionChooserData")]
    public class CachedGameActionChooserData : ScriptableObject, ICachedGameAction, IChoose
    {
        [OdinSerialize, ShowInInspector]
        private ChooseCachedGameAction m_ChooseCachedGameAction;

        public void Choose()
        {
            m_ChooseCachedGameAction.Choose();
        }

        public ICachedGameAction Clone()
        {
            return m_ChooseCachedGameAction.Clone();
        }

        public GameObject GetGameObject()
        {
            return m_ChooseCachedGameAction.GetGameObject();
        }

        public void Execute()
        {
            m_ChooseCachedGameAction.Execute();
        }

        public bool SetGameObject(GameObject gameObj)
        {
            return m_ChooseCachedGameAction.SetGameObject(gameObj);
        }
    }
}
