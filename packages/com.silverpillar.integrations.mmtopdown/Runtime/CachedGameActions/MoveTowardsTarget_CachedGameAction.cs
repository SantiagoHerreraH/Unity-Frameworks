using UnityEngine;
using SilverPillar.Core;
using System;
using Sirenix.OdinInspector;
using MoreMountains.TopDownEngine;
using Sirenix.Serialization;

namespace SilverPillar.Integrations.MMTopDown
{
    [Serializable]
    public class MoveTowardsTarget_CachedGameAction : ICachedGameAction
    {
        public enum MovementType
        {
            SetMovement,
            AddForce,
            MovePosition
        }

        [Title("Controller")]
        [SerializeField]
        private SelfType m_WhereToGetControllerFrom;

        [SerializeField, ShowIf(nameof(m_WhereToGetControllerFrom), SelfType.CustomGameObject)]
        private TopDownController m_Controller;


        [Title("Movement")]
        [SerializeField]
        private MovementType m_MovementType;

        [OdinSerialize, ShowInInspector]
        private CachedScoreData m_Speed;


        [Title("Target")]
        [OdinSerialize, ShowInInspector]
        private GameObjectCalculatorData m_Target;

        [OdinSerialize, ShowInInspector]
        private CachedScoreData m_DistanceFromTarget;

        private GameObject m_Self;


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

                m_Self = m_Self
            };
        }


        public void Execute()
        {
            if (m_Controller == null ||
                !m_Target.IsValid() ||
                !m_Speed.IsValid())
            {
                return;
            }

            GameObject targetGameObject = m_Target.CalculateGameObject();

            if (targetGameObject == null)
            {
                return;
            }

            Vector3 controllerPosition = m_Controller.transform.position;
            Vector3 targetPosition = targetGameObject.transform.position;

            Vector3 directionToTarget = targetPosition - controllerPosition;
            targetPosition -= directionToTarget.normalized * m_DistanceFromTarget.CalculateScore();

            directionToTarget = targetPosition - controllerPosition;

            if (directionToTarget.sqrMagnitude <= Mathf.Epsilon)
            {
                // Important in case SetMovement was previously non-zero.
                if (m_MovementType == MovementType.SetMovement)
                {
                    m_Controller.SetMovement(Vector3.zero);
                }

                return;
            }

            float speed = Mathf.Max(0f, m_Speed.CalculateScore());

            Vector3 movement = directionToTarget.normalized * speed;

            switch (m_MovementType)
            {
                case MovementType.SetMovement:
                    SetMovement(movement);
                    break;

                case MovementType.AddForce:
                    AddForce(movement);
                    break;

                case MovementType.MovePosition:
                    MovePosition(targetPosition, speed);
                    break;
            }
        }


        private void SetMovement(Vector3 movement)
        {
            m_Controller.SetMovement(movement);
        }


        private void AddForce(Vector3 movement)
        {
            m_Controller.AddForce(movement);
        }


        private void MovePosition(Vector3 targetPosition, float speed)
        {
            Vector3 newPosition = Vector3.MoveTowards(
                m_Controller.transform.position,
                targetPosition,
                speed * Time.deltaTime);

            m_Controller.MovePosition(newPosition);
        }


        public GameObject GetGameObject()
        {
            return m_Self;
        }


        public bool SetGameObject(GameObject gameObj)
        {
            if (gameObj == null)
            {
                Debug.LogError(
                    $"gameObj is NULL in {nameof(MoveTowardsTarget_CachedGameAction)}");

                return false;
            }

            m_Self = gameObj;

            bool allGood = true;


            // ---------------------------------------------------------
            // Target
            // ---------------------------------------------------------

            if (!m_Target.IsValid())
            {
                Debug.LogError(
                    $"{nameof(m_Target)} is not valid in {nameof(MoveTowardsTarget_CachedGameAction)}");

                allGood = false;
            }
            else
            {
                allGood &= m_Target.SetGameObject(gameObj);
            }


            // ---------------------------------------------------------
            // Speed
            // ---------------------------------------------------------

            if (!m_Speed.IsValid())
            {
                Debug.LogError(
                    $"{nameof(m_Speed)} is NULL in {nameof(MoveTowardsTarget_CachedGameAction)}");

                allGood = false;
            }
            else
            {
                allGood &= m_Speed.SetGameObject(gameObj);
            }

            // ---------------------------------------------------------
            // Speed
            // ---------------------------------------------------------

            if (!m_DistanceFromTarget.IsValid())
            {
                Debug.LogError(
                    $"{nameof(m_DistanceFromTarget)} is NULL in {nameof(MoveTowardsTarget_CachedGameAction)}");

                allGood = false;
            }
            else
            {
                allGood &= m_DistanceFromTarget.SetGameObject(gameObj);
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
                            $"{m_Self.name} doesn't contain a {nameof(TopDownController)} " +
                            $"required by {nameof(MoveTowardsTarget_CachedGameAction)}");

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