using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Modules
{
    public class BoolModuleController : MonoBehaviour
    {
        private Dictionary<BoolModuleType, BoolModule> m_Type_To_BoolModule = new();
        
        public bool HasBoolModule(BoolModuleType type)
        {
            return m_Type_To_BoolModule.ContainsKey(type);
        }

#nullable enable
        public BoolModule? GetBoolModule(BoolModuleType type)
        {
            if (m_Type_To_BoolModule.ContainsKey(type))
            {
                return m_Type_To_BoolModule[type];
            }
            return null;
        }
        public bool GetState(BoolModuleType type)
        {
            if (m_Type_To_BoolModule.ContainsKey(type))
            {
                return m_Type_To_BoolModule[type].GetState();
            }
            return false;
        }

        public bool SetState(BoolModuleType type, bool state)
        {
            if (m_Type_To_BoolModule.ContainsKey(type))
            {
                m_Type_To_BoolModule[type].SetState(state);
                return true;
            }
            return false;
        }


        public bool CreateBoolModule(BoolModuleType type, bool state)
        {
            if (!m_Type_To_BoolModule.ContainsKey(type))
            {
                m_Type_To_BoolModule.Add(type, new BoolModule(state));

                return true;
            }

            return false;
        }
    }
}
