using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Target
{
    public class PossibleTarget : MonoBehaviour
    {
        public enum WhenToRegisterPossibleTarget
        {
            OnAwake,
            OnEnable
        }

        public enum WhenToUnregisterPossibleTarget
        {
            OnDestroy,
            OnDisable
        }

        [SerializeField]
        private WhenToRegisterPossibleTarget m_WhenToRegisterPossibleTarget = WhenToRegisterPossibleTarget.OnAwake;

        [SerializeField]
        private WhenToUnregisterPossibleTarget m_WhenToUnregisterPossibleTarget = WhenToUnregisterPossibleTarget.OnDestroy;

        public HashSet<TargetSystem> TargetSystemsThatChoseThisAsTarget { get; private set; } = new();

        public bool RegisterTargetSystem(TargetSystem targetSystem)
        {
            if (TargetSystemsThatChoseThisAsTarget == null)
            {
                TargetSystemsThatChoseThisAsTarget = new();
            }
            return TargetSystemsThatChoseThisAsTarget.Add(targetSystem);
        }

        public bool UnregisterTargetSystem(TargetSystem targetSystem)
        {
            if (TargetSystemsThatChoseThisAsTarget == null)
            {
                TargetSystemsThatChoseThisAsTarget = new();
            }
            return TargetSystemsThatChoseThisAsTarget.Remove(targetSystem); 
        }

        public bool HasTargetSystem(TargetSystem targetSystem)
        {
            return TargetSystemsThatChoseThisAsTarget.Contains(targetSystem);
        }

        private void Awake()
        {
            if (m_WhenToRegisterPossibleTarget == WhenToRegisterPossibleTarget.OnAwake)
            {
                TargetSystem.RegisterStaticPossibleTarget(this);
            }
        }

        private void OnEnable()
        {
            if (m_WhenToRegisterPossibleTarget == WhenToRegisterPossibleTarget.OnEnable)
            {
                TargetSystem.RegisterStaticPossibleTarget(this);
            }
        }

        private void OnDisable()
        {
            if (m_WhenToUnregisterPossibleTarget == WhenToUnregisterPossibleTarget.OnDisable)
            {
                TargetSystem.UnregisterStaticPossibleTarget(this);
            }
        }

        private void OnDestroy()
        {
            if (m_WhenToUnregisterPossibleTarget == WhenToUnregisterPossibleTarget.OnDestroy)
            {
                TargetSystem.UnregisterStaticPossibleTarget(this);
            }
        }
    }
}
