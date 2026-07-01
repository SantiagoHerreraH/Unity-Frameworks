using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace SilverPillar.Core
{
    public class MoveToMouse : SerializedMonoBehaviour
    {
        public enum MovementType
        {
            FreeMovement,
            ConstraintMovementAroundArea
        }

        public enum HowToMove
        {
            TransformMoveInstantly,
            TransformMoveSmoothly,
            RigidbodySetForce,
            RigidbodyAddForce
        }

        private bool UsesMovementMagnitude => m_HowToMove != HowToMove.TransformMoveInstantly;

        private bool UsesRigidbodyMovement =>
            m_HowToMove == HowToMove.RigidbodySetForce ||
            m_HowToMove == HowToMove.RigidbodyAddForce;

        private bool UsesRigidbodyAddForce => m_HowToMove == HowToMove.RigidbodyAddForce;

        [Title("Settings")]
        [SerializeField]
        private MovementType m_MovementType;

        [SerializeField, Tooltip("If null, Camera.main will be used.")]
        private Camera m_Camera;

        [Title("Movement")]
        [SerializeField]
        private HowToMove m_HowToMove = HowToMove.TransformMoveInstantly;

        [OdinSerialize, ShowInInspector]
        [LabelText("$" + nameof(GetMovementMagnitudeLabel))]
        [ShowIf(nameof(UsesMovementMagnitude))]
        private ICachedScore m_MovementMagnitude;

        private Rigidbody m_Rigidbody;

        [SerializeField, ShowIf(nameof(UsesRigidbodyAddForce))]
        private ForceMode m_ForceMode = ForceMode.Force;

        private string GetMovementMagnitudeLabel()
        {
            return m_HowToMove switch
            {
                HowToMove.TransformMoveSmoothly => "Movement Speed",
                HowToMove.RigidbodySetForce => "Velocity",
                HowToMove.RigidbodyAddForce => "Force",
                _ => "Movement Magnitude"
            };
        }

        [Title("Constraint Area")]
        [SerializeField, ShowIf(nameof(m_MovementType), MovementType.ConstraintMovementAroundArea)]
        private Transform m_ConstraintCenter;

        [OdinSerialize, ShowInInspector, ShowIf(nameof(m_MovementType), MovementType.ConstraintMovementAroundArea)]
        private ICachedScore m_MinDistanceFromConstraintTransform;

        [OdinSerialize, ShowInInspector, ShowIf(nameof(m_MovementType), MovementType.ConstraintMovementAroundArea)]
        private ICachedScore m_MaxDistanceFromConstraintTransform;

        private Vector3 m_LastTargetPosition;
        private bool m_HasTargetPosition;

        private const float ReachThreshold = 0.001f;


        private void Awake()
        {
            CacheReferences();
            CacheScores();
        }

        private void OnEnable()
        {
            m_HasTargetPosition = false;

            CacheReferences();
            CacheScores();
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

            MoveTransform(targetPosition, Time.deltaTime);
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

            MoveRigidbody(rigidbodyToUse, m_LastTargetPosition, Time.fixedDeltaTime);
        }

        private void CacheReferences()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
            if (m_Rigidbody == null)
            {
                Debug.LogError("Rigidbody is null in" + nameof(MoveToMouse) + "component in gameobject" + gameObject.name + ". Add a rigidbody to said gameobject.");
            }
        }

        private void CacheScores()
        {
            if (m_MovementMagnitude != null)
            {
                m_MovementMagnitude.SetGameObject(gameObject);
            }

            if (m_MinDistanceFromConstraintTransform != null)
            {
                m_MinDistanceFromConstraintTransform.SetGameObject(gameObject);
            }

            if (m_MaxDistanceFromConstraintTransform != null)
            {
                m_MaxDistanceFromConstraintTransform.SetGameObject(gameObject);
            }
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

            if (m_MovementType == MovementType.ConstraintMovementAroundArea)
            {
                targetPosition = GetConstrainedPosition(targetPosition);
            }

            return true;
        }

        private void MoveTransform(Vector3 targetPosition, float deltaTime)
        {
            switch (m_HowToMove)
            {
                case HowToMove.TransformMoveInstantly:
                    transform.position = targetPosition;
                    break;

                case HowToMove.TransformMoveSmoothly:
                    float speed = GetMovementMagnitude();
                    transform.position = Vector3.MoveTowards(
                        transform.position,
                        targetPosition,
                        speed * deltaTime
                    );
                    break;
            }
        }

        private void MoveRigidbody(Rigidbody rigidbodyToUse, Vector3 targetPosition, float fixedDeltaTime)
        {
            Vector3 currentPosition = rigidbodyToUse.position;
            Vector3 movementDirection = targetPosition - currentPosition;

            float distanceToTarget = movementDirection.magnitude;

            if (distanceToTarget <= ReachThreshold)
            {
                if (m_HowToMove == HowToMove.RigidbodySetForce)
                {
                    SetRigidbodyVelocity(rigidbodyToUse, Vector3.zero);
                }

                return;
            }

            float magnitude = GetMovementMagnitude();

            if (magnitude <= Mathf.Epsilon)
            {
                if (m_HowToMove == HowToMove.RigidbodySetForce)
                {
                    SetRigidbodyVelocity(rigidbodyToUse, Vector3.zero);
                }

                return;
            }

            Vector3 direction = movementDirection / distanceToTarget;

            switch (m_HowToMove)
            {
                case HowToMove.RigidbodySetForce:
                    MoveRigidbodyBySettingVelocity(
                        rigidbodyToUse,
                        targetPosition,
                        direction,
                        distanceToTarget,
                        magnitude,
                        fixedDeltaTime
                    );
                    break;

                case HowToMove.RigidbodyAddForce:
                    rigidbodyToUse.AddForce(direction * magnitude, m_ForceMode);
                    break;
            }
        }

        private void MoveRigidbodyBySettingVelocity(
            Rigidbody rigidbodyToUse,
            Vector3 targetPosition,
            Vector3 direction,
            float distanceToTarget,
            float velocity,
            float fixedDeltaTime)
        {
            float movementThisFrame = velocity * fixedDeltaTime;

            if (distanceToTarget <= movementThisFrame)
            {
                rigidbodyToUse.MovePosition(targetPosition);
                SetRigidbodyVelocity(rigidbodyToUse, Vector3.zero);
                return;
            }

            SetRigidbodyVelocity(rigidbodyToUse, direction * velocity);
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

        private Vector3 GetConstrainedPosition(Vector3 targetPosition)
        {
            if (m_ConstraintCenter == null)
            {
                return targetPosition;
            }

            float minDistance = GetMinDistance();
            float maxDistance = GetMaxDistance();

            if (maxDistance < minDistance)
            {
                (minDistance, maxDistance) = (maxDistance, minDistance);
            }

            Vector3 centerPosition = m_ConstraintCenter.position;
            Vector3 directionFromCenter = targetPosition - centerPosition;

            float currentDistance = directionFromCenter.magnitude;

            if (currentDistance <= Mathf.Epsilon)
            {
                directionFromCenter = GetFallbackDirection(centerPosition);
                currentDistance = directionFromCenter.magnitude;
            }

            if (currentDistance <= Mathf.Epsilon)
            {
                return centerPosition;
            }

            float constrainedDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);

            return centerPosition + directionFromCenter.normalized * constrainedDistance;
        }

        private float GetMovementMagnitude()
        {
            if (m_MovementMagnitude == null)
            {
                return 0f;
            }

            return Mathf.Max(0f, m_MovementMagnitude.CalculateScore());
        }

        private float GetMinDistance()
        {
            if (m_MinDistanceFromConstraintTransform == null)
            {
                return 0f;
            }

            return Mathf.Max(0f, m_MinDistanceFromConstraintTransform.CalculateScore());
        }

        private float GetMaxDistance()
        {
            if (m_MaxDistanceFromConstraintTransform == null)
            {
                return float.MaxValue;
            }

            return Mathf.Max(0f, m_MaxDistanceFromConstraintTransform.CalculateScore());
        }

        private Vector3 GetFallbackDirection(Vector3 centerPosition)
        {
            Vector3 currentDirection = transform.position - centerPosition;

            if (currentDirection.sqrMagnitude > Mathf.Epsilon)
            {
                return currentDirection;
            }

            return transform.right;
        }

        private void SetRigidbodyVelocity(Rigidbody rigidbodyToUse, Vector3 velocity)
        {
#if UNITY_6000_0_OR_NEWER
            rigidbodyToUse.linearVelocity = velocity;
#else
            rigidbodyToUse.velocity = velocity;
#endif
        }
    }
}