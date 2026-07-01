using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.Serialization;

namespace SilverPillar.Core
{
    public class LookAtMouse : SerializedMonoBehaviour
    {
        public enum RotationType
        {
            FreeRotation,
            ConstraintRotationAroundAngle
        }

        public enum HowToRotate
        {
            TransformRotateInstantly,
            TransformRotateSmoothly,
            RigidbodySetTorqueForce,
            RigidbodyAddTorqueForce
        }

        private bool UsesRotationMagnitude => m_HowToRotate != HowToRotate.TransformRotateInstantly;

        private bool UsesRigidbodyMovement =>
            m_HowToRotate == HowToRotate.RigidbodySetTorqueForce ||
            m_HowToRotate == HowToRotate.RigidbodyAddTorqueForce;

        private bool UsesRigidbodyAddForce => m_HowToRotate == HowToRotate.RigidbodyAddTorqueForce;

        [Title("Settings")]
        [SerializeField]
        private RotationType m_RotationType;

        [SerializeField, Tooltip("If null, Camera.main will be used.")]
        private Camera m_Camera;

        [Title("Rotation")]
        [SerializeField, FormerlySerializedAs("m_HowToMove")]
        private HowToRotate m_HowToRotate = HowToRotate.TransformRotateInstantly;

        [OdinSerialize, ShowInInspector]
        [LabelText("$" + nameof(GetRotationMagnitudeLabel))]
        [ShowIf(nameof(UsesRotationMagnitude))]
        private ICachedScore m_RotationMagnitude;

        private Rigidbody m_Rigidbody;

        [SerializeField, ShowIf(nameof(UsesRigidbodyAddForce))]
        private ForceMode m_ForceMode = ForceMode.Force;

        [Title("Constraint Rotation")]
        [SerializeField, ShowIf(nameof(m_RotationType), RotationType.ConstraintRotationAroundAngle)]
        [Tooltip("If null, the rotation when this component is enabled will be used as the reference rotation.")]
        private Transform m_ConstraintReference;

        [SerializeField, ShowIf(nameof(m_RotationType), RotationType.ConstraintRotationAroundAngle)]
        [Tooltip("The axis around which the signed constraint angle is calculated.")]
        private Vector3 m_ConstraintAxis = Vector3.up;

        [OdinSerialize, ShowInInspector, ShowIf(nameof(m_RotationType), RotationType.ConstraintRotationAroundAngle)]
        private ICachedScore m_MinConstraintAngle;

        [OdinSerialize, ShowInInspector, ShowIf(nameof(m_RotationType), RotationType.ConstraintRotationAroundAngle)]
        private ICachedScore m_MaxConstraintAngle;

        private Quaternion m_InitialConstraintReferenceRotation;

        private Vector3 m_LastTargetPosition;
        private bool m_HasTargetPosition;

        private const float AngleThreshold = 0.01f;

        private string GetRotationMagnitudeLabel()
        {
            return m_HowToRotate switch
            {
                HowToRotate.TransformRotateSmoothly => "Rotation Speed",
                HowToRotate.RigidbodySetTorqueForce => "Rotation Velocity",
                HowToRotate.RigidbodyAddTorqueForce => "Rotation Force",
                _ => "Rotation Magnitude"
            };
        }

        private void Awake()
        {
            CacheReferences();
            CacheScores();
            CacheConstraintReferenceRotation();
        }

        private void OnEnable()
        {
            m_HasTargetPosition = false;

            CacheReferences();
            CacheScores();
            CacheConstraintReferenceRotation();
        }

