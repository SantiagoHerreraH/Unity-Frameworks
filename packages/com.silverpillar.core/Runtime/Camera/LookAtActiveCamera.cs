using Sirenix.OdinInspector;
using UnityEngine;

namespace SilverPillar.Core
{
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public class LookAtActiveCamera : MonoBehaviour
    {
        public enum CameraSource
        {
            MainCamera,
            HighestDepthEnabledCamera,
            CustomCamera
        }

        public enum RotationMode
        {
            Full3D,
            YAxisOnly
        }

        public enum LocalAxisThatFacesCamera
        {
            Forward,
            Backward,
            Up,
            Down,
            Right,
            Left
        }

        public enum UpdateMode
        {
            Update,
            LateUpdate,
            Manual
        }

        public enum RotationType
        {
            Instant,
            Smooth
        }

        [Title("Camera")]
        [SerializeField]
        private CameraSource m_CameraSource = CameraSource.MainCamera;

        [SerializeField, ShowIf(nameof(m_CameraSource), CameraSource.CustomCamera)]
        private Camera m_CustomCamera;

        [SerializeField, Tooltip("If this is true, it will use the parent canvas camera. " +
            "If not found will default to camera source.")]
        private bool m_UseParentCanvasCameraIfAvailable = true;

        [Title("Rotation")]
        [SerializeField]
        private RotationMode m_RotationMode = RotationMode.Full3D;

        [SerializeField]
        private LocalAxisThatFacesCamera m_LocalAxisThatFacesCamera = LocalAxisThatFacesCamera.Forward;

        [SerializeField]
        private Vector3 m_WorldUp = Vector3.up;

        [SerializeField]
        private RotationType m_RotationType;

        [SerializeField, Min(0.01f), ShowIf(nameof(m_RotationType), RotationType.Smooth)]
        private float m_RotationSpeed = 12f;

        [Title("Update")]
        [SerializeField]
        private UpdateMode m_UpdateMode = UpdateMode.LateUpdate;

        private void Update()
        {
            if (m_UpdateMode == UpdateMode.Update)
            {
                LookAtCamera();
            }
        }

        private void LateUpdate()
        {
            if (m_UpdateMode == UpdateMode.LateUpdate)
            {
                LookAtCamera();
            }
        }

        public void LookAtCamera()
        {
            Camera targetCamera = GetTargetCamera();

            if (targetCamera == null)
            {
                return;
            }

            Vector3 directionToCamera = targetCamera.transform.position - transform.position;

            if (m_RotationMode == RotationMode.YAxisOnly)
            {
                directionToCamera.y = 0f;
            }

            if (directionToCamera.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Vector3 up = m_WorldUp.sqrMagnitude <= 0.0001f ? Vector3.up : m_WorldUp.normalized;

            Quaternion lookRotation = Quaternion.LookRotation(directionToCamera.normalized, up);

            Quaternion axisCorrection = Quaternion.FromToRotation(
                GetLocalAxis(m_LocalAxisThatFacesCamera),
                Vector3.forward
            );

            Quaternion targetRotation = lookRotation * axisCorrection;


            if (m_RotationType == RotationType.Smooth && Application.isPlaying)
            {
                float t = 1f - Mathf.Exp(-m_RotationSpeed * Time.deltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, t);
            }
            else
            {
                transform.rotation = targetRotation;
            }
        }

        private Camera GetTargetCamera()
        {
            if (m_CameraSource == CameraSource.CustomCamera)
            {
                return m_CustomCamera;
            }

            Camera canvasCamera = GetCanvasCamera();

            if (canvasCamera != null)
            {
                return canvasCamera;
            }

            switch (m_CameraSource)
            {
                case CameraSource.MainCamera:
                    return Camera.main;

                case CameraSource.HighestDepthEnabledCamera:
                    return GetHighestDepthEnabledCamera();

                default:
                    return Camera.main;
            }
        }

        private Camera GetCanvasCamera()
        {
            if (!m_UseParentCanvasCameraIfAvailable)
            {
                return null;
            }

            Canvas parentCanvas = GetComponentInParent<Canvas>();

            if (parentCanvas == null)
            {
                return null;
            }

            if (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return null;
            }

            return parentCanvas.worldCamera;
        }

        private Camera GetHighestDepthEnabledCamera()
        {
            Camera[] cameras = Camera.allCameras;

            Camera bestCamera = null;
            float bestDepth = float.MinValue;

            foreach (Camera camera in cameras)
            {
                if (camera == null)
                {
                    continue;
                }

                if (!camera.enabled || !camera.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (camera.depth > bestDepth)
                {
                    bestDepth = camera.depth;
                    bestCamera = camera;
                }
            }

            return bestCamera != null ? bestCamera : Camera.main;
        }

        private Vector3 GetLocalAxis(LocalAxisThatFacesCamera axis)
        {
            switch (axis)
            {
                case LocalAxisThatFacesCamera.Forward:
                    return Vector3.forward;

                case LocalAxisThatFacesCamera.Backward:
                    return Vector3.back;

                case LocalAxisThatFacesCamera.Up:
                    return Vector3.up;

                case LocalAxisThatFacesCamera.Down:
                    return Vector3.down;

                case LocalAxisThatFacesCamera.Right:
                    return Vector3.right;

                case LocalAxisThatFacesCamera.Left:
                    return Vector3.left;

                default:
                    return Vector3.forward;
            }
        }
    }
}