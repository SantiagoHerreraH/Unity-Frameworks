using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace SilverPillar.XR
{
    public class MultipleImagesTrackingManager : MonoBehaviour
    {
        public struct TrackingInstanceData
        {
            public GameObject Instance;
            public ARAnchor Anchor;
        }

        [Title("References")]
        [SerializeField, Tooltip("If null will try to get from self")]
        private ARTrackedImageManager m_TrackedImageManager;

        [Title("Data")]
        [SerializeField]
        private ImageTrackingPrefabData m_ImageTrackingPrefabData;

        [Title("Tracking Settings")]
        [SerializeField]
        private bool m_RequireFullTracking = true;

        [Title("Spawning Settings")]
        [SerializeField]
        private bool m_ParentInstanceToAnchors = true;

        [SerializeField, ShowIf(nameof(m_ParentInstanceToAnchors)), Tooltip("If null will try to get from self")]
        private ARAnchorManager m_AnchorManager;

        private readonly Dictionary<string, TrackingInstanceData> m_ReferenceImageName_To_InstanceData = new();
        private readonly HashSet<string> m_PendingAnchorRequests = new();

        private bool m_Initialized;

        private void Awake()
        {
            if (!m_TrackedImageManager)
            {
                m_TrackedImageManager = GetComponent<ARTrackedImageManager>();

                if (!m_TrackedImageManager)
                {
                    Debug.LogError($"[Missing Component]: ARTrackedImageManager not found on {gameObject.name}. Image tracking will not work.");
                }
            }

            if (m_ParentInstanceToAnchors && !m_AnchorManager)
            {
                m_AnchorManager = GetComponent<ARAnchorManager>();

                if (!m_AnchorManager)
                {
                    Debug.LogWarning($"[Missing Component]: ARAnchorManager not found on {gameObject.name}.");
                }
            }

            if (!m_ImageTrackingPrefabData)
            {
                Debug.LogWarning($"[Missing Data]: {nameof(m_ImageTrackingPrefabData)} is not assigned on {gameObject.name}.");
            }
        }

        private void Start()
        {
            Initialize();
        }

        private void OnEnable()
        {
            if (m_TrackedImageManager == null) return;

            Debug.Log($"ImageTracking running: {m_TrackedImageManager.subsystem?.running}");
            m_TrackedImageManager.trackablesChanged.AddListener(OnTrackablesChanged);
        }

        private void OnDestroy()
        {
            if (m_TrackedImageManager == null) return;

            m_TrackedImageManager.trackablesChanged.RemoveListener(OnTrackablesChanged);
        }

        private void Initialize()
        {
            if (m_Initialized) return;

            m_Initialized = true;

            if (m_ImageTrackingPrefabData == null)
            {
                Debug.LogError($"[{nameof(MultipleImagesTrackingManager)}]: Cannot initialize because ImageTrackingPrefabData is null.");
                return;
            }

            foreach (var item in m_ImageTrackingPrefabData.PrefabData)
            {
                string imageName = item.XRReferenceImageName;
                GameObject prefab = item.Prefab;

                if (string.IsNullOrWhiteSpace(imageName))
                {
                    Debug.LogWarning($"[{nameof(MultipleImagesTrackingManager)}]: Found item with empty image name.");
                    continue;
                }

                if (prefab == null)
                {
                    Debug.LogWarning($"[{nameof(MultipleImagesTrackingManager)}]: Prefab for image '{imageName}' is null.");
                    continue;
                }

                if (m_ReferenceImageName_To_InstanceData.ContainsKey(imageName))
                {
                    Debug.LogWarning($"[{nameof(MultipleImagesTrackingManager)}]: Duplicate image name '{imageName}'. Skipping.");
                    continue;
                }

                GameObject instance = Instantiate(prefab, transform);
                instance.name = $"{prefab.name}_{imageName}";
                instance.SetActive(false);

                m_ReferenceImageName_To_InstanceData.Add(imageName, new TrackingInstanceData
                {
                    Instance = instance,
                    Anchor = null
                });
            }
        }

        public void OnTrackablesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> args)
        {
            foreach (ARTrackedImage img in args.added)
            {
                UpdateTrackedImage(img);
            }

            foreach (ARTrackedImage img in args.updated)
            {
                UpdateTrackedImage(img);
            }

            foreach (var img in args.removed)
            {
                UpdateTrackedImage(img.Value, true);
            }
        }

        private void UpdateTrackedImage(ARTrackedImage trackedImage, bool forceDisable = false)
        {
            if (trackedImage == null) return;

            string imageName = trackedImage.referenceImage.name;

            if (!m_ReferenceImageName_To_InstanceData.TryGetValue(imageName, out TrackingInstanceData instanceData))
            {
                Debug.LogWarning($"[{nameof(MultipleImagesTrackingManager)}]: No prefab registered for tracked image '{imageName}'.");
                return;
            }

            GameObject instance = instanceData.Instance;

            if (instance == null) return;

            bool shouldDisable =
                forceDisable ||
                trackedImage.trackingState == TrackingState.None ||
                trackedImage.trackingState == TrackingState.Limited ||
                (m_RequireFullTracking && trackedImage.trackingState != TrackingState.Tracking);

            if (shouldDisable)
            {
                instance.SetActive(false);
                return;
            }

            instance.SetActive(true);

            Pose pose = new Pose(
                trackedImage.transform.position,
                trackedImage.transform.rotation
            );

            if (!m_ParentInstanceToAnchors)
            {
                instance.transform.SetPositionAndRotation(pose.position, pose.rotation);
                return;
            }

            if (instanceData.Anchor == null)
            {
                CreateAnchorAsync(imageName, pose);
                return;
            }

            instanceData.Anchor.transform.SetPositionAndRotation(pose.position, pose.rotation);
        }

        private async void CreateAnchorAsync(string imageName, Pose pose)
        {
            if (m_AnchorManager == null)
            {
                Debug.LogError("[Missing Component]: ARAnchorManager is required to create anchors.");
                return;
            }

            if (m_PendingAnchorRequests.Contains(imageName))
            {
                return;
            }

            if (!m_ReferenceImageName_To_InstanceData.TryGetValue(imageName, out TrackingInstanceData instanceData))
            {
                return;
            }

            m_PendingAnchorRequests.Add(imageName);

            var result = await m_AnchorManager.TryAddAnchorAsync(pose);

            m_PendingAnchorRequests.Remove(imageName);

            if (!result.status.IsSuccess())
            {
                Debug.LogWarning($"[Anchor Failed]: Could not create anchor for '{imageName}'. Status: {result.status}");
                return;
            }

            ARAnchor anchor = result.value;

            if (anchor == null)
            {
                Debug.LogWarning($"[Anchor Failed]: Anchor returned null for '{imageName}'.");
                return;
            }

            instanceData.Anchor = anchor;

            if (instanceData.Instance != null)
            {
                instanceData.Instance.transform.SetParent(anchor.transform, false);
                instanceData.Instance.transform.localPosition = Vector3.zero;
                instanceData.Instance.transform.localRotation = Quaternion.identity;
            }

            m_ReferenceImageName_To_InstanceData[imageName] = instanceData;
        }
    }
}