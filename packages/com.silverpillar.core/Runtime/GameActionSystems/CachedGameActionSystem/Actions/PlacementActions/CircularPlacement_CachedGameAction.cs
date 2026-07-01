using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class CircularPlacement_CachedGameAction : ICachedGameAction
    {
        [Title("Placement Object")]
        [SerializeField]
        private SelfType m_WhatObjectToPlace;

        [SerializeField, ShowIf(nameof(m_WhatObjectToPlace), SelfType.CustomGameObject)]
        private GameObject m_CustomPlacementObject;

        [Title("Placement Settings")]
        [SerializeField]
        private Transform m_Center;

        [SerializeField]
        private Space m_PlacementSpace;

        [OdinSerialize, ShowInInspector,
         Tooltip("Interaction score between center and placed gameobject " +
                 "where center is self and placement gameobject is other.")]
        private ICachedInteractionScore m_Radius;

        [OdinSerialize, ShowInInspector]
        private ICachedScore m_DivideCircleBy;

        [Title("Runtime")]
        [ShowInInspector, ReadOnly]
        private int m_CurrentPlacementIndex;

        private GameObject m_GameObject;

        public ICachedGameAction Clone()
        {
            return new CircularPlacement_CachedGameAction
            {
                m_WhatObjectToPlace = m_WhatObjectToPlace,
                m_CustomPlacementObject = m_CustomPlacementObject,
                m_Center = m_Center,
                m_PlacementSpace = m_PlacementSpace,
                m_Radius = m_Radius?.Clone(),
                m_DivideCircleBy = m_DivideCircleBy?.Clone(),
                m_CurrentPlacementIndex = m_CurrentPlacementIndex,
                m_GameObject = m_GameObject
            };
        }

        public void Execute()
        {
            GameObject placementObject = GetPlacementObject();

            if (placementObject == null)
            {
                Debug.LogWarning($"{nameof(CircularPlacement_CachedGameAction)} could not execute because the placement object is null.");
                return;
            }

            Transform center = GetCenterTransform();

            if (center == null)
            {
                Debug.LogWarning($"{nameof(CircularPlacement_CachedGameAction)} could not execute because the center is null.");
                return;
            }

            int divisionCount = GetDivisionCount();
            float radius = GetRadius(center, placementObject);

            int placementIndex = GetWrappedPlacementIndex(divisionCount);

            float angleInDegrees = 360f * placementIndex / divisionCount;
            float angleInRadians = angleInDegrees * Mathf.Deg2Rad;

            Vector3 circleOffset = GetCircleOffset(
                center,
                radius,
                angleInRadians);

            placementObject.transform.position = center.position + circleOffset;

            m_CurrentPlacementIndex = (m_CurrentPlacementIndex + 1) % divisionCount;
        }

        public GameObject GetGameObject()
        {
            return m_GameObject;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            m_GameObject = gameObj;

            bool successfullySetDivideCircleBy = true;

            if (m_DivideCircleBy != null)
            {
                successfullySetDivideCircleBy = m_DivideCircleBy.SetGameObject(GetPlacementObject());
            }

            // Important:
            // Do NOT set m_Radius here.
            // m_Radius uses center.gameObject as self and placementObject as target.
            // That is resolved during Execute().
            return successfullySetDivideCircleBy;
        }

        public void ResetPlacementIndex()
        {
            m_CurrentPlacementIndex = 0;
        }

        public void SetPlacementIndex(int placementIndex)
        {
            m_CurrentPlacementIndex = Mathf.Max(0, placementIndex);
        }

        private GameObject GetPlacementObject()
        {
            if (m_WhatObjectToPlace == SelfType.CustomGameObject)
            {
                return m_CustomPlacementObject;
            }

            return m_GameObject;
        }

        private Transform GetCenterTransform()
        {
            if (m_Center != null)
            {
                return m_Center;
            }

            if (m_GameObject != null)
            {
                return m_GameObject.transform;
            }

            return null;
        }

        private int GetDivisionCount()
        {
            if (m_DivideCircleBy == null)
            {
                return 1;
            }

            float rawDivisionCount = m_DivideCircleBy.CalculateScore();
            int divisionCount = Mathf.RoundToInt(rawDivisionCount);

            return Mathf.Max(1, divisionCount);
        }

        private float GetRadius(Transform center, GameObject placementObject)
        {
            if (m_Radius == null)
            {
                return 0f;
            }

            if (center == null || placementObject == null)
            {
                return 0f;
            }

            bool successfullySetCenterAsSelf = m_Radius.SetGameObject(center.gameObject);

            if (!successfullySetCenterAsSelf)
            {
                Debug.LogWarning(
                    $"{nameof(CircularPlacement_CachedGameAction)} could not set center as self on radius score.");

                return 0f;
            }

            float radius = m_Radius.CalculateScore(placementObject);
            return Mathf.Max(0f, radius);
        }

        private int GetWrappedPlacementIndex(int divisionCount)
        {
            divisionCount = Mathf.Max(1, divisionCount);

            if (m_CurrentPlacementIndex < 0)
            {
                m_CurrentPlacementIndex = 0;
            }

            return m_CurrentPlacementIndex % divisionCount;
        }

        private Vector3 GetCircleOffset(
            Transform center,
            float radius,
            float angleInRadians)
        {
            Vector3 rightAxis;
            Vector3 forwardAxis;

            if (m_PlacementSpace == Space.Self)
            {
                rightAxis = center.right;
                forwardAxis = center.forward;
            }
            else
            {
                rightAxis = Vector3.right;
                forwardAxis = Vector3.forward;
            }

            Vector3 direction =
                rightAxis * Mathf.Cos(angleInRadians) +
                forwardAxis * Mathf.Sin(angleInRadians);

            if (direction.sqrMagnitude <= 0.000001f)
            {
                direction = rightAxis;
            }

            return direction.normalized * radius;
        }
    }
}