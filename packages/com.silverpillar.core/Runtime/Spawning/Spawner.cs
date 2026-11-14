using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.Events;

namespace SilverPillar.Core
{
    public class Spawner : SerializedMonoBehaviour
    {
        private enum WhichSpawnPointToRespawnIn
        {
            NearestSpawnPoint,
            RandomSpawnPoint,
            BasedOnConditionStartingFromClosest
        }

        private enum IfConditionNotMet
        {
            ChooseClosest,
            DontRespawn
        }

        private enum WhenToChooseSpawnPoint
        {
            OnlyOnceOnStart,
            OnlyOnceOnRespawn,
            OnEachRespawn
        }

        // Sorted by distance, then registry index for deterministic tie-breaking.
        private readonly struct SpawnPointCandidate : IComparable<SpawnPointCandidate>
        {
            public readonly SpawnPoint Point;
            public readonly float SqrDistance;
            public readonly int Index;

            public SpawnPointCandidate(SpawnPoint point, float sqrDistance, int index)
            {
                Point = point;
                SqrDistance = sqrDistance;
                Index = index;
            }

            public int CompareTo(SpawnPointCandidate other)
            {
                int distanceComparison = SqrDistance.CompareTo(other.SqrDistance);
                return distanceComparison != 0 ? distanceComparison : Index.CompareTo(other.Index);
            }
        }

        [Title("Respawn Settings")]
        [SerializeField, Tooltip("Negative means infinite; zero means no respawns."), Min(-1)]
        private int m_AmountOfTimesToRespawn;
        private int m_CurrentAmountOfTimesRespawned;

        [SerializeField, Min(0)]
        private float m_SecondsToRespawn;

        [Title("Spawn Point")]
        [InfoBox("It won't respawn if there is no SpawnPoint found")]
        [SerializeField]
        private WhichSpawnPointToRespawnIn m_WhichSpawnPointToRespawnIn;
        [OdinSerialize, ShowIf(nameof(m_WhichSpawnPointToRespawnIn), WhichSpawnPointToRespawnIn.BasedOnConditionStartingFromClosest)]
        private IInteractionCondition m_Condition;
        [SerializeField, ShowIf(nameof(m_WhichSpawnPointToRespawnIn), WhichSpawnPointToRespawnIn.BasedOnConditionStartingFromClosest)]
        private IfConditionNotMet m_IfConditionNotMet;
        [SerializeField]
        private WhenToChooseSpawnPoint m_WhenToChooseSpawnPoint;

        [Title("Events")]
        [SerializeField]
        private UnityEvent m_OnRespawn = null;
        [SerializeField]
        private UnityEvent m_OnDespawn = null;

        [SerializeField, Tooltip("Triggered when canceled due to spawn point being disabled or destroyed")]
        private UnityEvent m_OnRespawnCanceledBySpawnPoint = null;

        private SpawnPoint m_ChosenSpawnPoint;
        private SpawnPoint m_PendingSpawnPoint;
        private bool m_HasChosenSpawnPoint;
        private bool m_IsRespawnPending;

        // Reused so repeated respawns don't allocate a temporary candidate list.
        private readonly List<SpawnPointCandidate> m_SortedCandidates = new();

        public int CurrentAmountOfTimesRespawned => m_CurrentAmountOfTimesRespawned;
        public bool CanRespawn => m_AmountOfTimesToRespawn < 0 ||
                                  m_CurrentAmountOfTimesRespawned < m_AmountOfTimesToRespawn;
        public bool IsRespawnPending => m_IsRespawnPending;

        private void Start()
        {
            if (m_WhenToChooseSpawnPoint == WhenToChooseSpawnPoint.OnlyOnceOnStart &&
                !m_HasChosenSpawnPoint)
            {
                ChooseSpawnPoint();
            }
        }

        private void OnDestroy()
        {
            CancelPendingRespawn();
        }

        /// <summary>Despawn without scheduling a respawn.</summary>
        public void Despawn()
        {
            CancelPendingRespawn();
            Deactivate();
        }

        /// <summary>
        /// Despawns immediately; the selected SpawnPoint runs the respawn timer.
        /// </summary>
        public void Respawn()
        {
            if (m_IsRespawnPending)
                return;

            // Protect against reentrant calls from OnDespawn.
            m_IsRespawnPending = true;
            Deactivate();

            if (this == null || !m_IsRespawnPending)
                return;

            if (!CanRespawn)
            {
                m_IsRespawnPending = false;
                return;
            }

            // The point must be chosen now because it owns the coroutine.
            // OnEachRespawn chooses once per request, not once per component lifetime.
            if (m_WhenToChooseSpawnPoint == WhenToChooseSpawnPoint.OnEachRespawn ||
                !m_HasChosenSpawnPoint)
            {
                ChooseSpawnPoint();
            }

            if (!IsChosenSpawnPointAvailable())
            {
                m_IsRespawnPending = false;
                return;
            }

            m_PendingSpawnPoint = m_ChosenSpawnPoint;
            if (!m_PendingSpawnPoint.ScheduleRespawn(this, Mathf.Max(0f, m_SecondsToRespawn)))
            {
                m_PendingSpawnPoint = null;
                m_IsRespawnPending = false;
            }
        }

