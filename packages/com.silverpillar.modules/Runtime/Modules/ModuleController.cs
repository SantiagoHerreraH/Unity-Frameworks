using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Modules
{
    public class ModuleController : MonoBehaviour
    {
        private Dictionary<ModuleType, Module> m_Type_To_Module = new();

        public bool HasModule(ModuleType type)
        {
            return m_Type_To_Module.ContainsKey(type);
        }

#nullable enable
        public Module? GetModule(ModuleType type)
        {
            if (m_Type_To_Module.TryGetValue(type, out var module))
            {
                return module;
            }
            return null;
        }

        public int GetCurrent(ModuleType type)
        {
            return m_Type_To_Module.TryGetValue(type, out var module) ? module.GetCurrent() : 0;
        }

        public int GetCount(ModuleType type)
        {
            return m_Type_To_Module.TryGetValue(type, out var module) ? module.GetCount() : 0;
        }

        public bool SetCurrent(ModuleType type, int value)
        {
            if (m_Type_To_Module.TryGetValue(type, out var module))
            {
                module.SetCurrent(value);
                return true;
            }
            return false;
        }

        public bool SetCount(ModuleType type, int count)
        {
            if (m_Type_To_Module.TryGetValue(type, out var module))
            {
                module.SetCount(count);
                return true;
            }
            return false;
        }

        public bool CreateModule(ModuleType type, int current, int max)
        {
            if (!m_Type_To_Module.ContainsKey(type))
            {
                m_Type_To_Module.Add(type, new Module(current, max));
                return true;
            }
            return false;
        }
    }
}
