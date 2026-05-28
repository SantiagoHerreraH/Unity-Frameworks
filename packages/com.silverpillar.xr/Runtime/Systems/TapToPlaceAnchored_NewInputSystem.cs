#if ENABLE_INPUT_SYSTEM
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace SilverPillar.XR
{

    public class TapToPlaceAnchored_NewInputSystem : MonoBehaviour
    {
        public enum RaycastType
        {
            All,
            OnlyTrackables,
            OnlyNonTrackables,
        }

        [Title("Prefab")]
        [SerializeField] private GameObject m_CurrentPrefabToPlace;
        [SerializeField, Min(1)] private int m_MaxNumberOfInstancesPerPrefab = 1;

        [Title("Managers")]
        [SerializeField, Tooltip("If null will get from self")]
        private ARAnchorManager m_AnchorManager;

        [SerializeField, Tooltip("If null will get from self")]
        private ARPlaneManager m_PlaneManager;

        [SerializeField, Tooltip("If null will get from self")]
        private ARRaycastManager m_RaycastManager;

        [Title("Placement Filter")]
        [SerializeField] private LayerMask m_AllowedHitLayers = ~0;
        [SerializeField] private RaycastType m_RaycastType;

        [Title("Input")]
        [SerializeField] private bool m_AllowMouseInEditor = true;

        [Title("Input Blocking")]
        [SerializeField] private bool m_BlockWhenPointerOverUI = true;
        [SerializeField] private LayerMask m_InputBlockLayers = 0;

        [Title("Events")]
        [SerializeField] private UnityEvent<GameObject> m_OnPlace = new();
        [SerializeField] private UnityEvent<GameObject> m_OnSpawn = new();
        [SerializeField] private UnityEvent<GameObject> m_OnReuse = new();

        private readonly Dictionary<GameObject, List<GameObject>> m_PrefabToInstances = new();

        private bool m_IsPlacing;

        private static readonly List<ARRaycastHit> s_Hits = new();
        private static readonly List<RaycastResult> s_UIRaycastResults = new();

        private void Awake()
        {
            if (m_RaycastManager == null)
                m_RaycastManager = GetComponent<ARRaycastManager>();

            if (m_AnchorManager == null)
                m_AnchorManager = GetComponent<ARAnchorManager>();

            if (m_PlaneManager == null)
                m_PlaneManager = GetComponent<ARPlaneManager>();

            RegisterPrefabIfNeeded(m_CurrentPrefabToPlace);
        }

        private void Update()
        {

            if (!TryGetScreenPressBegan(out Vector2 screenPos))
                return;

            if (IsTapBlocked(screenPos))
                return;

            if (!TryGetPlacementPose(screenPos, out Pose pose))
                return;

            _ = PlaceAnchoredAsync(pose, m_CurrentPrefabToPlace);
        }

        public void SetPrefabToPlace(GameObject prefab)
        {
            m_CurrentPrefabToPlace = prefab;
            RegisterPrefabIfNeeded(prefab);
        }

        public GameObject GetPrefabToPlace()
        {
            return m_CurrentPrefabToPlace;
        }

        public int CurrentPrefabInstancesCount()
        {
            if (m_CurrentPrefabToPlace == null)
                return 0;

            if (!m_PrefabToInstances.TryGetValue(m_CurrentPrefabToPlace, out List<GameObject> instances))
                return 0;

            instances.RemoveAll(instance => instance == null);
            return instances.Count;
        }

        public void AddListenerOnPlace(UnityAction<GameObject> action)
        {
            m_OnPlace.AddListener(action);
        }

        public void AddListenerOnSpawn(UnityAction<GameObject> action)
        {
            m_OnSpawn.AddListener(action);
        }

        public void AddListenerOnReuse(UnityAction<GameObject> action)
        {
            m_OnReuse.AddListener(action);
        }

        private bool TryGetCurrentPrefab(out GameObject prefab)
        {
            prefab = m_CurrentPrefabToPlace;
            return prefab != null;
        }

        private void RegisterPrefabIfNeeded(GameObject prefab)
        {
            if (prefab == null)
                return;

            if (!m_PrefabToInstances.ContainsKey(prefab))
                m_PrefabToInstances.Add(prefab, new List<GameObject>());
        }

        private bool TryGetPlacementPose(Vector2 screenPos, out Pose pose)
        {
            pose = default;

            switch (m_RaycastType)
            {
                case RaycastType.OnlyTrackables:
                    return TryGetTrackablePose(screenPos, out pose);

                case RaycastType.OnlyNonTrackables:
                    return TryGetNonTrackablePose(screenPos, out pose);

                case RaycastType.All:
                    return TryGetTrackablePose(screenPos, out pose) ||
                           TryGetNonTrackablePose(screenPos, out pose);

                default:
                    return false;
            }
        }

        private bool TryGetTrackablePose(Vector2 screenPos, out Pose pose)
        {
            pose = default;

            if (m_RaycastManager == null)
                return false;

            if (!m_RaycastManager.Raycast(screenPos, s_Hits, TrackableType.All))
                return false;

            foreach (ARRaycastHit hit in s_Hits)
            {
                if (!IsAllowedTrackableHit(hit))
                    continue;

                pose = hit.pose;
                return true;
            }

            return false;
        }

        private bool TryGetNonTrackablePose(Vector2 screenPos, out Pose pose)
        {
            pose = default;

            Camera camera = Camera.main;

            if (camera == null)
                return false;

            Ray ray = camera.ScreenPointToRay(screenPos);

            if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, m_AllowedHitLayers))
                return false;

            pose = new Pose(
                hit.point,
                Quaternion.LookRotation(camera.transform.forward, hit.normal));

            return true;
        }

        private bool IsAllowedTrackableHit(in ARRaycastHit hit)
        {
            if (m_PlaneManager == null)
                return true;

            ARPlane plane = m_PlaneManager.GetPlane(hit.trackableId);

            if (plane == null)
                return false;

            int planeLayer = plane.gameObject.layer;
            return (m_AllowedHitLayers.value & (1 << planeLayer)) != 0;
        }

        private bool IsTapBlocked(Vector2 screenPos)
        {
            if (m_BlockWhenPointerOverUI && IsScreenPositionOverUI(screenPos))
                return true;

            if (m_InputBlockLayers.value != 0)
            {
                Camera camera = Camera.main;

                if (camera != null)
                {
                    Ray ray = camera.ScreenPointToRay(screenPos);

                    if (Physics.Raycast(ray, Mathf.Infinity, m_InputBlockLayers))
                        return true;
                }
            }

            return false;
        }

        private static bool IsScreenPositionOverUI(Vector2 screenPos)
        {
            if (EventSystem.current == null)
                return false;

            PointerEventData eventData = new(EventSystem.current)
            {
                position = screenPos
            };

            s_UIRaycastResults.Clear();
            EventSystem.current.RaycastAll(eventData, s_UIRaycastResults);

            return s_UIRaycastResults.Count > 0;
        }

        private async Task PlaceAnchoredAsync(Pose pose, GameObject prefabToPlace)
        {
            if (m_IsPlacing)
                return;

            if (prefabToPlace == null)
                return;

            m_IsPlacing = true;

            try
            {
                RegisterPrefabIfNeeded(prefabToPlace);

                List<GameObject> instances = m_PrefabToInstances[prefabToPlace];
                instances.RemoveAll(instance => instance == null);

                GameObject instanceToPlace;

                if (instances.Count >= m_MaxNumberOfInstancesPerPrefab)
                {
                    instanceToPlace = instances[0];
                    instances.RemoveAt(0);

                    await MoveInstanceToPoseAsync(instanceToPlace, pose);

                    m_OnReuse.Invoke(instanceToPlace);
                }
                else
                {
                    instanceToPlace = Instantiate(prefabToPlace);

                    EnsureHasCollider(instanceToPlace);

                    await MoveInstanceToPoseAsync(instanceToPlace, pose);

                    m_OnSpawn.Invoke(instanceToPlace);
                }

                instances.Add(instanceToPlace);

                instanceToPlace.SetActive(false);
                instanceToPlace.SetActive(true);

                m_OnPlace.Invoke(instanceToPlace);
            }
            finally
            {
                m_IsPlacing = false;
            }
        }

        private async Task MoveInstanceToPoseAsync(GameObject instance, Pose pose)
        {
            if (instance == null)
                return;

            ARAnchor anchor = null;

            if (m_AnchorManager != null)
            {
                var result = await m_AnchorManager.TryAddAnchorAsync(pose);

                if (result.status.IsSuccess())
                    anchor = result.value;
            }

            if (anchor != null)
            {
                instance.transform.SetParent(anchor.transform, false);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
            }
            else
            {
                instance.transform.SetParent(transform, true);
                instance.transform.SetPositionAndRotation(pose.position, pose.rotation);
            }
        }

        private static void EnsureHasCollider(GameObject instance)
        {
            if (instance == null)
                return;

            if (instance.GetComponentInChildren<Collider>() != null)
                return;

            Renderer renderer = instance.GetComponentInChildren<Renderer>();

            if (renderer == null)
                return;

            BoxCollider box = instance.AddComponent<BoxCollider>();
            box.center = instance.transform.InverseTransformPoint(renderer.bounds.center);

            Vector3 sizeWorld = renderer.bounds.size;
            Vector3 lossy = instance.transform.lossyScale;

            box.size = new Vector3(
                lossy.x != 0 ? sizeWorld.x / lossy.x : sizeWorld.x,
                lossy.y != 0 ? sizeWorld.y / lossy.y : sizeWorld.y,
                lossy.z != 0 ? sizeWorld.z / lossy.z : sizeWorld.z
            );
        }

        private bool TryGetScreenPressBegan(out Vector2 screenPos)
        {
            screenPos = default;

#if UNITY_EDITOR || UNITY_STANDALONE
            if (m_AllowMouseInEditor &&
                Mouse.current != null &&
                Mouse.current.leftButton.wasPressedThisFrame)
            {
                screenPos = Mouse.current.position.ReadValue();
                return true;
            }
#endif

            if (Touchscreen.current == null)
                return false;

            var primary = Touchscreen.current.primaryTouch;

            if (!primary.press.wasPressedThisFrame)
                return false;

            screenPos = primary.position.ReadValue();
            return true;
        }
    }
}
#endif