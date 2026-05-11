using System;
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

        #region Subscribe and Unsubscribe

        public void SubscribeOnChangeState(BoolModuleType type, Action<bool> action)
        {
            var boolModule = GetBoolModule(type);
            if (boolModule != null)
            {
                boolModule.SubscribeOnChangeState(action);
            }
        }

        public void UnsubscribeOnChangeState(BoolModuleType type, Action<bool> action)
        {
            var boolModule = GetBoolModule(type);
            if (boolModule != null)
            {
                boolModule.UnsubscribeOnChangeState(action);
            }
        }
        public void SubscribeOnSetState(BoolModuleType type, Action<bool> action)
        {
            var boolModule = GetBoolModule(type);
            if (boolModule != null)
            {
                boolModule.SubscribeOnSetState(action);
            }
        }

        public void UnsubscribeOnSetState(BoolModuleType type, Action<bool> action)
        {
            var boolModule = GetBoolModule(type);
            if (boolModule != null)
            {
                boolModule.UnsubscribeOnSetState(action);
            }
        }

        #endregion
    }
}
