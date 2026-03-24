using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SilverPillar.Core
{
#nullable enable
    public interface ICachedInteractionScore
    {
        public ICachedInteractionScore Clone();
        public GameObject? GetGameObject();
        public bool SetGameObject(GameObject self);
        public float CalculateScore(GameObject target);
    }
}

