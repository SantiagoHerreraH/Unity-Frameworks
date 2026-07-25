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

        public enum WhatToMove
        {
            MoveCamera,
            MoveCameraParent,
            MoveCameraGrandParent
        }

        private enum WhenToCall
        {
            LateUpdate,
            OnBeforeRender
        }

        [Title("Camera Target")]
        [SerializeField]
        private CameraType m_CameraType;

        [SerializeField]
        private WhatToMove m_WhatToMove;

        [SerializeField]
        private WhenToCall m_WhenToCall;

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
        private Transform m_ResolvedTransformToMove;
        private Transform m_ResolvedDistanceTarget;

        private bool m_HasLoggedInvalidHierarchy;

        private void OnEnable()
        {
            InitializeReferences();

            if (m_WhenToCall == WhenToCall.OnBeforeRender)
            {
                Application.onBeforeRender += ApplyCameraOffset;
            }

        }

        private void OnDisable()
        {
            if (m_WhenToCall == WhenToCall.OnBeforeRender)
            {
                Application.onBeforeRender -= ApplyCameraOffset;
            }
        }

        private void LateUpdate()
        {
            if (m_WhenToCall == WhenToCall.LateUpdate)
            {
                ApplyCameraOffset();
            }
        }

        /*
         * A high order makes this callback execute after callbacks with
         * lower before-render orders.
         */
        [BeforeRenderOrder(int.MaxValue)]
        private void ApplyCameraOffset()
        {
            RefreshDynamicReferences();
            ApplyDistanceLimit();
        }

        private void ApplyDistanceLimit()
        {
            if (m_ResolvedCamera == null ||
                m_ResolvedTransformToMove == null ||
                m_ResolvedDistanceTarget == null)
            {
                return;
            }

            Transform cameraTransform = m_ResolvedCamera.transform;

            /*
             * If the target is inside the subtree being moved, moving that
             * subtree moves the camera and target together.
             *
             * Their relative distance therefore cannot be changed.
             */
            if (m_ResolvedDistanceTarget == m_ResolvedTransformToMove ||
                m_ResolvedDistanceTarget.IsChildOf(m_ResolvedTransformToMove))
            {
                if (!m_HasLoggedInvalidHierarchy)
                {
                    Debug.LogError(
                        $"[{nameof(CameraDistanceLimiter)}] " +
                        $"The target '{m_ResolvedDistanceTarget.name}' is inside " +
                        $"the hierarchy of the transform being moved " +
                        $"'{m_ResolvedTransformToMove.name}'. The camera-target " +
                        $"distance cannot change because both objects move together.",
                        this);

                    m_HasLoggedInvalidHierarchy = true;
                }

                return;
            }

            m_HasLoggedInvalidHierarchy = false;

            /*
             * Always measure from the actual camera.
             *
             * Do not measure from m_ResolvedTransformToMove because its
             * position is not necessarily the camera position.
             */
            Vector3 cameraOffset =
                cameraTransform.position -
                m_ResolvedDistanceTarget.position;

            float currentDistance = cameraOffset.magnitude;

            float minimumDistance = m_LimitMin
                ? m_MinDistance
                : 0f;

            float maximumDistance = m_LimitMax
                ? m_MaxDistance
                : float.PositiveInfinity;

            if (m_LimitMin && m_LimitMax)
            {
                maximumDistance = Mathf.Max(
                    minimumDistance,
                    maximumDistance);
            }

            float limitedDistance = Mathf.Clamp(
                currentDistance,
                minimumDistance,
                maximumDistance);

            if (Mathf.Approximately(currentDistance, limitedDistance))
                return;

            Vector3 direction;

            if (currentDistance > Mathf.Epsilon)
            {
                direction = cameraOffset / currentDistance;
            }
            else
            {
                direction = -cameraTransform.forward;
            }

            /*
             * This is the desired world position of the camera,
             * regardless of which ancestor will be moved.
             */
            Vector3 targetCameraPosition =
                m_ResolvedDistanceTarget.position +
                direction * limitedDistance;

            Vector3 desiredCameraPosition;

            switch (m_MovementType)
            {
                case MovementType.Instant:
                    desiredCameraPosition = targetCameraPosition;
                    break;

                case MovementType.SmoothMovement:
                    {
                        float interpolation =
                            1f - Mathf.Exp(
                                -m_SmoothSpeed * Time.unscaledDeltaTime);

                        desiredCameraPosition = Vector3.Lerp(
                            cameraTransform.position,
                            targetCameraPosition,
                            interpolation);

                        break;
                    }

                default:
                    return;
            }

            /*
             * Calculate how much the camera must move in world space.
             * Apply that displacement to the selected ancestor.
             */
            Vector3 worldCorrection =
                desiredCameraPosition -
                cameraTransform.position;

            m_ResolvedTransformToMove.position += worldCorrection;
        }

        private void InitializeReferences()
        {
            m_ResolvedCamera = ResolveCamera();
            m_ResolvedDistanceTarget = ResolveDistanceTarget();

            ResolveTransformToMove();
            ValidateReferences();
        }

        private void RefreshDynamicReferences()
        {
            Camera previousCamera = m_ResolvedCamera;

            switch (m_CameraType)
            {
                case CameraType.MainCamera:
                    if (m_ResolvedCamera == null ||
                        !m_ResolvedCamera.isActiveAndEnabled)
                    {
                        m_ResolvedCamera = Camera.main;
                    }

                    break;

                case CameraType.CurrentCamera:
                    /*
                     * Camera.current is only reliably populated while
                     * Unity is processing a camera-render callback.
                     */
                    if (Camera.current != null)
                    {
                        m_ResolvedCamera = Camera.current;
                    }

                    break;

                case CameraType.CameraOnThisGameObject:
                    if (m_ResolvedCamera == null)
                    {
                        m_ResolvedCamera = GetComponent<Camera>();
                    }

                    break;

                case CameraType.CustomCamera:
                    m_ResolvedCamera = m_ChosenCamera;
                    break;
            }

            if (m_ResolvedCamera != previousCamera)
            {
                ResolveTransformToMove();
            }

            if (m_DistanceFrom == SelfType.ThisGameObject)
            {
                m_ResolvedDistanceTarget = transform;
            }
            else
            {
                m_ResolvedDistanceTarget = m_TargetTransform;
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

        private void ResolveTransformToMove()
        {
            m_ResolvedTransformToMove = null;

            if (m_ResolvedCamera == null)
                return;

            Transform cameraTransform = m_ResolvedCamera.transform;

            switch (m_WhatToMove)
            {
                case WhatToMove.MoveCamera:
                    m_ResolvedTransformToMove = cameraTransform;
                    break;

                case WhatToMove.MoveCameraParent:
                    m_ResolvedTransformToMove =
                        cameraTransform.parent;
                    break;

                case WhatToMove.MoveCameraGrandParent:
                    m_ResolvedTransformToMove =
                        cameraTransform.parent != null
                            ? cameraTransform.parent.parent
                            : null;

                    break;
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

        private void ValidateReferences()
        {
            if (m_ResolvedCamera == null)
            {
                Debug.LogError(
                    $"[{nameof(CameraDistanceLimiter)}] " +
                    $"No camera could be resolved on '{gameObject.name}'.",
                    this);

                return;
            }

            if (m_ResolvedTransformToMove == null)
            {
                Debug.LogError(
                    $"[{nameof(CameraDistanceLimiter)}] " +
                    $"The camera '{m_ResolvedCamera.name}' does not have the " +
                    $"parent hierarchy required by '{m_WhatToMove}'.",
                    this);
            }

            if (m_ResolvedDistanceTarget == null)
            {
                Debug.LogError(
                    $"[{nameof(CameraDistanceLimiter)}] " +
                    $"No distance target could be resolved on '{gameObject.name}'.",
                    this);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            m_MinDistance = Mathf.Max(0f, m_MinDistance);
            m_MaxDistance = Mathf.Max(0f, m_MaxDistance);
            m_SmoothSpeed = Mathf.Max(0f, m_SmoothSpeed);

            if (m_LimitMin &&
                m_LimitMax &&
                m_MaxDistance < m_MinDistance)
            {
                m_MaxDistance = m_MinDistance;
            }
        }
#endif
    }
}