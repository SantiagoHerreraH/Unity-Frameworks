using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SilverPillar.Core
{
    public class SpawnPoint : MonoBehaviour
    {
        public enum WhenToRegisterSpawnPoint
        {
            OnAwake,
            OnEnable
        }

        public enum WhenToDeregisterSpawnPoint
        {
            OnDisable,
            OnDestroy
        }

        private static readonly List<SpawnPoint> m_SpawnPoints = new();
        public static IReadOnlyList<SpawnPoint> SpawnPoints => m_SpawnPoints;

        [Title("Registration Settings")]
        [SerializeField]
        private WhenToRegisterSpawnPoint m_WhenToRegisterSpawnPoint;
        [SerializeField]
        private WhenToDeregisterSpawnPoint m_WhenToDeregisterSpawnPoint;

        [Title("Events")]
        public UnityEvent<GameObject> m_OnRespawn = new();

        // The actual spawn point runs the timer. No separate runner GameObject is needed.
        private readonly Dictionary<Spawner, Coroutine> m_PendingRespawns = new();
        private bool m_IsRegistered;

        public bool IsRegistered => m_IsRegistered;
        public bool IsAvailableForRespawn => m_IsRegistered && isActiveAndEnabled;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry()
        {
            m_SpawnPoints.Clear();
        }

        private void Awake()
        {
            if (m_WhenToRegisterSpawnPoint == WhenToRegisterSpawnPoint.OnAwake)
                Register();
        }

        private void OnEnable()
        {
            // A spawn point registered in Awake may have deregistered on disable.
            if (m_WhenToRegisterSpawnPoint == WhenToRegisterSpawnPoint.OnEnable ||
                m_WhenToDeregisterSpawnPoint == WhenToDeregisterSpawnPoint.OnDisable)
            {
                Register();
            }
        }

        private void OnDisable()
        {
            // Unity stops coroutines when this GameObject is deactivated.
            // Explicitly cancel and notify spawners so none stays permanently pending.
            CancelAllScheduledRespawns();

            if (m_WhenToDeregisterSpawnPoint == WhenToDeregisterSpawnPoint.OnDisable)
                Deregister();
        }

        private void OnDestroy()
        {
            CancelAllScheduledRespawns();
            Deregister();
        }

        private void Register()
        {
            if (!m_SpawnPoints.Contains(this))
                m_SpawnPoints.Add(this);

            m_IsRegistered = true;
        }

        private void Deregister()
        {
            m_SpawnPoints.Remove(this);
            m_IsRegistered = false;
        }

        /// <summary>
        /// Begins a delayed respawn on this SpawnPoint, rather than on the
        /// Spawner's GameObject (which will be deactivated during the delay).
        /// </summary>
        internal bool ScheduleRespawn(Spawner spawner, float seconds)
        {
            if (spawner == null || !IsAvailableForRespawn)
                return false;

            CancelScheduledRespawn(spawner);

            if (seconds <= 0f)
            {
                CompleteScheduledRespawn(spawner);
                return true;
            }

            m_PendingRespawns[spawner] = StartCoroutine(
                WaitAndRespawn(spawner, seconds));
            return true;
        }

        internal void CancelScheduledRespawn(Spawner spawner)
        {
            if (ReferenceEquals(spawner, null) ||
                !m_PendingRespawns.TryGetValue(spawner, out Coroutine coroutine))
            {
                return;
            }

            StopCoroutine(coroutine);
            m_PendingRespawns.Remove(spawner);
        }

        private void CancelAllScheduledRespawns()
        {
            foreach (KeyValuePair<Spawner, Coroutine> pending in m_PendingRespawns)
            {
                if (pending.Value != null)
                    StopCoroutine(pending.Value);

                if (pending.Key != null)
                    pending.Key.NotifyRespawnCancelled(this);
            }

            m_PendingRespawns.Clear();
        }

        private IEnumerator WaitAndRespawn(Spawner spawner, float seconds)
        {
            yield return new WaitForSeconds(seconds);

            // Remove before invoking events; callbacks can request a new respawn.
            m_PendingRespawns.Remove(spawner);
            CompleteScheduledRespawn(spawner);
        }

        private void CompleteScheduledRespawn(Spawner spawner)
        {
            if (spawner != null && spawner.CompleteRespawn(this))
                m_OnRespawn?.Invoke(spawner.gameObject);
        }
    }
}
