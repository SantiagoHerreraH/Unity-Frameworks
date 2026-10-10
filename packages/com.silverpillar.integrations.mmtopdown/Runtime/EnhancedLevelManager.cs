using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SilverPillar.Integrations.MMTopDown
{
    public class EnhancedLevelManager : LevelManager
    {
        [Header("Before Player Spawning")]
        [SerializeField]
        private List<GameObject> m_PrefabsToSpawnBeforePlayerSpawning;

        [SerializeField]
        private UnityEvent m_OnBeforePlayerSpawn;


        [Header("After Player Spawning")]
        [SerializeField]
        private List<GameObject> m_PrefabsToSpawnAfterPlayerSpawning;

        [SerializeField]
        private UnityEvent m_OnAfterPlayerSpawn;


        /// <summary>
        /// Initializes the level while adding custom logic immediately
        /// before and after the player is spawned.
        /// </summary>
        protected override IEnumerator InitializationCoroutine()
        {
            if (SpawnDelay > 0f)
            {
                yield return MMCoroutine.WaitFor(SpawnDelay);
            }

            BoundsCollider = _collider;
            BoundsCollider2D = _collider2D;


            // ---------------------------------------------------------
            // Before player spawning
            // ---------------------------------------------------------

            SpawnPrefabs(m_PrefabsToSpawnBeforePlayerSpawning);
            m_OnBeforePlayerSpawn?.Invoke();


            // Instantiate/retrieve the playable characters
            InstantiatePlayableCharacters();


            if (UseLevelBounds)
            {
                MMCameraEvent.Trigger(
                    MMCameraEventTypes.SetConfiner,
                    null,
                    BoundsCollider,
                    BoundsCollider2D);
            }

            if (Players == null || Players.Count == 0)
            {
                yield break;
            }

            Initialization();

            TopDownEngineEvent.Trigger(
                TopDownEngineEventTypes.SpawnCharacterStarts,
                null);


            // ---------------------------------------------------------
            // Player spawning
            // ---------------------------------------------------------

            if (Players.Count == 1)
            {
                SpawnSingleCharacter();
            }
            else
            {
                SpawnMultipleCharacters();
            }


            // ---------------------------------------------------------
            // After player spawning
            // ---------------------------------------------------------

            SpawnPrefabs(m_PrefabsToSpawnAfterPlayerSpawning);
            m_OnAfterPlayerSpawn?.Invoke();


            // ---------------------------------------------------------
            // Original LevelManager initialization
            // ---------------------------------------------------------

            CheckpointAssignment();

            MMFadeOutEvent.Trigger(
                IntroFadeDuration,
                FadeCurve,
                FaderID);

            TopDownEngineEvent.Trigger(
                TopDownEngineEventTypes.LevelStart,
                null);

            MMGameEvent.Trigger("Load");

            if (SetPlayerAsFeedbackRangeCenter)
            {
                MMSetFeedbackRangeCenterEvent.Trigger(
                    Players[0].transform);
            }

            MMCameraEvent.Trigger(
                MMCameraEventTypes.SetTargetCharacter,
                Players[0]);

            MMCameraEvent.Trigger(
                MMCameraEventTypes.StartFollowing);

            MMGameEvent.Trigger("CameraBound");
        }


        /// <summary>
        /// Instantiates all valid prefabs contained in the specified list.
        /// </summary>
        protected virtual void SpawnPrefabs(List<GameObject> prefabs)
        {
            if (prefabs == null)
            {
                return;
            }

            foreach (GameObject prefab in prefabs)
            {
                if (prefab == null)
                {
                    continue;
                }

                Instantiate(prefab);
            }
        }
    }
}