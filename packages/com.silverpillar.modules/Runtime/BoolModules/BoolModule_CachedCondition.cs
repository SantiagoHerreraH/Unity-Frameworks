using SilverPillar.Core;
using Sirenix.OdinInspector;
using UnityEngine;

namespace SilverPillar.Modules
{
    public class BoolModule_CachedCondition : ICachedCondition
    {
        [SerializeField]
        private SelfType m_FromWhoToGetController;
        [SerializeField, ShowIf(nameof(m_FromWhoToGetController), SelfType.CustomGameObject)]
        private BoolModuleController m_Controller;
        [SerializeField]
        private BoolModuleType m_ModuleType;

        private GameObject m_Self;

        public BoolModule_CachedCondition(){}

        public BoolModule_CachedCondition(BoolModule_CachedCondition other)
        {
            m_FromWhoToGetController = other.m_FromWhoToGetController;
            m_Controller = other.m_Controller;
            m_ModuleType = other.m_ModuleType;
            m_Self = other.m_Self;
        }

        public ICachedCondition Clone()
        {
            throw new System.NotImplementedException();
        }

        public GameObject GetGameObject()
        {
            return m_Self;
        }

        public bool IsFulfilled()
        {
            if (m_Controller == null || m_ModuleType == null || !m_Controller.HasBoolModule(m_ModuleType))
            {
                return false;
            }

            var module = m_Controller.GetBoolModule(m_ModuleType);

            return module.GetState();
        }

        public bool SetGameObject(GameObject gameObj)
        {
            bool allGood = true;
            m_Self = gameObj;
            allGood &= m_Self != null;
            if (m_FromWhoToGetController == SelfType.ThisGameObject)
            {
                gameObj.TryGetComponent(out m_Controller);
            }
            allGood &= m_Controller != null;

            return allGood;
        }
    }
}
