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
