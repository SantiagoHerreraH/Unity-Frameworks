using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace SilverPillar.Modules
{
    [Serializable]
    public class ModuleData<T>
    {
        [SerializeField]
        private ModuleType m_ModuleType;

        [Title("Data Mapping")]
        [Tooltip("The index in this list corresponds to the m_CurrentModuleIndex.")]
        [SerializeField]
        private List<T> m_DataPerIndex = new();

#nullable enable
        public T? GetData(ModuleController controller)
        {
            if (!controller.HasModule(m_ModuleType))
            {
                controller.CreateModule(m_ModuleType, 0, m_DataPerIndex.Count);
            }

            int count = controller.GetCount(m_ModuleType);

            if (count < m_DataPerIndex.Count)
            {
                controller.SetCount(m_ModuleType, m_DataPerIndex.Count);
            }

            int currentIndex = controller.GetCurrent(m_ModuleType);

            return m_DataPerIndex[currentIndex];
        }
    }
}
