
using SilverPillar.Core;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace SilverPillar.Modules
{
    public class BoolModuleController : MonoBehaviour
    {
        [Serializable]
        public struct Data
        {
            public BoolModuleType BoolModuleType;
            public BoolModule BoolModule;
        }
        [Button(ButtonSizes.Small)]
        private void PopulateWithAllBoolModuleTypes()
        {
            if (m_BoolModuleData == null)
            {
                m_BoolModuleData = new List<Data>();
            }

            m_BoolModuleData = m_BoolModuleData
            .Where(x => x.BoolModuleType != null)
            .GroupBy(x => x.BoolModuleType)
            .Select(g => g.First())
            .ToList();

            var currentStatTypes = m_BoolModuleData.Select(x => x.BoolModuleType).ToHashSet();

            var allBoolModuleTypes = ScriptableObjectRegistry.Instance.GetAllOfType<BoolModuleType>();

            foreach (var boolModuleType in allBoolModuleTypes)
            {
                if (boolModuleType != null && !currentStatTypes.Contains(boolModuleType))
                {
                    m_BoolModuleData.Add(new Data { BoolModuleType = boolModuleType });
                }
            }
        }

        [Button(ButtonSizes.Small)]
        private void EnforceModuleDataValidity()
        {
            if (m_BoolModuleData == null || m_BoolModuleData.Count == 0) return;

            m_BoolModuleData = m_BoolModuleData
                .Where(x => x.BoolModuleType != null)
                .GroupBy(x => x.BoolModuleType)
                .Select(g => g.First())
                .ToList();
        }
        [SerializeField]
        private List<Data> m_BoolModuleData;
        

        private bool m_Initialized = false;
        private void Initialize()
        {
            if (m_Initialized)
            {
                return;
            }
            EnforceModuleDataValidity();

            m_Type_To_BoolModule ??= new();

            for (int i = 0; i < m_BoolModuleData.Count; i++)
            {
                m_Type_To_BoolModule.Add(m_BoolModuleData[i].BoolModuleType, m_BoolModuleData[i].BoolModule);
            }

            m_Initialized = true;
        }

        private Dictionary<BoolModuleType, BoolModule> m_Type_To_BoolModule = new();

        private void Awake()
        {
            Initialize();
        }

        public bool HasBoolModule(BoolModuleType type)
        {
            Initialize();
            return m_Type_To_BoolModule.ContainsKey(type);
        }

#nullable enable
        public BoolModule? GetBoolModule(BoolModuleType type)
        {
            Initialize();
            if (m_Type_To_BoolModule.ContainsKey(type))
            {
                return m_Type_To_BoolModule[type];
            }
            return null;
        }
        public bool GetState(BoolModuleType type)
        {
            Initialize();
            if (m_Type_To_BoolModule.ContainsKey(type))
            {
                return m_Type_To_BoolModule[type].GetState();
            }
            return false;
        }

        public bool SetState(BoolModuleType type, bool state)
        {
            Initialize();
            if (m_Type_To_BoolModule.ContainsKey(type))
            {
                m_Type_To_BoolModule[type].SetState(state);
                return true;
            }
            return false;
        }


        public bool CreateBoolModule(BoolModuleType type, bool state)
        {
            Initialize();
            if (!m_Type_To_BoolModule.ContainsKey(type))
            {
                m_Type_To_BoolModule.Add(type, new BoolModule(state));

                return true;
            }

            return false;
        }

        #region Subscribe and Unsubscribe

        public void SubscribeOnChangeState(BoolModuleType type, UnityAction<bool> action)
        {
            Initialize();
            var boolModule = GetBoolModule(type);
            if (boolModule != null)
            {
                boolModule.SubscribeOnChangeState(action);
            }
        }

        public void UnsubscribeOnChangeState(BoolModuleType type, UnityAction<bool> action)
        {
            Initialize();
            var boolModule = GetBoolModule(type);
            if (boolModule != null)
            {
                boolModule.UnsubscribeOnChangeState(action);
            }
        }
        public void SubscribeOnSetState(BoolModuleType type, UnityAction<bool> action)
        {
            Initialize();
            var boolModule = GetBoolModule(type);
            if (boolModule != null)
            {
                boolModule.SubscribeOnSetState(action);
            }
        }

        public void UnsubscribeOnSetState(BoolModuleType type, UnityAction<bool> action)
        {
            Initialize();
            var boolModule = GetBoolModule(type);
            if (boolModule != null)
            {
                boolModule.UnsubscribeOnSetState(action);
            }
        }

        #endregion
    }
}
