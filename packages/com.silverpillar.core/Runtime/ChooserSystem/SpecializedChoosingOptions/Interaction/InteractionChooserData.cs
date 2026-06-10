using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace SilverPillar.Core
{
    [CreateAssetMenu(fileName = "InteractionChooserData", menuName = "SilverPillar/Core/ChoosingSystem/InteractionChooserData")]
    public class InteractionChooserData : ScriptableObject, IInteraction, IChoose
    {
        [OdinSerialize, ShowInInspector]
        private ChooseInteraction m_ChooseInteraction;

        public void Choose()
        {
            m_ChooseInteraction.Choose();
        }

        public IInteraction Clone()
        {
            return m_ChooseInteraction.Clone();
        }

        public GameObject GetSelf()
        {
            return m_ChooseInteraction.GetSelf();
        }

        public void Interact(GameObject other)
        {
            m_ChooseInteraction.Interact(other);
        }

        public bool SetSelf(GameObject self)
        {
            return m_ChooseInteraction.SetSelf(self);
        }
    }
}
