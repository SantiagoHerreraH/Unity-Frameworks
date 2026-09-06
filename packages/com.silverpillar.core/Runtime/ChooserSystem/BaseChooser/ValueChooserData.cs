using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Core
{
    [CreateAssetMenu(fileName = "ActionChooserData", menuName = "SilverPillar/Core/ChoosingSystem/ActionChooserData")]
    public class ValueChooserData<T> : SaveableScriptableObject, IChoose
    {
        [OdinSerialize, ShowInInspector]
        private ChooseValue<T> m_ChooseAction;
        public void Choose()
        {
            m_ChooseAction.Choose();
        }

        public ChooseValue<T> Clone()
        {
            return m_ChooseAction.Clone();
        }

    }
}
