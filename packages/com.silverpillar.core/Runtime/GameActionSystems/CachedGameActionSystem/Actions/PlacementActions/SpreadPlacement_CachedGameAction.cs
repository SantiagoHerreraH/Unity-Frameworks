using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class SpreadPlacement_CachedGameAction : ICachedGameAction
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
         Tooltip("Interaction score between center and placed gameobject, where center is self and placement gameobject is other.")]
        private ICachedInteractionScore m_Radius;

        [OdinSerialize, ShowInInspector]
        private ICachedScore m_DegreesBetweenPlacement;

        public enum PlacementProtocol
        {
            LeftThenRight,
            RightThenLeft,
            OnlyLeft,
            OnlyRight
        }

        [SerializeField]
        private PlacementProtocol m_PlacementProtocol;

        [Title("Runtime")]
        [ShowInInspector, ReadOnly]
        private int m_CurrentPlacementIndex;

        private GameObject m_GameObject;

        public ICachedGameAction Clone()
        {
            return new SpreadPlacement_CachedGameAction
            {
                m_WhatObjectToPlace = m_WhatObjectToPlace,
                m_CustomPlacementObject = m_CustomPlacementObject,
                m_Center = m_Center,
                m_PlacementSpace = m_PlacementSpace,
                m_Radius = m_Radius?.Clone(),
                m_DegreesBetweenPlacement = m_DegreesBetweenPlacement?.Clone(),
                m_PlacementProtocol = m_PlacementProtocol,
                m_CurrentPlacementIndex = m_CurrentPlacementIndex,
                m_GameObject = m_GameObject
            };
        }

        public void Execute()
        {
            GameObject placementObject = GetPlacementObject();

            if (placementObject == null)
            {
                Debug.LogWarning($"{nameof(SpreadPlacement_CachedGameAction)} could not execute because the placement object is null.");
                return;
            }

            Transform center = GetCenterTransform();

            if (center == null)
            {
                Debug.LogWarning($"{nameof(SpreadPlacement_CachedGameAction)} could not execute because the center is null.");
                return;
            }

            float radius = GetRadius(center, placementObject);
            float degreesBetweenPlacement = GetDegreesBetweenPlacement();

            Vector3 direction = GetPlacementDirection(
                center,
                m_CurrentPlacementIndex,
                degreesBetweenPlacement);

            placementObject.transform.position = center.position + direction * radius;

            m_CurrentPlacementIndex++;
        }

        public GameObject GetGameObject()
        {
            return m_GameObject;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            m_GameObject = gameObj;

            bool successfullySetDegreesBetweenPlacement = true;

            if (m_DegreesBetweenPlacement != null)
            {
                successfullySetDegreesBetweenPlacement = m_DegreesBetweenPlacement.SetGameObject(GetPlacementObject());
            }

            return successfullySetDegreesBetweenPlacement;
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
                Debug.LogWarning($"{nameof(SpreadPlacement_CachedGameAction)} could not set center as self on radius score.");
                return 0f;
            }

            float radius = m_Radius.CalculateScore(placementObject);
            return Mathf.Max(0f, radius);
        }

        private float GetDegreesBetweenPlacement()
        {
            if (m_DegreesBetweenPlacement == null)
            {
                return 0f;
            }

            float degrees = m_DegreesBetweenPlacement.CalculateScore();

            if (float.IsNaN(degrees) || float.IsInfinity(degrees))
            {
                return 0f;
            }

            return Mathf.Max(0f, degrees);
        }

        private Vector3 GetPlacementDirection(
            Transform center,
            int placementIndex,
            float degreesBetweenPlacement)
        {
            GetPlacementAxes(
                center,
                out Vector3 baseForward,
                out Vector3 rotationAxis);

            float signedAngle = GetSignedAngleForPlacementIndex(
                placementIndex,
                degreesBetweenPlacement);

            Vector3 direction = Quaternion.AngleAxis(signedAngle, rotationAxis) * baseForward;

            if (direction.sqrMagnitude <= 0.000001f)
            {
                return baseForward.normalized;
            }

            return direction.normalized;
        }

        private void GetPlacementAxes(
            Transform center,
            out Vector3 baseForward,
            out Vector3 rotationAxis)
        {
            if (m_PlacementSpace == Space.Self && center != null)
            {
                baseForward = center.forward;
                rotationAxis = center.up;
            }
            else
            {
                baseForward = Vector3.forward;
                rotationAxis = Vector3.up;
            }

            if (baseForward.sqrMagnitude <= 0.000001f)
            {
                baseForward = Vector3.forward;
            }

            if (rotationAxis.sqrMagnitude <= 0.000001f)
            {
                rotationAxis = Vector3.up;
            }

            baseForward.Normalize();
            rotationAxis.Normalize();
        }

        private float GetSignedAngleForPlacementIndex(
            int placementIndex,
            float degreesBetweenPlacement)
        {
            placementIndex = Mathf.Max(0, placementIndex);

            switch (m_PlacementProtocol)
            {
                case PlacementProtocol.LeftThenRight:
                    return GetAlternatingSignedAngle(
                        placementIndex,
                        degreesBetweenPlacement,
                        startLeft: true);

                case PlacementProtocol.RightThenLeft:
                    return GetAlternatingSignedAngle(
                        placementIndex,
                        degreesBetweenPlacement,
                        startLeft: false);

                case PlacementProtocol.OnlyLeft:
                    return -degreesBetweenPlacement * placementIndex;

                case PlacementProtocol.OnlyRight:
                    return degreesBetweenPlacement * placementIndex;

                default:
                    return 0f;
            }
        }

        private static float GetAlternatingSignedAngle(
            int placementIndex,
            float degreesBetweenPlacement,
            bool startLeft)
        {
            if (placementIndex <= 0)
            {
                return 0f;
            }

            int step = (placementIndex + 1) / 2;
            bool isFirstSideOfPair = placementIndex % 2 == 1;

            bool placeLeft = startLeft
                ? isFirstSideOfPair
                : !isFirstSideOfPair;

            float sign = placeLeft ? -1f : 1f;
            return sign * degreesBetweenPlacement * step;
        }
    }
}