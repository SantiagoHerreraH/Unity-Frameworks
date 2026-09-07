using MoreMountains.TopDownEngine;
using SilverPillar.Core;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace SilverPillar.Integrations.MMTopDown
{
    [Serializable]
    public class MoveToTargetAimablePosition_Action : ICachedGameAction
    {
        [Title("Controller")]
        [SerializeField]
        private SelfType m_WhereToGetControllerFrom;

        [SerializeField, ShowIf(nameof(m_WhereToGetControllerFrom), SelfType.CustomGameObject)]
        private TopDownController m_Controller;


        [Title("Movement")]
        [SerializeField]
        private TopDownControllerMovementType m_MovementType;

        [OdinSerialize, ShowInInspector]
        private CachedScoreData m_Speed;


        [Title("Raycast")]
        [InfoBox(
            "Raycast from target to self to find an aimable position. " +
            "If the position found is more than distance from target units away, " +
            "choose that position. If not, choose new direction based on current direction plus angle of next raycast, " +
            "taking into account self up vector. Every time between calculations, check if the current target position still meets the criteria with a raycast. " +
            "If not, calculate the target position again")]
        [SerializeField]
        private LayerMask m_LayerMask;

        [SerializeField, Min(1)]
        private float m_AngleOfNextRaycast = 40f;

        [SerializeField, Min(0)]
        private float m_TimeBetweenRaycastCalculations = 2f;


        [Title("Target")]
        [OdinSerialize, ShowInInspector]
        private GameObjectCalculatorData m_Target;

        [OdinSerialize, ShowInInspector]
        private CachedScoreData m_DistanceFromTarget;


        [Title("Events")]
        [Tooltip(
            "How close the controller must be to the calculated aimable position " +
            "to be considered as having reached the destination.")]
        [SerializeField, Min(0)]
        private float m_ReachedTargetDistance = 0.05f;

        [SerializeField]
        private UnityEvent m_OnReachedTargetDestination;

        [SerializeField]
        private UnityEvent m_OnNoLongerReachedTargetDestination;


        private GameObject m_Self;


        // Direction from the target towards the currently selected
        // aimable position.
        private Vector3 m_CurrentAimableDirection;

        private bool m_HasAimablePosition;

        private float m_LastRaycastCalculationTime =
            float.NegativeInfinity;


        // ---------------------------------------------------------
        // Destination event state
        // ---------------------------------------------------------

        // Used to make the events state-transition events:
        //
        // false -> true  = OnReachedTargetDestination
        // true -> false  = OnNoLongerReachedTargetDestination
        //
        // This prevents the events from being invoked every Execute().
        private bool m_HasReachedTargetDestination;


        // Used so raycast validation doesn't allocate every Execute().
        private const int RaycastBufferSize = 16;

        [NonSerialized]
        private RaycastHit[] m_RaycastHits;


        public ICachedGameAction Clone()
        {
            return new MoveToTargetAimablePosition_Action
            {
                m_WhereToGetControllerFrom =
                    m_WhereToGetControllerFrom,

                m_Controller =
                    m_Controller,

                m_MovementType =
                    m_MovementType,

                m_Speed =
                    m_Speed.CloneData(),

                m_LayerMask =
                    m_LayerMask,

                m_AngleOfNextRaycast =
                    m_AngleOfNextRaycast,

                m_TimeBetweenRaycastCalculations =
                    m_TimeBetweenRaycastCalculations,

                m_Target =
                    m_Target.CloneData(),

                m_DistanceFromTarget =
                    m_DistanceFromTarget.CloneData(),

                m_ReachedTargetDistance =
                    m_ReachedTargetDistance,

                // Preserve configured UnityEvent listeners.
                m_OnReachedTargetDestination =
                    m_OnReachedTargetDestination,

                m_OnNoLongerReachedTargetDestination =
                    m_OnNoLongerReachedTargetDestination,

                m_Self =
                    m_Self,

                // Runtime state starts fresh.
                m_HasAimablePosition =
                    false,

                m_HasReachedTargetDestination =
                    false,

                m_LastRaycastCalculationTime =
                    float.NegativeInfinity
            };
        }


        public void Execute()
        {
            if (m_Controller == null ||
                !m_Target.IsValid() ||
                !m_Speed.IsValid() ||
                !m_DistanceFromTarget.IsValid())
            {
                SetReachedTargetDestination(false);

                StopMovement();

                return;
            }


            GameObject targetGameObject =
                m_Target.CalculateGameObject();


            if (targetGameObject == null)
            {
                SetReachedTargetDestination(false);

                StopMovement();

                return;
            }


            float speed =
                Mathf.Max(
                    0f,
                    m_Speed.CalculateScore());


            float distanceFromTarget =
                Mathf.Max(
                    0f,
                    m_DistanceFromTarget.CalculateScore());


            Vector3 targetPosition =
                targetGameObject.transform.position;


            // ---------------------------------------------------------
            // Determine whether the cached aim direction can still
            // be used.
            // ---------------------------------------------------------

            bool calculateNewPosition =
                !m_HasAimablePosition ||
                m_TimeBetweenRaycastCalculations <= 0f ||
                Time.time - m_LastRaycastCalculationTime >=
                m_TimeBetweenRaycastCalculations;


            if (!calculateNewPosition)
            {
                // Even between full calculations we verify that the
                // currently selected direction is still valid.
                //
                // If an obstacle suddenly appears, don't wait for the
                // timer before finding a new destination.
                if (!IsDirectionAimable(
                        targetGameObject,
                        targetPosition,
                        m_CurrentAimableDirection,
                        distanceFromTarget))
                {
                    calculateNewPosition = true;
                }
            }


            // ---------------------------------------------------------
            // Calculate a new aimable direction when necessary.
            // ---------------------------------------------------------

            if (calculateNewPosition)
            {
                if (!TryCalculateAimableDirection(
                        targetGameObject,
                        targetPosition,
                        distanceFromTarget,
                        out m_CurrentAimableDirection))
                {
                    m_HasAimablePosition = false;

                    SetReachedTargetDestination(false);

                    StopMovement();

                    return;
                }


                m_HasAimablePosition = true;

                m_LastRaycastCalculationTime =
                    Time.time;
            }


            // ---------------------------------------------------------
            // Convert direction into current world destination.
            //
            // The direction is cached instead of the position itself,
            // allowing the destination to follow a moving target.
            // ---------------------------------------------------------

            Vector3 aimablePosition =
                targetPosition +
                m_CurrentAimableDirection *
                distanceFromTarget;


            MoveTowardsPosition(
                aimablePosition,
                speed);
        }


        /// <summary>
        /// Starts with the direction from the target towards self.
        ///
        /// If that direction doesn't contain enough unobstructed
        /// space, rotates it around self's up vector and tries again.
        /// </summary>
        private bool TryCalculateAimableDirection(
            GameObject targetGameObject,
            Vector3 targetPosition,
            float distanceFromTarget,
            out Vector3 aimableDirection)
        {
            Vector3 direction =
                m_Controller.transform.position -
                targetPosition;


            // If target and self occupy the exact same position there
            // isn't a target -> self direction, so use forward.
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                direction =
                    m_Self != null
                        ? m_Self.transform.forward
                        : m_Controller.transform.forward;
            }


            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                aimableDirection =
                    Vector3.zero;

                return false;
            }


            direction.Normalize();


            // No distance is required, therefore any direction works.
            if (distanceFromTarget <= Mathf.Epsilon)
            {
                aimableDirection =
                    direction;

                return true;
            }


            Vector3 up =
                m_Self != null
                    ? m_Self.transform.up
                    : m_Controller.transform.up;


            if (up.sqrMagnitude <= Mathf.Epsilon)
            {
                up =
                    Vector3.up;
            }


            up.Normalize();


            float angleStep =
                Mathf.Clamp(
                    Mathf.Abs(m_AngleOfNextRaycast),
                    1f,
                    360f);


            // Prevent infinite searching.
            int numberOfRaycasts =
                Mathf.Max(
                    1,
                    Mathf.CeilToInt(
                        360f / angleStep));


            for (int i = 0; i < numberOfRaycasts; i++)
            {
                if (IsDirectionAimable(
                        targetGameObject,
                        targetPosition,
                        direction,
                        distanceFromTarget))
                {
                    aimableDirection =
                        direction;

                    return true;
                }


                direction =
                    Quaternion.AngleAxis(
                        angleStep,
                        up)
                    * direction;


                direction.Normalize();
            }


            aimableDirection =
                Vector3.zero;

            return false;
        }


        /// <summary>
        /// Checks whether a direction from the target has enough
        /// unobstructed space to place self at the desired distance.
        /// </summary>
        private bool IsDirectionAimable(
            GameObject targetGameObject,
            Vector3 targetPosition,
            Vector3 direction,
            float distanceFromTarget)
        {
            if (distanceFromTarget <= Mathf.Epsilon)
            {
                return true;
            }


            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return false;
            }


            direction.Normalize();


            EnsureRaycastBuffer();


            int hitCount =
                Physics.RaycastNonAlloc(
                    targetPosition,
                    direction,
                    m_RaycastHits,
                    distanceFromTarget,
                    m_LayerMask,
                    QueryTriggerInteraction.UseGlobal);


            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider =
                    m_RaycastHits[i].collider;


                if (hitCollider == null)
                {
                    continue;
                }


                // Ignore self.
                if (IsColliderPartOfGameObject(
                        hitCollider,
                        m_Self))
                {
                    continue;
                }


                // Ignore target.
                if (IsColliderPartOfGameObject(
                        hitCollider,
                        targetGameObject))
                {
                    continue;
                }


                // Something else blocks the destination.
                return false;
            }


            return true;
        }


        private bool IsColliderPartOfGameObject(
            Collider collider,
            GameObject gameObject)
        {
            if (collider == null ||
                gameObject == null)
            {
                return false;
            }


            Transform colliderTransform =
                collider.transform;

            Transform root =
                gameObject.transform;


            return
                colliderTransform == root ||
                colliderTransform.IsChildOf(root);
        }


        private void EnsureRaycastBuffer()
        {
            if (m_RaycastHits == null ||
                m_RaycastHits.Length != RaycastBufferSize)
            {
                m_RaycastHits =
                    new RaycastHit[RaycastBufferSize];
            }
        }


        // =============================================================
        // Movement
        // =============================================================

        private void MoveTowardsPosition(
            Vector3 targetPosition,
            float speed)
        {
            Vector3 controllerPosition =
                m_Controller.transform.position;


            // ---------------------------------------------------------
            // Check destination before issuing movement.
            // ---------------------------------------------------------

            if (HasReachedTargetDestination(
                    controllerPosition,
                    targetPosition))
            {
                SetReachedTargetDestination(true);

                StopMovement();

                return;
            }


            // We were previously at the destination, but the target
            // moved or a different aimable position was selected.
            SetReachedTargetDestination(false);


            Vector3 direction =
                targetPosition -
                controllerPosition;


            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                SetReachedTargetDestination(true);

                StopMovement();

                return;
            }


            Vector3 movement =
                direction.normalized *
                speed;


            switch (m_MovementType)
            {
                case TopDownControllerMovementType.SetMovement:

                    SetMovement(movement);

                    break;


                case TopDownControllerMovementType.AddForce:

                    AddForce(movement);

                    break;


                case TopDownControllerMovementType.MovePosition:

                    MovePosition(
                        targetPosition,
                        speed);

                    break;
            }


            // ---------------------------------------------------------
            // Check again after issuing movement.
            //
            // This matters especially for MovePosition, as the
            // controller may have reached the destination during this
            // Execute() call.
            // ---------------------------------------------------------

            controllerPosition =
                m_Controller.transform.position;


            if (HasReachedTargetDestination(
                    controllerPosition,
                    targetPosition))
            {
                SetReachedTargetDestination(true);

                StopMovement();
            }
        }


        /// <summary>
        /// Returns whether the controller is close enough to the
        /// currently calculated target destination.
        /// </summary>
        private bool HasReachedTargetDestination(
            Vector3 controllerPosition,
            Vector3 targetPosition)
        {
            float reachedDistance =
                Mathf.Max(
                    0f,
                    m_ReachedTargetDistance);


            float reachedDistanceSqr =
                reachedDistance *
                reachedDistance;


            return
                (targetPosition - controllerPosition).sqrMagnitude
                <= reachedDistanceSqr;
        }


        // =============================================================
        // Events
        // =============================================================

        /// <summary>
        /// Changes the reached state and invokes the appropriate event
        /// only when the state actually changes.
        /// </summary>
        private void SetReachedTargetDestination(
            bool reached)
        {
            if (m_HasReachedTargetDestination == reached)
            {
                return;
            }


            m_HasReachedTargetDestination =
                reached;


            if (reached)
            {
                m_OnReachedTargetDestination?.Invoke();
            }
            else
            {
                m_OnNoLongerReachedTargetDestination?.Invoke();
            }
        }


        private void StopMovement()
        {
            // SetMovement persists until another movement value is set,
            // so explicitly clear it when we've reached the destination
            // or can no longer move towards one.
            if (m_Controller != null &&
                m_MovementType ==
                TopDownControllerMovementType.SetMovement)
            {
                m_Controller.SetMovement(
                    Vector3.zero);
            }
        }


        // =============================================================
        // ICachedGameAction
        // =============================================================

        public GameObject GetGameObject()
        {
            return m_Self;
        }


        public bool SetGameObject(GameObject gameObj)
        {
            if (gameObj == null)
            {
                Debug.LogError(
                    $"gameObj is NULL in " +
                    $"{nameof(MoveToTargetAimablePosition_Action)}");

                return false;
            }


            m_Self =
                gameObj;


            // ---------------------------------------------------------
            // Reset runtime state.
            //
            // Don't invoke events here. SetGameObject is initialization,
            // not a gameplay transition.
            // ---------------------------------------------------------

            m_HasAimablePosition =
                false;

            m_HasReachedTargetDestination =
                false;

            m_LastRaycastCalculationTime =
                float.NegativeInfinity;


            bool allGood =
                true;


            // ---------------------------------------------------------
            // Target
            // ---------------------------------------------------------

            if (!m_Target.IsValid())
            {
                Debug.LogError(
                    $"{nameof(m_Target)} is not valid in " +
                    $"{nameof(MoveToTargetAimablePosition_Action)}");

                allGood =
                    false;
            }
            else
            {
                allGood &=
                    m_Target.SetGameObject(
                        gameObj);
            }


            // ---------------------------------------------------------
            // Distance From Target
            // ---------------------------------------------------------

            if (!m_DistanceFromTarget.IsValid())
            {
                Debug.LogError(
                    $"{nameof(m_DistanceFromTarget)} is not valid in " +
                    $"{nameof(MoveToTargetAimablePosition_Action)}");

                allGood =
                    false;
            }
            else
            {
                allGood &=
                    m_DistanceFromTarget.SetGameObject(
                        gameObj);
            }


            // ---------------------------------------------------------
            // Speed
            // ---------------------------------------------------------

            if (!m_Speed.IsValid())
            {
                Debug.LogError(
                    $"{nameof(m_Speed)} is not valid in " +
                    $"{nameof(MoveToTargetAimablePosition_Action)}");

                allGood =
                    false;
            }
            else
            {
                allGood &=
                    m_Speed.SetGameObject(
                        gameObj);
            }


            // ---------------------------------------------------------
            // Controller
            // ---------------------------------------------------------

            switch (m_WhereToGetControllerFrom)
            {
                case SelfType.ThisGameObject:

                    if (!m_Self.TryGetComponent(
                            out m_Controller))
                    {
                        Debug.LogError(
                            $"{m_Self.name} doesn't contain a " +
                            $"{nameof(TopDownController)} required by " +
                            $"{nameof(MoveToTargetAimablePosition_Action)}");

                        allGood =
                            false;
                    }

                    break;


                case SelfType.CustomGameObject:

                    if (m_Controller == null)
                    {
                        Debug.LogError(
                            $"{nameof(m_Controller)} is NULL in " +
                            $"{nameof(MoveToTargetAimablePosition_Action)}");

                        allGood =
                            false;
                    }

                    break;
            }


            return allGood;
        }


        // =============================================================
        // TopDownController movement wrappers
        // =============================================================

        private void SetMovement(Vector3 movement)
        {
            m_Controller.SetMovement(
                movement);
        }


        private void AddForce(Vector3 movement)
        {
            m_Controller.AddForce(
                movement);
        }


        private void MovePosition(
            Vector3 targetPosition,
            float speed)
        {
            Vector3 newPosition =
                Vector3.MoveTowards(
                    m_Controller.transform.position,
                    targetPosition,
                    speed * Time.deltaTime);


            m_Controller.MovePosition(
                newPosition);
        }
    }
}