        private void Update()
        {
            if (!TryGetTargetPosition(out Vector3 targetPosition))
            {
                return;
            }

            m_LastTargetPosition = targetPosition;
            m_HasTargetPosition = true;

            if (UsesRigidbodyMovement)
            {
                return;
            }

            RotateTransform(targetPosition, Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (!UsesRigidbodyMovement)
            {
                return;
            }

            Rigidbody rigidbodyToUse = GetRigidbody();

            if (rigidbodyToUse == null)
            {
                return;
            }

            if (!m_HasTargetPosition && !TryGetTargetPosition(out m_LastTargetPosition))
            {
                return;
            }

            RotateRigidbody(rigidbodyToUse, m_LastTargetPosition, Time.fixedDeltaTime);
        }

        private void CacheReferences()
        {
            m_Rigidbody = GetComponent<Rigidbody>();

            if (m_Rigidbody == null && UsesRigidbodyMovement)
            {
                Debug.LogError(
                    "Rigidbody is null in " +
                    nameof(LookAtMouse) +
                    " component in GameObject " +
                    gameObject.name +
                    ". Add a Rigidbody to said GameObject."
                );
            }
        }

        private void CacheScores()
        {
            if (m_RotationMagnitude != null)
            {
                m_RotationMagnitude.SetGameObject(gameObject);
            }

            if (m_MinConstraintAngle != null)
            {
                m_MinConstraintAngle.SetGameObject(gameObject);
            }

            if (m_MaxConstraintAngle != null)
            {
                m_MaxConstraintAngle.SetGameObject(gameObject);
            }
        }

        private void CacheConstraintReferenceRotation()
        {
            m_InitialConstraintReferenceRotation = transform.rotation;
        }

        private bool TryGetTargetPosition(out Vector3 targetPosition)
        {
            targetPosition = transform.position;

            Camera cameraToUse = GetCamera();

            if (cameraToUse == null)
            {
                return false;
            }

            targetPosition = GetMouseWorldPosition(cameraToUse);
            return true;
        }

        private void RotateTransform(Vector3 targetPosition, float deltaTime)
        {
            if (!TryGetTargetRotation(transform.position, targetPosition, out Quaternion targetRotation))
            {
                return;
            }

            switch (m_HowToRotate)
            {
                case HowToRotate.TransformRotateInstantly:
                    transform.rotation = targetRotation;
                    break;

                case HowToRotate.TransformRotateSmoothly:
                    float rotationSpeed = GetRotationMagnitude();

                    if (rotationSpeed <= Mathf.Epsilon)
                    {
                        return;
                    }

                    transform.rotation = Quaternion.RotateTowards(
                        transform.rotation,
                        targetRotation,
                        rotationSpeed * deltaTime
                    );
                    break;
            }
        }

        private void RotateRigidbody(Rigidbody rigidbodyToUse, Vector3 targetPosition, float fixedDeltaTime)
        {
            if (!TryGetTargetRotation(rigidbodyToUse.position, targetPosition, out Quaternion targetRotation))
            {
                if (m_HowToRotate == HowToRotate.RigidbodySetTorqueForce)
                {
                    SetRigidbodyAngularVelocity(rigidbodyToUse, Vector3.zero);
                }

                return;
            }

            if (!TryGetRotationDelta(
                    rigidbodyToUse.rotation,
                    targetRotation,
                    out Vector3 rotationAxis,
                    out float signedAngle))
            {
                if (m_HowToRotate == HowToRotate.RigidbodySetTorqueForce)
                {
                    rigidbodyToUse.MoveRotation(targetRotation);
                    SetRigidbodyAngularVelocity(rigidbodyToUse, Vector3.zero);
                }

                return;
            }

            float magnitude = GetRotationMagnitude();

            if (magnitude <= Mathf.Epsilon)
            {
                if (m_HowToRotate == HowToRotate.RigidbodySetTorqueForce)
                {
                    SetRigidbodyAngularVelocity(rigidbodyToUse, Vector3.zero);
                }

                return;
            }

            Vector3 signedRotationAxis = rotationAxis * Mathf.Sign(signedAngle);

            switch (m_HowToRotate)
            {
                case HowToRotate.RigidbodySetTorqueForce:
                    RotateRigidbodyBySettingAngularVelocity(
                        rigidbodyToUse,
                        targetRotation,
                        signedRotationAxis,
                        Mathf.Abs(signedAngle),
                        magnitude,
                        fixedDeltaTime
                    );
                    break;

                case HowToRotate.RigidbodyAddTorqueForce:
                    rigidbodyToUse.AddTorque(signedRotationAxis * magnitude, m_ForceMode);
                    break;
            }
        }

        private void RotateRigidbodyBySettingAngularVelocity(
            Rigidbody rigidbodyToUse,
            Quaternion targetRotation,
            Vector3 signedRotationAxis,
            float angleToTarget,
            float angularVelocityInDegrees,
            float fixedDeltaTime)
        {
            float rotationThisFrame = angularVelocityInDegrees * fixedDeltaTime;

            if (angleToTarget <= rotationThisFrame || angleToTarget <= AngleThreshold)
            {
                rigidbodyToUse.MoveRotation(targetRotation);
                SetRigidbodyAngularVelocity(rigidbodyToUse, Vector3.zero);
                return;
            }

            Vector3 angularVelocityInRadians =
                signedRotationAxis.normalized *
                angularVelocityInDegrees *
                Mathf.Deg2Rad;

            SetRigidbodyAngularVelocity(rigidbodyToUse, angularVelocityInRadians);
        }

        private Camera GetCamera()
        {
            if (m_Camera != null)
            {
                return m_Camera;
            }

            return Camera.main;
        }

        private Rigidbody GetRigidbody()
        {
            if (m_Rigidbody != null)
            {
                return m_Rigidbody;
            }

            CacheReferences();
            return m_Rigidbody;
        }

        private Vector3 GetMouseWorldPosition(Camera cameraToUse)
        {
            Vector3 mouseScreenPosition = Input.mousePosition;

            float distanceFromCamera = Vector3.Dot(
                transform.position - cameraToUse.transform.position,
                cameraToUse.transform.forward
            );

            mouseScreenPosition.z = distanceFromCamera;

            return cameraToUse.ScreenToWorldPoint(mouseScreenPosition);
        }

        private bool TryGetTargetRotation(
            Vector3 originPosition,
            Vector3 targetPosition,
            out Quaternion targetRotation)
        {
            targetRotation = Quaternion.identity;

            Vector3 directionToTarget = targetPosition - originPosition;

            if (directionToTarget.sqrMagnitude <= Mathf.Epsilon)
            {
                return false;
            }

            if (m_RotationType == RotationType.ConstraintRotationAroundAngle)
            {
                directionToTarget = GetConstrainedDirection(directionToTarget);
            }

            if (directionToTarget.sqrMagnitude <= Mathf.Epsilon)
            {
                return false;
            }

            targetRotation = Quaternion.LookRotation(directionToTarget.normalized, Vector3.up);
            return true;
        }

        private Vector3 GetConstrainedDirection(Vector3 directionToTarget)
        {
            Vector3 constraintAxis = GetConstraintAxis();

            if (constraintAxis.sqrMagnitude <= Mathf.Epsilon)
            {
                return directionToTarget;
            }

            constraintAxis.Normalize();

            Vector3 referenceForward = GetConstraintReferenceForward(constraintAxis);
            Vector3 targetPlanarDirection = Vector3.ProjectOnPlane(directionToTarget, constraintAxis);
            Vector3 targetAxisDirection = Vector3.Project(directionToTarget, constraintAxis);

            if (referenceForward.sqrMagnitude <= Mathf.Epsilon ||
                targetPlanarDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                return directionToTarget;
            }

            referenceForward.Normalize();
            targetPlanarDirection.Normalize();

            float minAngle = GetMinConstraintAngle();
            float maxAngle = GetMaxConstraintAngle();

            if (maxAngle < minAngle)
            {
                (minAngle, maxAngle) = (maxAngle, minAngle);
            }

            float signedAngle = Vector3.SignedAngle(
                referenceForward,
                targetPlanarDirection,
                constraintAxis
            );

            float clampedAngle = Mathf.Clamp(signedAngle, minAngle, maxAngle);

            Vector3 constrainedPlanarDirection =
                Quaternion.AngleAxis(clampedAngle, constraintAxis) * referenceForward;

            float planarMagnitude = Vector3.ProjectOnPlane(directionToTarget, constraintAxis).magnitude;

            return constrainedPlanarDirection.normalized * planarMagnitude + targetAxisDirection;
        }

        private Vector3 GetConstraintAxis()
        {
            if (m_ConstraintAxis.sqrMagnitude > Mathf.Epsilon)
            {
                return m_ConstraintAxis.normalized;
            }

            return Vector3.up;
        }

        private Vector3 GetConstraintReferenceForward(Vector3 constraintAxis)
        {
            Quaternion referenceRotation = GetConstraintReferenceRotation();

            Vector3 referenceForward = Vector3.ProjectOnPlane(
                referenceRotation * Vector3.forward,
                constraintAxis
            );

            if (referenceForward.sqrMagnitude > Mathf.Epsilon)
            {
                return referenceForward.normalized;
            }

            referenceForward = Vector3.ProjectOnPlane(transform.forward, constraintAxis);

            if (referenceForward.sqrMagnitude > Mathf.Epsilon)
            {
                return referenceForward.normalized;
            }

            referenceForward = Vector3.ProjectOnPlane(Vector3.forward, constraintAxis);

            if (referenceForward.sqrMagnitude > Mathf.Epsilon)
            {
                return referenceForward.normalized;
            }

            return Vector3.right;
        }

        private Quaternion GetConstraintReferenceRotation()
        {
            if (m_ConstraintReference != null)
            {
                return m_ConstraintReference.rotation;
            }

            return m_InitialConstraintReferenceRotation;
        }

        private bool TryGetRotationDelta(
            Quaternion currentRotation,
            Quaternion targetRotation,
            out Vector3 rotationAxis,
            out float signedAngle)
        {
            rotationAxis = Vector3.zero;
            signedAngle = 0f;

            Quaternion rotationDelta = targetRotation * Quaternion.Inverse(currentRotation);
            rotationDelta.ToAngleAxis(out float angle, out rotationAxis);

            if (angle > 180f)
            {
                angle -= 360f;
            }

            if (rotationAxis.sqrMagnitude <= Mathf.Epsilon ||
                float.IsNaN(rotationAxis.x) ||
                float.IsNaN(rotationAxis.y) ||
                float.IsNaN(rotationAxis.z))
            {
                return false;
            }

            signedAngle = angle;
            rotationAxis.Normalize();

            return Mathf.Abs(signedAngle) > AngleThreshold;
        }

        private float GetRotationMagnitude()
        {
            if (m_RotationMagnitude == null)
            {
                return 0f;
            }

            return Mathf.Max(0f, m_RotationMagnitude.CalculateScore());
        }

        private float GetMinConstraintAngle()
        {
            if (m_MinConstraintAngle == null)
            {
                return -180f;
            }

            return m_MinConstraintAngle.CalculateScore();
        }

        private float GetMaxConstraintAngle()
        {
            if (m_MaxConstraintAngle == null)
            {
                return 180f;
            }

            return m_MaxConstraintAngle.CalculateScore();
        }

        private void SetRigidbodyAngularVelocity(Rigidbody rigidbodyToUse, Vector3 angularVelocity)
        {
            rigidbodyToUse.angularVelocity = angularVelocity;
        }
    }
}