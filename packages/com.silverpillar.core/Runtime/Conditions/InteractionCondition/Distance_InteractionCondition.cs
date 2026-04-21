using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Core
{
    public enum AxisFilter
    {
        xyz,
        xz,
        yz,
        xy,
        x,
        y,
        z
    }

    public class AxisFilterTools
    {
        public static float CalculateDistance(AxisFilter whatDistanceAxisToCompare, Vector3 firstPos, Vector3 secondPos)
        {
            Vector3 diff = firstPos - secondPos;
            float distance = 0;

            switch (whatDistanceAxisToCompare)
            {
                case AxisFilter.xyz:
                    distance = diff.magnitude;
                    break;
                case AxisFilter.xz:
                    distance = new Vector3(diff.x, 0, diff.z).magnitude;
                    break;
                case AxisFilter.yz:
                    distance = new Vector3(0, diff.y, diff.z).magnitude;
                    break;
                case AxisFilter.xy:
                    distance = new Vector3(diff.x, diff.y, 0).magnitude;
                    break;
                case AxisFilter.x:
                    distance = diff.x;
                    break;
                case AxisFilter.y:
                    distance = diff.y;
                    break;
                case AxisFilter.z:
                    distance = diff.z;
                    break;
                default:
                    break;
            }

            return distance;
        }
        public static float CalculateSquareDistance(AxisFilter whatDistanceAxisToCompare, Vector3 first, Vector3 second)
        {
            Vector3 diff = first - second;
            float distanceSqr = 0;

            switch (whatDistanceAxisToCompare)
            {
                case AxisFilter.xyz:
                    distanceSqr = diff.sqrMagnitude;
                    break;
                case AxisFilter.xz:
                    distanceSqr = new Vector3(diff.x, 0, diff.z).sqrMagnitude;
                    break;
                case AxisFilter.yz:
                    distanceSqr = new Vector3(0, diff.y, diff.z).sqrMagnitude;
                    break;
                case AxisFilter.xy:
                    distanceSqr = new Vector3(diff.x, diff.y, 0).sqrMagnitude;
                    break;
                case AxisFilter.x:
                    distanceSqr = diff.x * diff.x;
                    break;
                case AxisFilter.y:
                    distanceSqr = diff.y * diff.y;
                    break;
                case AxisFilter.z:
                    distanceSqr = diff.z * diff.z;
                    break;
                default:
                    break;
            }

            return distanceSqr;
        }

        public static Vector3 GetFilteredVector(Vector3 dir, AxisFilter vectorFromAxis)
        {
            switch (vectorFromAxis)
            {
                case AxisFilter.xz: return new Vector3(dir.x, 0, dir.z);
                case AxisFilter.yz: return new Vector3(0, dir.y, dir.z);
                case AxisFilter.xy: return new Vector3(dir.x, dir.y, 0);
                case AxisFilter.x: return new Vector3(dir.x, 0, 0);
                case AxisFilter.y: return new Vector3(0, dir.y, 0);
                case AxisFilter.z: return new Vector3(0, 0, dir.z);
                default: return dir; // xyz
            }
        }

    }

    [Serializable]
    public class Distance_InteractionCondition : IInteractionCondition
    {

        [Title("What Distance Axis to compare")]
        [SerializeField]
        private AxisFilter m_WhatAxisToCompare = AxisFilter.xyz;

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
                case AxisFilter.xyz:
                    distanceSqr = diff.sqrMagnitude;
                    break;
                case AxisFilter.xz:
                    distanceSqr = new Vector3(diff.x, 0, diff.z).sqrMagnitude;
                    break;
                case AxisFilter.yz:
                    distanceSqr = new Vector3(0, diff.y, diff.z).sqrMagnitude;
                    break;
                case AxisFilter.xy:
                    distanceSqr = new Vector3(diff.x, diff.y, 0).sqrMagnitude;
                    break;
                case AxisFilter.x:
                    distanceSqr = diff.x * diff.x;
                    break;
                case AxisFilter.y:
                    distanceSqr = diff.y * diff.y;
                    break;
                case AxisFilter.z:
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
