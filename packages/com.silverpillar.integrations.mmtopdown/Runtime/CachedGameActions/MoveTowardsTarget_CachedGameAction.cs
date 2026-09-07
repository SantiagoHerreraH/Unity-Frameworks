using MoreMountains.TopDownEngine;
using SilverPillar.Core;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace SilverPillar.Integrations.MMTopDown
{
    public enum TopDownControllerMovementType
    {
        SetMovement,
        AddForce,
        MovePosition
    }

    [Serializable]
    public class MoveTowardsTarget_CachedGameAction : ICachedGameAction
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

        // Runtime state used so events only fire when the reached state changes.
        private bool m_HasReachedTargetDestination;


        public ICachedGameAction Clone()
        {
            return new MoveTowardsTarget_CachedGameAction
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

                m_Self = m_Self,

                // Runtime state starts fresh.
                m_HasReachedTargetDestination = false
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


            Vector3 controllerPosition =
                m_Controller.transform.position;

            Vector3 targetPosition =
                targetGameObject.transform.position;


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
            // reached the destination during this Execute().
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
                    $"{nameof(MoveTowardsTarget_CachedGameAction)}");

                return false;
            }


            m_Self = gameObj;

            // Initialization isn't a gameplay state transition, so
            // reset without invoking either event.
            m_HasReachedTargetDestination = false;


            bool allGood = true;


            // ---------------------------------------------------------
            // Target
            // ---------------------------------------------------------

            if (!m_Target.IsValid())
            {
                Debug.LogError(
                    $"{nameof(m_Target)} is not valid in " +
                    $"{nameof(MoveTowardsTarget_CachedGameAction)}");

                allGood = false;
            }
            else
            {
                allGood &=
                    m_Target.SetGameObject(gameObj);
            }


            // ---------------------------------------------------------
            // Speed
            // ---------------------------------------------------------

            if (!m_Speed.IsValid())
            {
                Debug.LogError(
                    $"{nameof(m_Speed)} is not valid in " +
                    $"{nameof(MoveTowardsTarget_CachedGameAction)}");

                allGood = false;
            }
            else
            {
                allGood &=
                    m_Speed.SetGameObject(gameObj);
            }


            // ---------------------------------------------------------
            // Distance From Target
            // ---------------------------------------------------------

            if (!m_DistanceFromTarget.IsValid())
            {
                Debug.LogError(
                    $"{nameof(m_DistanceFromTarget)} is not valid in " +
                    $"{nameof(MoveTowardsTarget_CachedGameAction)}");

                allGood = false;
            }
            else
            {
                allGood &=
                    m_DistanceFromTarget.SetGameObject(gameObj);
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
                            $"{nameof(MoveTowardsTarget_CachedGameAction)}");

                        allGood = false;
                    }

                    break;


                case SelfType.CustomGameObject:

                    if (m_Controller == null)
                    {
                        Debug.LogError(
                            $"{nameof(m_Controller)} is NULL in " +
                            $"{nameof(MoveTowardsTarget_CachedGameAction)}");

                        allGood = false;
                    }

                    break;
            }


            return allGood;
        }
    }
}