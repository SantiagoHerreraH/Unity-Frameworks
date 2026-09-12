using UnityEngine;
using SilverPillar.Core;
using System;
using Sirenix.OdinInspector;
using MoreMountains.TopDownEngine;
using Sirenix.Serialization;

namespace SilverPillar.Integrations.MMTopDown
{
    [Serializable]
    public class TeleportTowardsTarget_ContactInteraction : IContactInteraction
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

        // World-space vector from the contacted GameObject origin to the
        // average contact point captured when this interaction starts.
        private Vector3 m_ContactVector;

        private bool m_IsInitialized;


        public IContactInteraction Clone()
        {
            return new TeleportTowardsTarget_ContactInteraction
            {
                m_WhereToGetControllerFrom = m_WhereToGetControllerFrom,
                m_Controller = m_Controller,

                m_Target = m_Target.CloneData(),
                m_DistanceFromTarget = m_DistanceFromTarget.CloneData(),

                m_ContactVector = Vector3.zero,
                m_IsInitialized = false
            };
        }


        public void Update()
        {
            if (!m_IsInitialized ||
                m_Controller == null ||
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
            Vector3 targetPosition =
                targetGameObject.transform.position +
                m_ContactVector;

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


        public void Start(
            GameObject self,
            ContactData otherData)
        {
            m_Self = self;
            m_ContactVector = CalculateContactVector(otherData);
            m_IsInitialized = false;

            if (m_Self == null)
            {
                Debug.LogError(
                    $"self is NULL in {nameof(TeleportTowardsTarget_ContactInteraction)}");

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
                    $"{nameof(TeleportTowardsTarget_ContactInteraction)}");

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
                    $"{nameof(TeleportTowardsTarget_ContactInteraction)}");

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
                            $"{nameof(TeleportTowardsTarget_ContactInteraction)}");

                        allGood = false;
                    }

                    break;

                case SelfType.CustomGameObject:

                    if (m_Controller == null)
                    {
                        Debug.LogError(
                            $"{nameof(m_Controller)} is NULL in " +
                            $"{nameof(TeleportTowardsTarget_ContactInteraction)}");

                        allGood = false;
                    }

                    break;
            }

            m_IsInitialized = allGood;
        }


        public void End()
        {
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
