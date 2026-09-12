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
    public class MoveTowardsTarget_ContactInteraction : IContactInteraction
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


        [Title("Target")]
        [OdinSerialize, ShowInInspector]
        private GameObjectCalculatorData m_Target;

        [OdinSerialize, ShowInInspector]
        private CachedScoreData m_DistanceFromTarget;


        [Title("Events")]
        [Tooltip(
            "How close the controller must be to the calculated target position " +
            "to be considered as having reached the destination.")]
        [SerializeField, Min(0)]
        private float m_ReachedTargetDistance = 0.05f;

        [SerializeField]
        private UnityEvent m_OnReachedTargetDestination;

        [SerializeField]
        private UnityEvent m_OnNoLongerReachedTargetDestination;


        private GameObject m_Self;

        // World-space vector from the contacted GameObject origin to the
        // average contact point captured when this interaction starts.
        private Vector3 m_ContactVector;

        private bool m_IsInitialized;

        // Runtime state used so events only fire when the reached state changes.
        private bool m_HasReachedTargetDestination;


        public IContactInteraction Clone()
        {
            return new MoveTowardsTarget_ContactInteraction
            {
                m_WhereToGetControllerFrom = m_WhereToGetControllerFrom,
                m_Controller = m_Controller,

                m_MovementType = m_MovementType,

                m_Speed = m_Speed.CloneData(),
                m_Target = m_Target.CloneData(),
                m_DistanceFromTarget = m_DistanceFromTarget.CloneData(),

                m_ReachedTargetDistance = m_ReachedTargetDistance,

                // Preserve inspector-configured listeners.
                m_OnReachedTargetDestination = m_OnReachedTargetDestination,
                m_OnNoLongerReachedTargetDestination = m_OnNoLongerReachedTargetDestination,

                // Runtime state starts fresh.
                m_HasReachedTargetDestination = false,
                m_IsInitialized = false,
                m_ContactVector = Vector3.zero
            };
        }


        public void Update()
        {
            if (!m_IsInitialized ||
                m_Controller == null ||
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


            Vector3 controllerPosition =
                m_Controller.transform.position;

            // Contact interactions target the same calculated GameObject as the
            // CachedGameAction, but offset that position by the contact vector.
            Vector3 targetPosition =
                targetGameObject.transform.position +
                m_ContactVector;


            float distanceFromTarget =
                Mathf.Max(
                    0f,
                    m_DistanceFromTarget.CalculateScore());


            // ---------------------------------------------------------
            // Calculate desired position.
            //
            // Stay distanceFromTarget units away from the target,
            // along the current target -> controller direction.
            // ---------------------------------------------------------

            Vector3 targetToController =
                controllerPosition -
                targetPosition;


            if (targetToController.sqrMagnitude > Mathf.Epsilon)
            {
                targetPosition +=
                    targetToController.normalized *
                    distanceFromTarget;
            }


            // ---------------------------------------------------------
            // Check whether we're already there.
            // ---------------------------------------------------------

            if (HasReachedTargetDestination(
                    controllerPosition,
                    targetPosition))
            {
                SetReachedTargetDestination(true);
                StopMovement();

                return;
            }


            // We were reached previously, but the target moved far
            // enough away that the desired destination changed.
            SetReachedTargetDestination(false);


            Vector3 directionToTarget =
                targetPosition -
                controllerPosition;


            if (directionToTarget.sqrMagnitude <= Mathf.Epsilon)
            {
                SetReachedTargetDestination(true);
                StopMovement();

                return;
            }


            float speed =
                Mathf.Max(
                    0f,
                    m_Speed.CalculateScore());


            Vector3 movement =
                directionToTarget.normalized *
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
            // Check again after movement.
            //
            // Particularly useful for MovePosition, which may have
            // reached the destination during this Update().
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


        // =============================================================
        // Reached-state handling
        // =============================================================

        private bool HasReachedTargetDestination(
            Vector3 controllerPosition,
            Vector3 targetPosition)
        {
            float reachedDistance =
                Mathf.Max(
                    0f,
                    m_ReachedTargetDistance);

            return
                (targetPosition - controllerPosition).sqrMagnitude
                <= reachedDistance * reachedDistance;
        }


        /// <summary>
        /// Changes the reached state and invokes an event only when
        /// that state actually changes.
        /// </summary>
        private void SetReachedTargetDestination(bool reached)
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
            // SetMovement persists until another movement is supplied,
            // so explicitly clear it when movement should stop.
            if (m_Controller != null &&
                m_MovementType ==
                TopDownControllerMovementType.SetMovement)
            {
                m_Controller.SetMovement(
                    Vector3.zero);
            }
        }


        // =============================================================
        // Movement
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


        // =============================================================
        // IContactInteraction
        // =============================================================

        public void Start(
            GameObject self,
            ContactData otherData)
        {
            m_Self = self;
            m_ContactVector = CalculateContactVector(otherData);
            m_HasReachedTargetDestination = false;
            m_IsInitialized = false;

            if (m_Self == null)
            {
                Debug.LogError(
                    $"self is NULL in {nameof(MoveTowardsTarget_ContactInteraction)}");

                return;
            }

            bool allGood = true;

            // ---------------------------------------------------------
            // Target
            // ---------------------------------------------------------

            allGood &= m_Target.SetGameObject(m_Self);

            if (!m_Target.IsValid())
            {
                Debug.LogError(
                    $"{nameof(m_Target)} is not valid in " +
                    $"{nameof(MoveTowardsTarget_ContactInteraction)}");

                allGood = false;
            }

            // ---------------------------------------------------------
            // Speed
            // ---------------------------------------------------------

            allGood &= m_Speed.SetGameObject(m_Self);

            if (!m_Speed.IsValid())
            {
                Debug.LogError(
                    $"{nameof(m_Speed)} is not valid in " +
                    $"{nameof(MoveTowardsTarget_ContactInteraction)}");

                allGood = false;
            }

            // ---------------------------------------------------------
            // Distance From Target
            // ---------------------------------------------------------

            allGood &= m_DistanceFromTarget.SetGameObject(m_Self);

            if (!m_DistanceFromTarget.IsValid())
            {
                Debug.LogError(
                    $"{nameof(m_DistanceFromTarget)} is not valid in " +
                    $"{nameof(MoveTowardsTarget_ContactInteraction)}");

                allGood = false;
            }

            // ---------------------------------------------------------
            // Controller
            // ---------------------------------------------------------

            switch (m_WhereToGetControllerFrom)
            {
                case SelfType.ThisGameObject:

                    if (!m_Self.TryGetComponent(out m_Controller))
                    {
                        Debug.LogError(
                            $"{m_Self.name} doesn't contain a " +
                            $"{nameof(TopDownController)} required by " +
                            $"{nameof(MoveTowardsTarget_ContactInteraction)}");

                        allGood = false;
                    }

                    break;

                case SelfType.CustomGameObject:

                    if (m_Controller == null)
                    {
                        Debug.LogError(
                            $"{nameof(m_Controller)} is NULL in " +
                            $"{nameof(MoveTowardsTarget_ContactInteraction)}");

                        allGood = false;
                    }

                    break;
            }

            m_IsInitialized = allGood;
        }


        public void End()
        {
            SetReachedTargetDestination(false);
            StopMovement();
            m_IsInitialized = false;
        }


        private static Vector3 CalculateContactVector(ContactData contactData)
        {
            if (contactData.GameObject == null ||
                contactData.WorldContactPoints == null ||
                contactData.WorldContactPoints.Count == 0)
            {
                return Vector3.zero;
            }

            return
                contactData.GetAverageWorldContactPoint().Position -
                contactData.GameObject.transform.position;
        }
    }
}
