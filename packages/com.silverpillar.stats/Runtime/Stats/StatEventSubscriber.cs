using SilverPillar.Core;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Stats
{
    public class StatEventSubscriber : MonoBehaviour
    {
        [Serializable]
        private class SubscriptionData
        {
            public StatType StatType;

            public StatRatioEvent CurrentToDefaultRatioEvents;
            public StatEvent CurrentStatEvents;
            public StatEvent DefaultStatEvents;
            public StatEvent MinLimitStatEvents;
            public StatEvent MaxLimitStatEvents;

            public void Subscribe(StatController statController, GameObject self)
            {
                if (StatType == null)
                {
                    Debug.LogError(
                        $"StatEventSubscriber in GameObject {self.name}: " +
                        $"There is a NULL {nameof(StatType)}.",
                        self);

                    return;
                }

                if (statController.HasStatType(StatType))
                {
                    statController.SubscribeOnCurrentDefaultRatioChange(
                        StatType,
                        TriggerCurrentDefaultRatioChange);

                    statController.SubscribeOnCurrentStatChange(
                        StatType,
                        TriggerCurrentStatChange);

                    statController.SubscribeOnDefaultStatChange(
                        StatType,
                        TriggerDefaultStatChange);

                    statController.SubscribeOnMinLimitStatChange(
                        StatType,
                        TriggerMinLimitStatChange);

                    statController.SubscribeOnMaxLimitStatChange(
                        StatType,
                        TriggerMaxLimitStatChange);
                }
                else
                {
                    Debug.LogError(
                        $"StatEventSubscriber in GameObject {self.name}: " +
                        $"StatController in GameObject {statController.gameObject.name} " +
                        $"does not have stat type {StatType}.",
                        self);
                }
            }

            public void Unsubscribe(StatController statController, GameObject self)
            {
                if (StatType == null)
                {
                    Debug.LogError(
                        $"StatEventSubscriber in GameObject {self.name}: " +
                        $"There is a NULL {nameof(StatType)}.",
                        self);

                    return;
                }

                if (statController.HasStatType(StatType))
                {
                    statController.UnsubscribeOnCurrentDefaultRatioChange(
                        StatType,
                        TriggerCurrentDefaultRatioChange);

                    statController.UnsubscribeOnCurrentStatChange(
                        StatType,
                        TriggerCurrentStatChange);

                    statController.UnsubscribeOnDefaultStatChange(
                        StatType,
                        TriggerDefaultStatChange);

                    statController.UnsubscribeOnMinLimitStatChange(
                        StatType,
                        TriggerMinLimitStatChange);

                    statController.UnsubscribeOnMaxLimitStatChange(
                        StatType,
                        TriggerMaxLimitStatChange);
                }
                else
                {
                    Debug.LogError(
                        $"StatEventSubscriber in GameObject {self.name}: " +
                        $"StatController in GameObject {statController.gameObject.name} " +
                        $"does not have stat type {StatType}.",
                        self);
                }
            }

            private void TriggerCurrentDefaultRatioChange(float lastValue, float newValue)
            {
                CurrentToDefaultRatioEvents.Trigger(lastValue, newValue);
            }

            private void TriggerCurrentStatChange(float lastValue, float newValue)
            {
                CurrentStatEvents.Trigger(lastValue, newValue);
            }

            private void TriggerDefaultStatChange(float lastValue, float newValue)
            {
                DefaultStatEvents.Trigger(lastValue, newValue);
            }

            private void TriggerMinLimitStatChange(float lastValue, float newValue)
            {
                MinLimitStatEvents.Trigger(lastValue, newValue);
            }

            private void TriggerMaxLimitStatChange(float lastValue, float newValue)
            {
                MaxLimitStatEvents.Trigger(lastValue, newValue);
            }
        }

        private enum ToWhichStatController
        {
            Self,
            Custom,
            PlayerSelf,
            PlayerSelfOrChildren,
            None
        }

        private enum WhenToSubscribe
        {
            DontAutoSubscribe,
            OnStart,
            OnEnable
        }

        private enum WhenToUnsubscribe
        {
            OnDestroy,
            OnDisable
        }

        [Title("Settings")]
        [SerializeField,Tooltip("If set to None, null won't throw.")]
        private ToWhichStatController m_ToWhichStatController;

        [SerializeField, ShowIf(nameof(m_ToWhichStatController), ToWhichStatController.Custom)]
        private StatController m_StatController;

        [SerializeField]
        private WhenToSubscribe m_WhenToSubscribe;

        [SerializeField]
        private WhenToUnsubscribe m_WhenToUnsubscribe;

        [SerializeField, Tooltip("If subscribe is automatically called when changing stat controller")]
        private bool m_SubscribeOnChangeStatController = true;

        [Title("Subscriptions")]
        [SerializeField]
        private List<SubscriptionData> m_Subscriptions;


        /// <summary>
        /// The controller that we actually subscribed to.
        /// This is stored so that Unsubscribe always targets the exact same
        /// controller, even if the configured source changes afterwards.
        /// </summary>
        private StatController m_SubscribedStatController;

        private bool m_IsSubscribed;


        private void Start()
        {
            if (m_WhenToSubscribe == WhenToSubscribe.OnStart)
            {
                Subscribe();
            }
        }

        private void OnEnable()
        {
            if (m_WhenToSubscribe == WhenToSubscribe.OnEnable)
            {
                Subscribe();
            }
        }

        private void OnDisable()
        {
            if (m_WhenToUnsubscribe == WhenToUnsubscribe.OnDisable)
            {
                Unsubscribe();
            }
        }

        private void OnDestroy()
        {
            if (m_WhenToUnsubscribe == WhenToUnsubscribe.OnDestroy)
            {
                Unsubscribe();
            }
        }


        [Title("Actions")]
        [Button]
        public void Subscribe()
        {
            // Prevent accidentally registering the same callbacks twice.
            if (m_IsSubscribed || m_ToWhichStatController == ToWhichStatController.None)
            {
                return;
            }

            StatController statController = GetStatController();

            if (statController == null)
            {
                Debug.LogError(
                    $"StatEventSubscriber in GameObject {gameObject.name}: " +
                    $"Could not find a {nameof(StatController)} using " +
                    $"{m_ToWhichStatController}.",
                    gameObject);

                return;
            }

            if (m_Subscriptions == null)
            {
                Debug.LogError(
                    $"StatEventSubscriber in GameObject {gameObject.name}: " +
                    $"{nameof(m_Subscriptions)} is NULL.",
                    gameObject);

                return;
            }

            for (int i = 0; i < m_Subscriptions.Count; i++)
            {
                SubscriptionData subscription = m_Subscriptions[i];

                if (subscription == null)
                {
                    Debug.LogError(
                        $"StatEventSubscriber in GameObject {gameObject.name}: " +
                        $"Subscription at index {i} is NULL.",
                        gameObject);

                    continue;
                }

                subscription.Subscribe(statController, gameObject);
            }

            m_SubscribedStatController = statController;
            m_IsSubscribed = true;
        }

        [Button]
        public void Unsubscribe()
        {
            if (!m_IsSubscribed || m_ToWhichStatController == ToWhichStatController.None)
            {
                return;
            }

            // Use the controller we originally subscribed to instead of
            // resolving it again.
            StatController statController = m_SubscribedStatController;

            if (statController == null)
            {
                m_SubscribedStatController = null;
                m_IsSubscribed = false;
                return;
            }

            if (m_Subscriptions != null)
            {
                for (int i = 0; i < m_Subscriptions.Count; i++)
                {
                    SubscriptionData subscription = m_Subscriptions[i];

                    if (subscription == null)
                    {
                        continue;
                    }

                    subscription.Unsubscribe(statController, gameObject);
                }
            }

            m_SubscribedStatController = null;
            m_IsSubscribed = false;
        }

        public void ChangeStatController(StatController statController)
        {
            if (m_SubscribeOnChangeStatController)
            {
                Unsubscribe();
            }

            m_ToWhichStatController = ToWhichStatController.Custom;
            m_StatController = statController;

            if (m_SubscribeOnChangeStatController)
            {
                Subscribe();
            }
        }

        public void ChangeStatController(GameObject gameObject)
        {
            if (gameObject.TryGetComponent(out StatController statController))
            {
                ChangeStatController(statController);
            }
        }

        private StatController GetStatController()
        {
            switch (m_ToWhichStatController)
            {
                case ToWhichStatController.Self:
                    return GetComponent<StatController>();

                case ToWhichStatController.Custom:
                    return m_StatController;

                case ToWhichStatController.PlayerSelf:
                    if (Player.Instance == null)
                    {
                        Debug.LogError(
                            $"StatEventSubscriber in GameObject {gameObject.name}: " +
                            $"{nameof(Player)}.Instance is NULL.",
                            gameObject);

                        return null;
                    }

                    return Player.Instance.GetComponent<StatController>();

                case ToWhichStatController.PlayerSelfOrChildren:
                    if (Player.Instance == null)
                    {
                        Debug.LogError(
                            $"StatEventSubscriber in GameObject {gameObject.name}: " +
                            $"{nameof(Player)}.Instance is NULL.",
                            gameObject);

                        return null;
                    }

                    return Player.Instance.GetComponentInChildren<StatController>();

                default:
                    Debug.LogError(
                        $"StatEventSubscriber in GameObject {gameObject.name}: " +
                        $"Unsupported {nameof(ToWhichStatController)} value: " +
                        $"{m_ToWhichStatController}.",
                        gameObject);

                    return null;
            }
        }
    }
}