using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Sirenix.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SilverPillar.Core
{
    [GlobalConfig("Assets/Resources")]
    public class ScriptableObjectRegistry : GlobalConfig<ScriptableObjectRegistry>, ISerializationCallbackReceiver, ISupportsPrefabSerialization
    {
        #region Odin Serialization Persistence

        [SerializeField, HideInInspector]
        private SerializationData serializationData;

        SerializationData ISupportsPrefabSerialization.SerializationData
        {
            get => this.serializationData;
            set => this.serializationData = value;
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            UnitySerializationUtility.DeserializeUnityObject(this, ref this.serializationData);
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            UnitySerializationUtility.SerializeUnityObject(this, ref this.serializationData);
        }

        #endregion

        [ShowInInspector, OdinSerialize, ReadOnly]
        private Dictionary<string, Dictionary<string, SaveableScriptableObject>> m_Container = new();

        // Unity-serialized keepalive list to force build inclusion of all saveable SOs.
        [SerializeField, HideInInspector]
        private List<SaveableScriptableObject> m_AllSaveableScriptableObjects = new();

        [SerializeField, ReadOnly]
        private bool m_IsBaked;

        private static string TypeKey(Type type) => type.AssemblyQualifiedName;

        // -------------------------
        // Runtime API
        // -------------------------

        public List<T> GetAllOfType<T>() where T : SaveableScriptableObject
        {
            var key = TypeKey(typeof(T));

            if (m_Container != null &&
                m_Container.TryGetValue(key, out var dict) &&
                dict != null)
            {
                return dict.Values
                    .Where(x => x != null)
                    .Cast<T>()
                    .ToList();
            }

            return new List<T>();
        }

        public T Get<T>(string guid) where T : SaveableScriptableObject
        {
            if (string.IsNullOrEmpty(guid))
                return null;

            var key = TypeKey(typeof(T));

            if (m_Container != null &&
                m_Container.TryGetValue(key, out var dict) &&
                dict != null &&
                dict.TryGetValue(guid, out var obj))
            {
                return obj as T;
            }

            return null;
        }

        public SaveableScriptableObject Get(string guid)
        {
            if (string.IsNullOrEmpty(guid) || m_Container == null)
                return null;

            foreach (var bucket in m_Container.Values)
            {
                if (bucket == null)
                    continue;

                if (bucket.TryGetValue(guid, out var asset) && asset != null)
                    return asset;
            }

            return null;
        }

        public IEnumerable<SaveableScriptableObject> GetAll()
        {
            if (m_Container == null)
                return Enumerable.Empty<SaveableScriptableObject>();

            return m_Container.Values
                .Where(x => x != null)
                .SelectMany(x => x.Values)
                .Where(x => x != null)
                .Distinct();
        }

#if UNITY_EDITOR
        // -------------------------
        // Editor-only helpers
        // -------------------------

        /// <summary>
        /// Ensures the asset has a non-empty GUID and that it is unique.
        /// If a collision is found, a new GUID is assigned until uniqueness is achieved.
        /// reservedGuids is used during full rebuilds so uniqueness does not depend on
        /// partially-built registry state.
        /// </summary>
        private void EnsureValidUniqueGuidEditorOnly(
            SaveableScriptableObject asset,
            HashSet<string> reservedGuids = null)
        {
            if (asset == null)
                return;

            while (true)
            {
                if (string.IsNullOrEmpty(asset.Guid))
                {
                    Debug.LogWarning(
                        $"[{nameof(ScriptableObjectRegistry)}] '{asset.name}' had an empty Guid. Generating a new one.",
                        asset
                    );

                    asset.RegenerateGuidEditorOnly();
                }

                var guid = asset.Guid;
                bool collidesWithReserved = reservedGuids != null && reservedGuids.Contains(guid);
                bool collidesWithRegistry = false;

                if (!collidesWithReserved)
                {
                    var existing = Get(guid);
                    collidesWithRegistry = existing != null && existing != asset;
                }

                if (!collidesWithReserved && !collidesWithRegistry)
                    return;

                Debug.LogWarning(
                    $"[{nameof(ScriptableObjectRegistry)}] Duplicate Guid '{guid}' detected on '{asset.name}'. Generating a new Guid.",
                    asset
                );

                asset.RegenerateGuidEditorOnly();
            }
        }

        /// <summary>
        /// Removes every stale reference to this asset from all buckets.
        /// This fixes the case where the asset changed Guid and was still present under the old Guid.
        /// It also prevents the same asset from existing twice in the registry.
        /// </summary>
        private void RemoveAssetFromAllBuckets(SaveableScriptableObject asset)
        {
            if (asset == null || m_Container == null)
                return;

            var outerKeysToRemove = new List<string>();

            foreach (var outerPair in m_Container)
            {
                var innerDict = outerPair.Value;
                if (innerDict == null)
                {
                    outerKeysToRemove.Add(outerPair.Key);
                    continue;
                }

                var guidsToRemove = new List<string>();

                foreach (var innerPair in innerDict)
                {
                    if (innerPair.Value == null || innerPair.Value == asset)
                        guidsToRemove.Add(innerPair.Key);
                }

                foreach (var guid in guidsToRemove)
                    innerDict.Remove(guid);

                if (innerDict.Count == 0)
                    outerKeysToRemove.Add(outerPair.Key);
            }

            foreach (var key in outerKeysToRemove)
                m_Container.Remove(key);
        }

        /// <summary>
        /// Removes null entries and duplicate asset references from the keepalive list.
        /// </summary>
        private void CleanupKeepAliveList()
        {
            if (m_AllSaveableScriptableObjects == null)
            {
                m_AllSaveableScriptableObjects = new List<SaveableScriptableObject>();
                return;
            }

            var seen = new HashSet<SaveableScriptableObject>();
            var cleaned = new List<SaveableScriptableObject>(m_AllSaveableScriptableObjects.Count);

            foreach (var asset in m_AllSaveableScriptableObjects)
            {
                if (asset == null)
                    continue;

                if (seen.Add(asset))
                    cleaned.Add(asset);
            }

            m_AllSaveableScriptableObjects = cleaned;
        }

        /// <summary>
        /// Adds or updates a single SaveableScriptableObject in the registry.
        /// Safe to call from OnValidate().
        /// </summary>
        public void RegisterOrUpdateEditorOnly(SaveableScriptableObject asset)
        {
            if (asset == null)
                return;

            if (!EditorUtility.IsPersistent(asset))
                return;

            m_Container ??= new Dictionary<string, Dictionary<string, SaveableScriptableObject>>();
            m_AllSaveableScriptableObjects ??= new List<SaveableScriptableObject>();

            EnsureValidUniqueGuidEditorOnly(asset);

            var guid = asset.Guid;
            if (string.IsNullOrEmpty(guid))
                return;

            // Remove stale references first.
            RemoveAssetFromAllBuckets(asset);

            var typeKey = TypeKey(asset.GetType());

            if (!m_Container.TryGetValue(typeKey, out var innerDict) || innerDict == null)
            {
                innerDict = new Dictionary<string, SaveableScriptableObject>();
                m_Container[typeKey] = innerDict;
            }

            innerDict[guid] = asset;

            if (!m_AllSaveableScriptableObjects.Contains(asset))
                m_AllSaveableScriptableObjects.Add(asset);

            CleanupKeepAliveList();

            m_IsBaked = true;

            EditorUtility.SetDirty(this);
            EditorSaveScheduler.QueueSaveAssetsOnce();
        }

        /// <summary>
        /// Optional helper if you want to explicitly unregister an asset.
        /// </summary>
        public void UnregisterEditorOnly(SaveableScriptableObject asset)
        {
            if (asset == null)
                return;

            RemoveAssetFromAllBuckets(asset);

            if (m_AllSaveableScriptableObjects != null)
                m_AllSaveableScriptableObjects.RemoveAll(x => x == null || x == asset);

            m_IsBaked = true;

            EditorUtility.SetDirty(this);
            EditorSaveScheduler.QueueSaveAssetsOnce();
        }

        /// <summary>
        /// Full rebuild by scanning the project (editor-only).
        /// Guarantees valid GUIDs, unique GUIDs, and no duplicate asset instances in the final registry.
        /// </summary>
        [Button(ButtonSizes.Medium)]
        public void RefreshCacheEditorOnly()
        {
            var assetGuids = AssetDatabase.FindAssets("t:SaveableScriptableObject");

            var foundAssets = assetGuids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(path => AssetDatabase.LoadAssetAtPath<SaveableScriptableObject>(path))
                .Where(asset => asset != null)
                .OrderBy(asset => asset.name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            m_Container = new Dictionary<string, Dictionary<string, SaveableScriptableObject>>();
            m_AllSaveableScriptableObjects = new List<SaveableScriptableObject>();

            var seenAssets = new HashSet<SaveableScriptableObject>();
            var usedGuids = new HashSet<string>();

            foreach (var asset in foundAssets)
            {
                if (asset == null)
                    continue;

                // Prevent processing the same instance twice.
                if (!seenAssets.Add(asset))
                    continue;

                EnsureValidUniqueGuidEditorOnly(asset, usedGuids);

                var guid = asset.Guid;
                if (string.IsNullOrEmpty(guid))
                {
                    Debug.LogWarning(
                        $"[{nameof(ScriptableObjectRegistry)}] '{asset.name}' still has an empty Guid after validation.",
                        asset
                    );
                    continue;
                }

                usedGuids.Add(guid);

                var typeKey = TypeKey(asset.GetType());

                if (!m_Container.TryGetValue(typeKey, out var innerDict) || innerDict == null)
                {
                    innerDict = new Dictionary<string, SaveableScriptableObject>();
                    m_Container[typeKey] = innerDict;
                }

                innerDict[guid] = asset;
                m_AllSaveableScriptableObjects.Add(asset);
            }

            CleanupKeepAliveList();

            m_IsBaked = true;

            EditorUtility.SetDirty(this);
            EditorSaveScheduler.QueueSaveAssetsOnce();
        }
#endif
    }
}