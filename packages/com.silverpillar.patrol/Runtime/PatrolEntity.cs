using SilverPillar.Core;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SilverPillar.Patrol
{
    public class PatrolEntity : SerializedMonoBehaviour
    {
        [Title("Actions")]
        [OdinSerialize, ShowInInspector]
        private ICachedGameAction m_ActionOnEntityArrived;

        [SerializeField]
        private UnityEvent m_OnEntityArrived;

        public enum HowToGetPatrolPath
        {
            SetExplicitTransformReferences,
            SearchPatrolRootsThatMeetConditions
        }

        [Title("Patrol Points")]

        [SerializeField]
        private OnWhichPatrolPointToStartPatrol m_OnWhichPatrolPointToStartPatrol;

        [OdinSerialize, ShowInInspector]
        private ICachedInteractionScore m_ArrivalRadius;

        [SerializeField]
        private HowToGetPatrolPath m_HowToGetPatrolPath;

        [SerializeField, ShowIf(nameof(m_HowToGetPatrolPath), HowToGetPatrolPath.SetExplicitTransformReferences)]
        private List<Transform> m_PatrolPointsInOrder;

        [InfoBox("In a patrol point search, the order of the patrol points starting from the first root that meets the condition is depth first search from the top child to the bottom child.")]
        [OdinSerialize, ShowInInspector, ShowIf(nameof(m_HowToGetPatrolPath), HowToGetPatrolPath.SearchPatrolRootsThatMeetConditions)]
        private IInteractionCondition m_ConditionThatPatrolRootsHaveToFulfill;

        [OdinSerialize, ShowInInspector, Tooltip("One path per root. Zero and negative means there is no max number."), ShowIf(nameof(m_HowToGetPatrolPath), HowToGetPatrolPath.SearchPatrolRootsThatMeetConditions)]
        private IntCachedScore m_MaxNumberOfPatrolPaths;

        public enum PatrolMovementType
        {
            TransformLerp,
            RigidbodyVelocity
        }

        public enum PatrolRotationType
        {
            DontAutoRotate,
            TransformRotationLerp,
            RigidbodyAngularVelocity
        }
        public enum PatrolMovementDirectionType
        {
            DirectToPatrolPoint,
            SmoothFollowRotation
        }

        public enum OnWhichPatrolPointToStartPatrol
        {
            Closest,
            First
        }

        [Title("Patrol Movement")]
        [SerializeField]
        private PatrolMovementType m_PatrolMovementType;

        [OdinSerialize, ShowInInspector]
        private ICachedScore m_MovementSpeed; 
        
        [SerializeField]
        private PatrolMovementDirectionType m_PatrolMovementDirectionType;

        private bool m_ShowSmoothFollowRotationSettings =>
            m_PatrolMovementDirectionType == PatrolMovementDirectionType.SmoothFollowRotation;

        [SerializeField, Range(0f, 1f), ShowIf(nameof(m_ShowSmoothFollowRotationSettings))]
        [Tooltip("0 means move directly to the patrol point. 1 means move fully using the entity forward direction.")]
        private float m_FollowRotationStrength = 0.65f;

        [SerializeField, Range(0.01f, 1f), ShowIf(nameof(m_ShowSmoothFollowRotationSettings))]
        [Tooltip("Guarantees that the movement direction always makes progress toward the patrol point. Higher values are safer but less curved.")]
        private float m_MinimumProgressTowardsTarget = 0.25f;

        [SerializeField, MinValue(0f), ShowIf(nameof(m_ShowSmoothFollowRotationSettings))]
        [Tooltip("When close to the patrol point, the entity switches to direct movement so it does not orbit around the point.")]
        private float m_DirectFinalApproachDistanceMultiplier = 2f;

        [Title("Patrol Rotation")]
        [SerializeField]
        private PatrolRotationType m_PatrolRotationType;

        [OdinSerialize, ShowInInspector, HideIf(nameof(m_PatrolRotationType), PatrolRotationType.DontAutoRotate)]
        private ICachedScore m_RotationSpeed;

        private readonly List<List<Transform>> m_PatrolPaths = new();


        private Rigidbody m_Rigidbody;

        private int m_CurrentPathIndex;
        private int m_CurrentPointIndex;

        private bool m_HasValidPatrolPath;

        private const float DEFAULT_ARRIVAL_RADIUS = 0.25f;

        private void Awake()
        {
            if (m_PatrolMovementType == PatrolMovementType.RigidbodyVelocity ||
                m_PatrolRotationType == PatrolRotationType.RigidbodyAngularVelocity)
            {
                if (!TryGetComponent(out m_Rigidbody))
                {
                    Debug.LogError($"{nameof(PatrolEntity)} in {gameObject.name} requires Rigidbody because either rotation or movement need it");
                }
            }

            SetCachedObjects();

        }

        private void Start()
        {
            RebuildPatrolPaths();
        }

        private void Update()
        {
            if (!m_HasValidPatrolPath)
                return;

            if (m_PatrolMovementType == PatrolMovementType.TransformLerp)
            {
                MoveWithTransform(Time.deltaTime);
                CheckArrival();
            }

            if (m_PatrolRotationType == PatrolRotationType.TransformRotationLerp)
            {
                RotateWithTransform(Time.deltaTime);
            }
        }

        private void FixedUpdate()
        {
            if (!m_HasValidPatrolPath)
                return;

            if (m_PatrolMovementType == PatrolMovementType.RigidbodyVelocity)
            {
                MoveWithRigidbody();
                CheckArrival();
            }

            if (m_PatrolRotationType == PatrolRotationType.RigidbodyAngularVelocity)
            {
                RotateWithRigidbody();
            }
        }


        public void RebuildPatrolPaths()
        {
            m_PatrolPaths.Clear();

            switch (m_HowToGetPatrolPath)
            {
                case HowToGetPatrolPath.SetExplicitTransformReferences:
                    BuildExplicitPatrolPath();
                    break;

                case HowToGetPatrolPath.SearchPatrolRootsThatMeetConditions:
                    BuildSearchedPatrolPaths();
                    break;
            }

            m_CurrentPathIndex = 0;
            m_CurrentPointIndex = 0;

            m_HasValidPatrolPath = HasAnyValidPatrolPath();

            if (!m_HasValidPatrolPath)
                return;

            SetStartingPatrolPoint();
        }

        private void SetStartingPatrolPoint()
        {
            switch (m_OnWhichPatrolPointToStartPatrol)
            {
                case OnWhichPatrolPointToStartPatrol.First:
                    SetFirstValidPatrolPoint();
                    break;

                case OnWhichPatrolPointToStartPatrol.Closest:
                    SetClosestPatrolPoint();
                    break;

                default:
                    SetFirstValidPatrolPoint();
                    break;
            }
        }

        private void SetFirstValidPatrolPoint()
        {
            m_CurrentPathIndex = 0;
            m_CurrentPointIndex = 0;
        }

        private void SetClosestPatrolPoint()
        {
            float closestSqrDistance = float.MaxValue;

            int closestPathIndex = -1;
            int closestPointIndex = -1;

            Vector3 currentPosition = transform.position;

            for (int pathIndex = 0; pathIndex < m_PatrolPaths.Count; pathIndex++)
            {
                List<Transform> path = m_PatrolPaths[pathIndex];

                if (path == null || path.Count == 0)
                    continue;

                for (int pointIndex = 0; pointIndex < path.Count; pointIndex++)
                {
                    Transform patrolPoint = path[pointIndex];

                    if (patrolPoint == null)
                        continue;

                    float sqrDistance = (patrolPoint.position - currentPosition).sqrMagnitude;

                    if (sqrDistance >= closestSqrDistance)
                        continue;

                    closestSqrDistance = sqrDistance;
                    closestPathIndex = pathIndex;
                    closestPointIndex = pointIndex;
                }
            }

            if (closestPathIndex < 0 || closestPointIndex < 0)
            {
                SetFirstValidPatrolPoint();
                return;
            }

            m_CurrentPathIndex = closestPathIndex;
            m_CurrentPointIndex = closestPointIndex;
        }

        private void SetCachedObjects()
        {
            m_ActionOnEntityArrived?.SetGameObject(gameObject);
            m_ArrivalRadius?.SetGameObject(gameObject);
            m_MovementSpeed?.SetGameObject(gameObject);
            m_RotationSpeed?.SetGameObject(gameObject);
            m_ConditionThatPatrolRootsHaveToFulfill?.SetGameObject(gameObject);
            m_MaxNumberOfPatrolPaths?.SetGameObject(gameObject);
        }

        private void BuildExplicitPatrolPath()
        {
            if (m_PatrolPointsInOrder == null || m_PatrolPointsInOrder.Count == 0)
                return;

            List<Transform> path = new();

            for (int i = 0; i < m_PatrolPointsInOrder.Count; i++)
            {
                if (m_PatrolPointsInOrder[i] != null)
                    path.Add(m_PatrolPointsInOrder[i]);
            }

            if (path.Count > 0)
                m_PatrolPaths.Add(path);
        }

        private void BuildSearchedPatrolPaths()
        {
            if (PatrolPoint.PatrolPoints == null || PatrolPoint.PatrolPoints.Count == 0)
                return;

            int maxNumberOfPaths = GetMaxNumberOfPatrolPaths();

            for (int i = 0; i < PatrolPoint.PatrolPoints.Count; i++)
            {
                PatrolPoint patrolPoint = PatrolPoint.PatrolPoints[i];

                if (patrolPoint == null)
                    continue;

                if (!patrolPoint.IsRoot)
                    continue;

                if (m_ConditionThatPatrolRootsHaveToFulfill != null &&
                    !m_ConditionThatPatrolRootsHaveToFulfill.IsFulfilled(patrolPoint.gameObject))
                    continue;

                List<Transform> path = new();
                DepthFirstCollectPatrolPoints(patrolPoint.transform, path);

                if (path.Count > 0)
                    m_PatrolPaths.Add(path);

                if (maxNumberOfPaths > 0 && m_PatrolPaths.Count >= maxNumberOfPaths)
                    break;
            }
        }

        private int GetMaxNumberOfPatrolPaths()
        {
            if (m_MaxNumberOfPatrolPaths == null)
                return 0;

            return m_MaxNumberOfPatrolPaths.CalculateScoreAsInt();
        }

        private void DepthFirstCollectPatrolPoints(Transform currentTransform, List<Transform> path)
        {
            if (currentTransform == null)
                return;

            path.Add(currentTransform);

            for (int i = 0; i < currentTransform.childCount; i++)
            {
                DepthFirstCollectPatrolPoints(currentTransform.GetChild(i), path);
            }
        }

        private bool HasAnyValidPatrolPath()
        {
            for (int i = 0; i < m_PatrolPaths.Count; i++)
            {
                if (m_PatrolPaths[i] != null && m_PatrolPaths[i].Count > 0)
                    return true;
            }

            return false;
        }

        private Transform GetCurrentPatrolPoint()
        {
            if (!m_HasValidPatrolPath)
                return null;

            if (m_CurrentPathIndex < 0 || m_CurrentPathIndex >= m_PatrolPaths.Count)
                return null;

            List<Transform> currentPath = m_PatrolPaths[m_CurrentPathIndex];

            if (currentPath == null || currentPath.Count == 0)
                return null;

            if (m_CurrentPointIndex < 0 || m_CurrentPointIndex >= currentPath.Count)
                return null;

            return currentPath[m_CurrentPointIndex];
        }

        private void MoveWithTransform(float deltaTime)
        {
            Transform patrolPoint = GetCurrentPatrolPoint();

            if (patrolPoint == null)
                return;

            float movementSpeed = GetMovementSpeed();

            if (movementSpeed <= 0f)
                return;

            float maxDistanceDelta = movementSpeed * deltaTime;

            if (ShouldUseDirectFinalApproach(patrolPoint, maxDistanceDelta))
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    patrolPoint.position,
                    maxDistanceDelta);

                return;
            }

            Vector3 movementDirection = GetMovementDirectionTowardsPatrolPoint(patrolPoint);

            if (movementDirection.sqrMagnitude <= Mathf.Epsilon)
                return;

            transform.position += movementDirection * maxDistanceDelta;
        }

        private void MoveWithRigidbody()
        {
            Transform patrolPoint = GetCurrentPatrolPoint();

            if (patrolPoint == null)
                return;

            if (m_Rigidbody == null)
            {
                MoveWithTransform(Time.fixedDeltaTime);
                return;
            }

            float movementSpeed = GetMovementSpeed();

            if (movementSpeed <= 0f)
            {
                SetRigidbodyVelocity(Vector3.zero);
                return;
            }

            float maxDistanceDelta = movementSpeed * Time.fixedDeltaTime;

            Vector3 movementDirection;

            if (ShouldUseDirectFinalApproach(patrolPoint, maxDistanceDelta))
            {
                movementDirection = patrolPoint.position - transform.position;
            }
            else
            {
                movementDirection = GetMovementDirectionTowardsPatrolPoint(patrolPoint);
            }

            if (movementDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                SetRigidbodyVelocity(Vector3.zero);
                return;
            }

            SetRigidbodyVelocity(movementDirection.normalized * movementSpeed);
        }

        private Vector3 GetMovementDirectionTowardsPatrolPoint(Transform patrolPoint)
        {
            if (patrolPoint == null)
                return Vector3.zero;

            Vector3 directionToPatrolPoint = patrolPoint.position - transform.position;

            if (directionToPatrolPoint.sqrMagnitude <= Mathf.Epsilon)
                return Vector3.zero;

            Vector3 directDirection = directionToPatrolPoint.normalized;

            if (m_PatrolMovementDirectionType != PatrolMovementDirectionType.SmoothFollowRotation)
                return directDirection;

            Vector3 forwardDirection = transform.forward;

            if (forwardDirection.sqrMagnitude <= Mathf.Epsilon)
                return directDirection;

            forwardDirection.Normalize();

            Vector3 smoothedDirection = Vector3.Slerp(
                directDirection,
                forwardDirection,
                Mathf.Clamp01(m_FollowRotationStrength));

            return ForceMinimumProgressTowardsTarget(smoothedDirection, directDirection);
        }

        private Vector3 ForceMinimumProgressTowardsTarget(Vector3 movementDirection, Vector3 directDirectionToTarget)
        {
            if (movementDirection.sqrMagnitude <= Mathf.Epsilon)
                return directDirectionToTarget;

            if (directDirectionToTarget.sqrMagnitude <= Mathf.Epsilon)
                return movementDirection.normalized;

            movementDirection.Normalize();
            directDirectionToTarget.Normalize();

            float minimumProgress = Mathf.Clamp(m_MinimumProgressTowardsTarget, 0.01f, 1f);

            float currentProgress = Vector3.Dot(movementDirection, directDirectionToTarget);

            if (currentProgress >= minimumProgress)
                return movementDirection;

            float maxAngleFromTarget = Mathf.Acos(minimumProgress);

            return Vector3.RotateTowards(
                directDirectionToTarget,
                movementDirection,
                maxAngleFromTarget,
                0f).normalized;
        }

        private bool ShouldUseDirectFinalApproach(Transform patrolPoint, float maxDistanceDelta)
        {
            if (patrolPoint == null)
                return false;

            if (m_PatrolMovementDirectionType != PatrolMovementDirectionType.SmoothFollowRotation)
                return false;

            float arrivalRadius = GetArrivalRadius(patrolPoint.gameObject);

            float directFinalApproachDistance =
                arrivalRadius + maxDistanceDelta * Mathf.Max(0f, m_DirectFinalApproachDistanceMultiplier);

            float distanceToTarget = Vector3.Distance(transform.position, patrolPoint.position);

            return distanceToTarget <= directFinalApproachDistance;
        }

        private void RotateWithTransform(float deltaTime)
        {
            Transform patrolPoint = GetCurrentPatrolPoint();

            if (patrolPoint == null)
                return;

            Vector3 direction = patrolPoint.position - transform.position;

            if (direction.sqrMagnitude <= Mathf.Epsilon)
                return;

            float rotationSpeed = GetRotationSpeed();

            if (rotationSpeed <= 0f)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * deltaTime);
        }

        private void RotateWithRigidbody()
        {
            Transform patrolPoint = GetCurrentPatrolPoint();

            if (patrolPoint == null)
                return;

            if (m_Rigidbody == null)
            {
                RotateWithTransform(Time.fixedDeltaTime);
                return;
            }

            Vector3 direction = patrolPoint.position - transform.position;

            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                m_Rigidbody.angularVelocity = Vector3.zero;
                return;
            }

            float rotationSpeed = GetRotationSpeed();

            if (rotationSpeed <= 0f)
            {
                m_Rigidbody.angularVelocity = Vector3.zero;
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            Quaternion rotationDifference = targetRotation * Quaternion.Inverse(m_Rigidbody.rotation);

            rotationDifference.ToAngleAxis(out float angleInDegrees, out Vector3 axis);

            if (angleInDegrees > 180f)
                angleInDegrees -= 360f;

            if (axis.sqrMagnitude <= Mathf.Epsilon)
            {
                m_Rigidbody.angularVelocity = Vector3.zero;
                return;
            }

            float angleInRadians = angleInDegrees * Mathf.Deg2Rad;
            float maxAngularSpeed = rotationSpeed * Mathf.Deg2Rad;

            float desiredAngularSpeed = angleInRadians / Time.fixedDeltaTime;
            desiredAngularSpeed = Mathf.Clamp(desiredAngularSpeed, -maxAngularSpeed, maxAngularSpeed);

            m_Rigidbody.angularVelocity = axis.normalized * desiredAngularSpeed;
        }

        private void CheckArrival()
        {
            Transform patrolPoint = GetCurrentPatrolPoint();

            if (patrolPoint == null)
                return;

            float arrivalRadius = GetArrivalRadius(patrolPoint.gameObject);

            float sqrDistance = (patrolPoint.position - transform.position).sqrMagnitude;

            if (sqrDistance <= arrivalRadius * arrivalRadius)
            {
                OnArrivedAtCurrentPatrolPoint(patrolPoint);
            }
        }

        private void OnArrivedAtCurrentPatrolPoint(Transform patrolPoint)
        {
            StopRigidbodyMovementIfNeeded();

            ExecuteEntityArrivedAction();
            m_OnEntityArrived?.Invoke();

            if (patrolPoint.TryGetComponent(out PatrolPoint patrolPointComponent))
            {
                patrolPointComponent.OnEntityArrived(gameObject);
            }

            GoToNextPatrolPoint();
        }

        private void ExecuteEntityArrivedAction()
        {
            if (m_ActionOnEntityArrived == null)
                return;

            if (m_ActionOnEntityArrived.SetGameObject(gameObject))
            {
                m_ActionOnEntityArrived.Execute();
            }
        }

        private void GoToNextPatrolPoint()
        {
            if (!m_HasValidPatrolPath)
                return;

            List<Transform> currentPath = m_PatrolPaths[m_CurrentPathIndex];

            m_CurrentPointIndex++;

            if (m_CurrentPointIndex < currentPath.Count)
                return;

            m_CurrentPointIndex = 0;
            m_CurrentPathIndex++;

            if (m_CurrentPathIndex >= m_PatrolPaths.Count)
            {
                m_CurrentPathIndex = 0;
            }
        }

        private float GetArrivalRadius(GameObject target)
        {
            if (m_ArrivalRadius == null)
                return DEFAULT_ARRIVAL_RADIUS;

            return Mathf.Max(0f, m_ArrivalRadius.CalculateScore(target));
        }

        private float GetMovementSpeed()
        {
            if (m_MovementSpeed == null)
                return 0f;

            return Mathf.Max(0f, m_MovementSpeed.CalculateScore());
        }

        private float GetRotationSpeed()
        {
            if (m_RotationSpeed == null)
                return 0f;

            return Mathf.Max(0f, m_RotationSpeed.CalculateScore());
        }

        private void StopRigidbodyMovementIfNeeded()
        {
            if (m_Rigidbody == null)
                return;

            if (m_PatrolMovementType == PatrolMovementType.RigidbodyVelocity)
                SetRigidbodyVelocity(Vector3.zero);

            if (m_PatrolRotationType == PatrolRotationType.RigidbodyAngularVelocity)
                m_Rigidbody.angularVelocity = Vector3.zero;
        }

        private void SetRigidbodyVelocity(Vector3 velocity)
        {
#if UNITY_6000_0_OR_NEWER
            m_Rigidbody.linearVelocity = velocity;
#else
            m_Rigidbody.velocity = velocity;
#endif
        }

        private void OnDisable()
        {
            Debug.Log("PatrolEntity Disabled");
        }
    }
}