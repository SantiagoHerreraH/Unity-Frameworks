using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace SilverPillar.Core
{
    public enum VectorFromAxis
    {
        xyz,
        xz,
        yz,
        xy,
        x,
        y,
        z
    }

    public class DistanceComparer
    {


        public static float CalculateDistance(VectorFromAxis whatDistanceAxisToCompare, Vector3 firstPos, Vector3 secondPos)
        {
            Vector3 diff = firstPos - secondPos;
            float distance = 0;

            switch (whatDistanceAxisToCompare)
            {
                case VectorFromAxis.xyz:
                    distance = diff.magnitude;
                    break;
                case VectorFromAxis.xz:
                    distance = new Vector3(diff.x, 0, diff.z).magnitude;
                    break;
                case VectorFromAxis.yz:
                    distance = new Vector3(0, diff.y, diff.z).magnitude;
                    break;
                case VectorFromAxis.xy:
                    distance = new Vector3(diff.x, diff.y, 0).magnitude;
                    break;
                case VectorFromAxis.x:
                    distance = diff.x;
                    break;
                case VectorFromAxis.y:
                    distance = diff.y;
                    break;
                case VectorFromAxis.z:
                    distance = diff.z;
                    break;
                default:
                    break;
            }

            return distance;
        }
        public static float CalculateSquareDistance(VectorFromAxis whatDistanceAxisToCompare, Vector3 first, Vector3 second)
        {
            Vector3 diff = first - second;
            float distanceSqr = 0;

            switch (whatDistanceAxisToCompare)
            {
                case VectorFromAxis.xyz:
                    distanceSqr = diff.sqrMagnitude;
                    break;
                case VectorFromAxis.xz:
                    distanceSqr = new Vector3(diff.x, 0, diff.z).sqrMagnitude;
                    break;
                case VectorFromAxis.yz:
                    distanceSqr = new Vector3(0, diff.y, diff.z).sqrMagnitude;
                    break;
                case VectorFromAxis.xy:
                    distanceSqr = new Vector3(diff.x, diff.y, 0).sqrMagnitude;
                    break;
                case VectorFromAxis.x:
                    distanceSqr = diff.x * diff.x;
                    break;
                case VectorFromAxis.y:
                    distanceSqr = diff.y * diff.y;
                    break;
                case VectorFromAxis.z:
                    distanceSqr = diff.z * diff.z;
                    break;
                default:
                    break;
            }

            return distanceSqr;
        }

    }

    [Serializable]
    public class Distance_InteractionCondition : IInteractionCondition
    {

        [Title("What Distance Axis to compare")]
        [SerializeField]
        private VectorFromAxis m_WhatAxisToCompare = VectorFromAxis.xyz;

        [Title("Comparison")]
        [SerializeField]
        private FloatComparison.OperationType m_IfDistanceIs;
        [OdinSerialize, ShowInInspector] private ICachedScore m_ThanDistanceToCompare;
        [SerializeField]
        private float m_ValueIfNullScore = 0f;

        
        private GameObject m_Self;

        public IInteractionCondition Clone()
        {
            return new Distance_InteractionCondition
            {
                m_ThanDistanceToCompare = this.m_ThanDistanceToCompare.Clone(),
                m_Self = this.m_Self
            };
        }

        public GameObject GetGameObject() => m_Self;

        public bool SetGameObject(GameObject self)
        {
            m_ThanDistanceToCompare?.SetGameObject(self);
            m_Self = self;
            return m_Self != null;
        }

        public bool IsFulfilled(GameObject target)
        {
            if (m_Self == null || target == null) return false;

            float distanceSqr = 0;
            Vector3 diff = m_Self.transform.position - target.transform.position;

            switch (m_WhatAxisToCompare)
            {
                case VectorFromAxis.xyz:
                    distanceSqr = diff.sqrMagnitude;
                    break;
                case VectorFromAxis.xz:
                    distanceSqr = new Vector3(diff.x, 0, diff.z).sqrMagnitude;
                    break;
                case VectorFromAxis.yz:
                    distanceSqr = new Vector3(0, diff.y, diff.z).sqrMagnitude;
                    break;
                case VectorFromAxis.xy:
                    distanceSqr = new Vector3(diff.x, diff.y, 0).sqrMagnitude;
                    break;
                case VectorFromAxis.x:
                    distanceSqr = diff.x * diff.x;
                    break;
                case VectorFromAxis.y:
                    distanceSqr = diff.y * diff.y;
                    break;
                case VectorFromAxis.z:
                    distanceSqr = diff.z * diff.z;
                    break;
                default:
                    break;
            }

            float score = m_ThanDistanceToCompare == null ? m_ValueIfNullScore : m_ThanDistanceToCompare.CalculateScore();

            return FloatComparison.Compare(distanceSqr, m_IfDistanceIs, score * score);
        }

    }
}
