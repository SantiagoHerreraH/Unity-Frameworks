using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class ChoosingOption<T>
    {
        [OdinSerialize, ShowInInspector]
        public T Value;

        public virtual bool Initialize(GameObject gameObj)
        {
            return true;
        }

        public virtual ChoosingOption<T> Clone()
        {
            return new ChoosingOption<T> { Value = Clone(Value) };
        }

        protected virtual  T Clone(T value)
        {
            return value;
        }
    }
}
