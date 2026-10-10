using System;
using UnityEngine;

namespace SilverPillar.Senses
{
    [Serializable]
    public abstract class SenseTargetQuality
    {
        [SerializeField]
        protected bool m_CanBeSensed = true;

        public void SetCanBeSensed(bool canBeSensed)
        {
            m_CanBeSensed = canBeSensed;
        }
        public bool CanBeSensed()
        {
            return m_CanBeSensed;
        }

        public abstract void CheckSelfValidity(GameObject self);
    }
}