using Sirenix.OdinInspector;
using UnityEngine;

namespace SilverPillar.Core
{
    public class CameraDistanceLimiter : MonoBehaviour
    {
        private enum CameraType
        {
            MainCamera,
            CurrentCamera,
            CameraOnThisGameObject,
            CustomCamera
        }

        public enum MovementType
        {
            Instant,
            SmoothMovement
        }

        [Title("Camera Target")]
        [SerializeField]
        private CameraType m_CameraType;

        [SerializeField, ShowIf(nameof(m_CameraType), CameraType.CustomCamera)]
        private Camera m_ChosenCamera;

        [Title("Distance Target")]
        [SerializeField]
        private SelfType m_DistanceFrom;

        [SerializeField, ShowIf(nameof(m_DistanceFrom), SelfType.CustomGameObject)]
        private Transform m_TargetTransform;

        [Title("Distance Limits")]
        [SerializeField]
        private bool m_LimitMin;

        [SerializeField, ShowIf(nameof(m_LimitMin)), Min(0f)]
        private float m_MinDistance = 0.5f;

        [SerializeField]
        private bool m_LimitMax;

        [SerializeField, ShowIf(nameof(m_LimitMax)), Min(0f)]
        private float m_MaxDistance = 1f;

        [Title("Camera Movement Settings")]
        [SerializeField]
        private MovementType m_MovementType;

        [SerializeField,
         ShowIf(nameof(m_MovementType), MovementType.SmoothMovement),
         Min(0f),
         Tooltip("Higher values mean faster tracking. Lower values mean smoother movement.")]
        private float m_SmoothSpeed = 5f;

        private Camera m_ResolvedCamera;
        private Transform m_ResolvedDistanceTarget;

        private void OnEnable()
        {
            InitializeReferences();
        }

        private void LateUpdate()
        {
            RefreshDynamicReferences();

            if (m_ResolvedCamera == null || m_ResolvedDistanceTarget == null)
                return;

            Transform cameraTransform = m_ResolvedCamera.transform;

            // The camera cannot measure its distance from itself.
            if (cameraTransform == m_ResolvedDistanceTarget)
                return;

            Vector3 cameraOffset =
                cameraTransform.position - m_ResolvedDistanceTarget.position;

            float currentDistance = cameraOffset.magnitude;

            float minimumDistance = m_LimitMin
                ? Mathf.Max(0f, m_MinDistance)
                : 0f;

            float maximumDistance = m_LimitMax
                ? Mathf.Max(0f, m_MaxDistance)
                : float.PositiveInfinity;

            if (m_LimitMin && m_LimitMax)
            {
                maximumDistance = Mathf.Max(
                    minimumDistance,
                    maximumDistance);
            }

            float targetDistance = Mathf.Clamp(
                currentDistance,
                minimumDistance,
                maximumDistance);

            // The camera is already inside the valid range.
            if (Mathf.Approximately(currentDistance, targetDistance))
                return;

            Vector3 direction;

            if (currentDistance > Mathf.Epsilon)
            {
                direction = cameraOffset / currentDistance;
            }
            else
            {
                // When the camera and target overlap, move the camera
                // backwards relative to its current orientation.
                direction = -cameraTransform.forward;
            }

            Vector3 targetPosition =
                m_ResolvedDistanceTarget.position +
                direction * targetDistance;

            switch (m_MovementType)
            {
                case MovementType.Instant:
                    cameraTransform.position = targetPosition;
                    break;

                case MovementType.SmoothMovement:
                    float interpolation =
                        1f - Mathf.Exp(-m_SmoothSpeed * Time.deltaTime);

                    cameraTransform.position = Vector3.Lerp(
                        cameraTransform.position,
                        targetPosition,
                        interpolation);
                    break;
            }
        }

        private void InitializeReferences()
        {
            m_ResolvedCamera = ResolveCamera();
            m_ResolvedDistanceTarget = ResolveDistanceTarget();

            if (m_ResolvedCamera == null)
            {
                Debug.LogError(
                    $"[{nameof(CameraDistanceLimiter)}] No camera could be resolved on " +
                    $"GameObject '{gameObject.name}'.",
                    this);
            }

            if (m_ResolvedDistanceTarget == null)
            {
                Debug.LogError(
                    $"[{nameof(CameraDistanceLimiter)}] No distance target could be resolved on " +
                    $"GameObject '{gameObject.name}'.",
                    this);
            }

            if (m_ResolvedCamera != null &&
                m_ResolvedCamera.transform == m_ResolvedDistanceTarget)
            {
                Debug.LogError(
                    $"[{nameof(CameraDistanceLimiter)}] The camera and distance target " +
                    $"cannot reference the same Transform on GameObject '{gameObject.name}'.",
                    this);
            }
        }

        private void RefreshDynamicReferences()
        {
            // Camera.main can change while loading scenes or switching cameras.
            if (m_CameraType == CameraType.MainCamera &&
                (m_ResolvedCamera == null || !m_ResolvedCamera.isActiveAndEnabled))
            {
                m_ResolvedCamera = Camera.main;
            }

            if (m_DistanceFrom == SelfType.ThisGameObject)
            {
                m_ResolvedDistanceTarget = transform;
            }
        }

        private Camera ResolveCamera()
        {
            switch (m_CameraType)
            {
                case CameraType.MainCamera:
                    return Camera.main;

                case CameraType.CurrentCamera:
                    return Camera.current;

                case CameraType.CameraOnThisGameObject:
                    return GetComponent<Camera>();

                case CameraType.CustomCamera:
                    return m_ChosenCamera;

                default:
                    return null;
            }
        }

        private Transform ResolveDistanceTarget()
        {
            switch (m_DistanceFrom)
            {
                case SelfType.ThisGameObject:
                    return transform;

                case SelfType.CustomGameObject:
                    return m_TargetTransform;

                default:
                    return null;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            m_MinDistance = Mathf.Max(0f, m_MinDistance);
            m_MaxDistance = Mathf.Max(0f, m_MaxDistance);
            m_SmoothSpeed = Mathf.Max(0f, m_SmoothSpeed);

            if (m_LimitMin && m_LimitMax && m_MaxDistance < m_MinDistance)
            {
                m_MaxDistance = m_MinDistance;
            }
        }
#endif
    }
}