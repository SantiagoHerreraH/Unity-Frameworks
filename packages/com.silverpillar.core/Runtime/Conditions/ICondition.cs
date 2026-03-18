using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Core
{
    public interface ICondition
    {
        public bool IsFulfilled(GameObject gameObj);
    }

    
}

