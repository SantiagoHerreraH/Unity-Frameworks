using UnityEngine;
using SilverPillar.Core;
using System;
using Sirenix.OdinInspector;
using MoreMountains.TopDownEngine;
using Sirenix.Serialization;

namespace SilverPillar.Integrations.MMTopDown
{
    [Serializable]
    public class TeleportTowardsTarget_CachedGameAction : ICachedGameAction
    {
        [Title("Controller")]
        [SerializeField]
        private SelfType m_WhereToGetControllerFrom;

        [SerializeField, ShowIf(nameof(m_WhereToGetControllerFrom), SelfType.CustomGameObject)]
        private TopDownController m_Controller;


        [Title("Target")]
        [OdinSerialize, ShowInInspector]
        private GameObjectCalculatorData m_Target;

        [OdinSerialize, ShowInInspector]
        private CachedScoreData m_DistanceFromTarget;


        private GameObject m_Self;


        public ICachedGameAction Clone()
        {
            return new TeleportTowardsTarget_CachedGameAction
            {
                m_WhereToGetControllerFrom = m_WhereToGetControllerFrom,
                m_Controller = m_Controller,

                m_Target = m_Target.CloneData(),
                m_DistanceFromTarget = m_DistanceFromTarget.CloneData(),

                m_Self = m_Self
            };
        }


        public void Execute()
        {
            if (m_Controller == null ||
                !m_Target.IsValid() ||
                !m_DistanceFromTarget.IsValid())
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

            float distanceFromTarget =
                Mathf.Max(0f, m_DistanceFromTarget.CalculateScore());


            // If we're already exactly at the target, there is no direction
            // from which to place the controller at the requested distance.
            if (directionToTarget.sqrMagnitude <= Mathf.Epsilon)
            {
                if (distanceFromTarget <= Mathf.Epsilon)
                {
                    m_Controller.MovePosition(targetPosition);
                }

                return;
            }

            Vector3 desiredPosition =
                targetPosition -
                directionToTarget.normalized * distanceFromTarget;

            m_Controller.MovePosition(desiredPosition);
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
                    $"gameObj is NULL in {nameof(TeleportTowardsTarget_CachedGameAction)}");

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
                    $"{nameof(m_Target)} is not valid in " +
                    $"{nameof(TeleportTowardsTarget_CachedGameAction)}");

                allGood = false;
            }
            else
            {
                allGood &= m_Target.SetGameObject(gameObj);
            }


            // ---------------------------------------------------------
            // Distance From Target
            // ---------------------------------------------------------

            if (!m_DistanceFromTarget.IsValid())
            {
                Debug.LogError(
                    $"{nameof(m_DistanceFromTarget)} is not valid in " +
                    $"{nameof(TeleportTowardsTarget_CachedGameAction)}");

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
                            $"required by {nameof(TeleportTowardsTarget_CachedGameAction)}");

                        allGood = false;
                    }

                    break;

                case SelfType.CustomGameObject:
                    if (m_Controller == null)
                    {
                        Debug.LogError(
                            $"{nameof(m_Controller)} is NULL in " +
                            $"{nameof(TeleportTowardsTarget_CachedGameAction)}");

                        allGood = false;
                    }

                    break;
            }


            return allGood;
        }
    }
}