        /// <summary>Called by the SpawnPoint after its respawn delay.</summary>
        internal bool CompleteRespawn(SpawnPoint spawnPoint)
        {
            if (!m_IsRespawnPending || m_PendingSpawnPoint != spawnPoint)
                return false;

            m_IsRespawnPending = false;
            m_PendingSpawnPoint = null;

            if (!CanRespawn || spawnPoint == null || !spawnPoint.IsAvailableForRespawn)
                return false;

            return TrySpawnAt(spawnPoint);
        }

        /// <summary>Called for a direct SpawnPoint.Respawn operation too.</summary>
        private bool TrySpawnAt(SpawnPoint spawnPoint)
        {
            if (spawnPoint == null || !spawnPoint.IsAvailableForRespawn || !CanRespawn)
                return false;

            CancelPendingRespawn();

            transform.SetPositionAndRotation(spawnPoint.transform.position,
                                             spawnPoint.transform.rotation);

            // Set count before OnEnable and UnityEvent callbacks.
            m_CurrentAmountOfTimesRespawned++;
            gameObject.SetActive(true);
            m_OnRespawn?.Invoke();
            return true;
        }

        /// <summary>
        /// Called when the SpawnPoint hosting our timer gets disabled/destroyed.
        /// </summary>
        internal void NotifyRespawnCancelled(SpawnPoint spawnPoint)
        {
            if (m_PendingSpawnPoint != spawnPoint)
                return;

            m_PendingSpawnPoint = null;
            m_IsRespawnPending = false;
            m_OnRespawnCanceledBySpawnPoint?.Invoke();
        }

        private void Deactivate()
        {
            if (!gameObject.activeSelf)
                return;

            m_OnDespawn?.Invoke();
            gameObject.SetActive(false);
        }

        private void CancelPendingRespawn()
        {
            SpawnPoint previousPoint = m_PendingSpawnPoint;
            m_PendingSpawnPoint = null;
            m_IsRespawnPending = false;

            if (previousPoint != null)
                previousPoint.CancelScheduledRespawn(this);
        }

        private bool IsChosenSpawnPointAvailable()
        {
            return m_ChosenSpawnPoint != null && m_ChosenSpawnPoint.IsAvailableForRespawn;
        }

        private void ChooseSpawnPoint()
        {
            m_HasChosenSpawnPoint = true;
            m_ChosenSpawnPoint = null;

            switch (m_WhichSpawnPointToRespawnIn)
            {
                case WhichSpawnPointToRespawnIn.NearestSpawnPoint:
                    ChooseNearestSpawnPoint();
                    break;
                case WhichSpawnPointToRespawnIn.RandomSpawnPoint:
                    ChooseRandomSpawnPoint();
                    break;
                case WhichSpawnPointToRespawnIn.BasedOnConditionStartingFromClosest:
                    ChooseSpawnPointBasedOnConditionStartingFromClosest();
                    break;
            }
        }

        private void ChooseNearestSpawnPoint()
        {
            float closestSqrDistance = float.PositiveInfinity;
            Vector3 currentPosition = transform.position;
            var spawnPoints = SpawnPoint.SpawnPoints;

            for (int i = 0; i < spawnPoints.Count; i++)
            {
                SpawnPoint point = spawnPoints[i];
                if (point == null || !point.IsAvailableForRespawn)
                    continue;

                float sqrDistance = (point.transform.position - currentPosition).sqrMagnitude;
                if (sqrDistance < closestSqrDistance)
                {
                    closestSqrDistance = sqrDistance;
                    m_ChosenSpawnPoint = point;
                }
            }
        }

        private void ChooseRandomSpawnPoint()
        {
            int candidateCount = 0;
            var spawnPoints = SpawnPoint.SpawnPoints;

            for (int i = 0; i < spawnPoints.Count; i++)
            {
                SpawnPoint point = spawnPoints[i];
                if (point == null || !point.IsAvailableForRespawn)
                    continue;

                candidateCount++;
                if (UnityEngine.Random.Range(0, candidateCount) == 0)
                    m_ChosenSpawnPoint = point;
            }
        }

        private void ChooseSpawnPointBasedOnConditionStartingFromClosest()
        {
            var spawnPoints = SpawnPoint.SpawnPoints;
            Vector3 currentPosition = transform.position;

            m_SortedCandidates.Clear();
            for (int i = 0; i < spawnPoints.Count; i++)
            {
                SpawnPoint point = spawnPoints[i];
                if (point == null || !point.IsAvailableForRespawn)
                    continue;

                float sqrDistance = (point.transform.position - currentPosition).sqrMagnitude;
                m_SortedCandidates.Add(new SpawnPointCandidate(point, sqrDistance, i));
            }

            m_SortedCandidates.Sort();

            // The spawner is 'self'; each spawn point is the condition's 'target'.
            if (m_Condition != null && m_Condition.SetGameObject(gameObject))
            {
                for (int i = 0; i < m_SortedCandidates.Count; i++)
                {
                    SpawnPoint point = m_SortedCandidates[i].Point;
                    if (point == null || !point.IsAvailableForRespawn)
                        continue;

                    if (m_Condition.IsFulfilled(point.gameObject))
                    {
                        m_ChosenSpawnPoint = point;
                        return;
                    }
                }
            }

            // No candidate met the condition (or there was no usable condition).
            if (m_IfConditionNotMet == IfConditionNotMet.ChooseClosest)
                ChooseNearestSpawnPoint();
            // Otherwise m_ChosenSpawnPoint remains null, and respawning is skipped.
        }
    }
}
