using System;
using UnityEngine;

namespace SilverPillar.Modules
{
#nullable enable

    [Serializable]
    public class Module
    {
        [SerializeField] 
        private int m_CurrentModuleIndex;
        [SerializeField]
        private int m_ModuleCount;

        private Action<int>? m_OnChangeCurrentModule;
        private Action<int>? m_OnSetCurrentModule;
        private Action<int>? m_OnChangeModuleCount; 
        private Action<int>? m_OnSetModuleCount;

        public Module() { }

        public Module(int current, int max)
        {
            m_ModuleCount = max;
            m_CurrentModuleIndex = Mathf.Clamp(current, 0, max - 1);
        }

        public void SetCurrent(int current)
        {
            int previous = m_CurrentModuleIndex;
            m_CurrentModuleIndex = Mathf.Clamp(current, 0, m_ModuleCount);

            m_OnSetCurrentModule?.Invoke(m_CurrentModuleIndex);

            if (previous != m_CurrentModuleIndex)
            {
                m_OnChangeCurrentModule?.Invoke(m_CurrentModuleIndex);
            }
        }

        public void SetCount(int count)
        {
            int previousMax = m_ModuleCount;
            m_ModuleCount = Mathf.Max(0, count);

            m_OnSetModuleCount?.Invoke(m_ModuleCount);

            if (previousMax != m_ModuleCount)
            {
                m_OnChangeModuleCount?.Invoke(m_ModuleCount);

                if (m_CurrentModuleIndex >= m_ModuleCount)
                {
                    SetCurrent(m_ModuleCount - 1);
                }
            }
        }

        public int GetCurrent() => m_CurrentModuleIndex;

        public int GetCount() => m_ModuleCount;

        public float GetPercentage() => m_ModuleCount > 0 ? (float)(m_CurrentModuleIndex + 1) / m_ModuleCount : 0f;
    }
}
