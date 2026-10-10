using Sirenix.OdinInspector;
using UnityEngine;

namespace SilverPillar.Core
{
    public class IsTimerFinished_CachedCondition : ICachedCondition
    {
        [SerializeField]
        private SelfType m_WhichTimerController;

        [SerializeField, ShowIf(nameof(m_WhichTimerController), SelfType.CustomGameObject)]
        private TimerController m_TimerController;

        [SerializeField]
        private TimerName m_TimerName;


        private GameObject m_Self;


        public ICachedCondition Clone()
        {
            return new IsTimerFinished_CachedCondition
            {
                m_WhichTimerController = m_WhichTimerController,
                m_TimerController = m_TimerController,
                m_TimerName = m_TimerName
            };
        }


        public GameObject GetGameObject()
        {
            return m_Self;
        }


        public bool IsFulfilled()
        {
            if (m_TimerController == null)
            {
                Debug.LogError(
                    $"{nameof(IsTimerFinished_CachedCondition)}: " +
                    $"{nameof(m_TimerController)} is NULL" +
                    (m_Self != null ? $" in {m_Self.name}." : "."));

                return false;
            }

            if (m_TimerName == null)
            {
                Debug.LogError(
                    $"{nameof(IsTimerFinished_CachedCondition)}: " +
                    $"{nameof(m_TimerName)} is NULL" +
                    (m_Self != null ? $" in {m_Self.name}." : "."));

                return false;
            }

            return m_TimerController.IsFinished(m_TimerName);
        }


        public bool SetGameObject(GameObject gameObj)
        {
            m_Self = gameObj;

            if (m_Self == null)
            {
                Debug.LogError(
                    $"{nameof(IsTimerFinished_CachedCondition)}: " +
                    $"Cannot set a NULL {nameof(GameObject)}.");

                return false;
            }

            if (m_WhichTimerController == SelfType.ThisGameObject)
            {
                if (!m_Self.TryGetComponent(out m_TimerController))
                {
                    Debug.LogError(
                        $"{nameof(IsTimerFinished_CachedCondition)} in {m_Self.name}: " +
                        $"Could not find a {nameof(TimerController)} component.");

                    return false;
                }
            }
            else
            {
                if (m_TimerController == null)
                {
                    Debug.LogError(
                        $"{nameof(IsTimerFinished_CachedCondition)} in {m_Self.name}: " +
                        $"{nameof(m_TimerController)} is NULL.");

                    return false;
                }
            }

            if (m_TimerName == null)
            {
                Debug.LogError(
                    $"{nameof(IsTimerFinished_CachedCondition)} in {m_Self.name}: " +
                    $"{nameof(m_TimerName)} is NULL.");

                return false;
            }

            return true;
        }
    }
}