using System;
using UnityEngine;

namespace SilverPillar.Modules
{
    [Serializable]
    public class BoolModuleData<T>
    {
        public enum WhatToDoIfDoesntHaveModuleType
        {
            ReturnNull,
            CreateOneAndReturnTrue,
            CreateOneAndReturnFalse,
            ReturnTrue,
            ReturnFalse
        }

        [SerializeField]
        private BoolModuleType m_BoolModuleType;
        [SerializeField]
        private T m_ValueOnTrue;
        [SerializeField]
        private T m_ValueOnFalse;
        [SerializeField]
        private WhatToDoIfDoesntHaveModuleType m_WhatToDoIfDoesntHaveModuleType;

#nullable enable
        public T? GetData(BoolModuleController controller)
        {
            if (!controller.HasBoolModule(m_BoolModuleType))
            {
                switch (m_WhatToDoIfDoesntHaveModuleType)
                {
                    case WhatToDoIfDoesntHaveModuleType.CreateOneAndReturnTrue:
                        controller.CreateBoolModule(m_BoolModuleType, true);
                        return m_ValueOnTrue;

                    case WhatToDoIfDoesntHaveModuleType.CreateOneAndReturnFalse:
                        controller.CreateBoolModule(m_BoolModuleType, false);
                        return m_ValueOnFalse;

                    case WhatToDoIfDoesntHaveModuleType.ReturnTrue:
                        return m_ValueOnTrue;

                    case WhatToDoIfDoesntHaveModuleType.ReturnFalse:
                        return m_ValueOnFalse;

                    case WhatToDoIfDoesntHaveModuleType.ReturnNull:
                    default:
                        return default;
                }
            }

            bool state = controller.GetState(m_BoolModuleType);
            return state ? m_ValueOnTrue : m_ValueOnFalse;
        }
    }
}
