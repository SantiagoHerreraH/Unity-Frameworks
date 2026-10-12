using System;
using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Senses
{
    [Serializable]
    public class SeeSense : ISense
    {
        [Serializable]
        public struct EyeField
        {
            public float ForwardOffsetFromTransform;
            public float Distance;

            [Range(1, 360)]
            public float Angle;
        }

        public struct RegisteredData
        {
            public SenseTarget SenseTarget;
            public Collider[] Colliders; 
        }
        [SerializeField]
        private SensingPermissions m_SensingPermissions;
        [SerializeField]
        private bool m_DrawSense = false;
        [SerializeField]
        private Transform m_EyeTransform;
        [SerializeField]
        private Vector3 m_ForwardEyeVector = Vector3.forward;
        [SerializeField]
        private LayerMask m_EyeRaycastLayer = ~0;
        [SerializeField]
        private List<EyeField> m_EyeFields;

        private SenseController m_SenseController;
        private readonly Dictionary<SenseTarget, int> m_RegisteredTargets_To_Index = new();
        private readonly List<RegisteredData> m_Targets = new();
        private readonly SensedInformation m_SensedInformation = new();
        private readonly List<Collider> m_ValidColliders = new();

        public override SensedInformation CalculateSensed()
        {
            m_SensedInformation.Clear();
            m_SensedInformation.EnsureCapacity(m_RegisteredTargets_To_Index.Count);

            if (!m_CanSense || m_EyeTransform == null || m_EyeFields == null || m_EyeFields.Count == 0)
            {
                return m_SensedInformation;
            }

            Vector3 forward = m_EyeTransform.TransformDirection(m_ForwardEyeVector);
            if (forward.sqrMagnitude <= Mathf.Epsilon)
            {
                forward = m_EyeTransform.forward;
            }
            forward.Normalize();

            for (int i = 0; i < m_Targets.Count; i++)
            {
                SenseTarget target = m_Targets[i].SenseTarget;
                if (!IsAllowedToSeeTarget(target))
                {
                    continue;
                }

                bool sensed = CanSeeTarget(m_Targets[i].Colliders, forward);
                if (sensed)
                {
                    m_SensedInformation.Sensed.Add(target);
                }
                else
                {
                    m_SensedInformation.NotSensed.Add(target);
                }

            }

            return m_SensedInformation;
        }

        public override bool RegisterSenseTarget(SenseTarget senseTarget)
        {
            if (senseTarget == null || m_RegisteredTargets_To_Index.ContainsKey(senseTarget))
            {
                return false;
            }

            Collider[] colliders = senseTarget.GetComponentsInChildren<Collider>(true);
            if (colliders == null || colliders.Length == 0)
            {
                return false;
            }

            m_RegisteredTargets_To_Index.Add(senseTarget, m_Targets.Count);
            m_Targets.Add(new RegisteredData { SenseTarget = senseTarget, Colliders = colliders });
            return true;
        }

        public override void SetSenseController(SenseController senseController)
        {
            m_SenseController = senseController;

            if (m_EyeTransform == null && m_SenseController != null)
            {
                m_EyeTransform = m_SenseController.transform;
            }
        }

        public override void UnregisterAll()
        {
            m_RegisteredTargets_To_Index.Clear();
            m_SensedInformation.Clear();
            m_Targets.Clear();
        }

        public override void UnregisterSenseTarget(SenseTarget senseTarget)
        {
            if (senseTarget == null)
            {
                return;
            }

            if (!senseTarget.HasSenseQualityType(SenseTargetQualityNeededToSense()))
            {
                return;
            }

            if (!m_RegisteredTargets_To_Index.TryGetValue(senseTarget, out int index))
            {
                return;
            }

            int lastIndex = m_Targets.Count - 1;

            if (index != lastIndex)
            {
                RegisteredData lastTargetData = m_Targets[lastIndex];

                m_Targets[index] = lastTargetData;

                m_RegisteredTargets_To_Index[lastTargetData.SenseTarget] = index;
            }

            m_Targets.RemoveAt(lastIndex);

            m_RegisteredTargets_To_Index.Remove(senseTarget);
            m_SensedInformation.Sensed?.Remove(senseTarget);
            m_SensedInformation.NotSensed?.Remove(senseTarget);
        }

        private bool IsAllowedToSeeTarget(SenseTarget target)
        {
            if (target == null || !target.isActiveAndEnabled)
            {
                return false;
            }

            switch (m_SensingPermissions)
            {
                case SensingPermissions.DontSenseIfSenseTargetHiddenOrSenseTargetQualityHidden:
                    return target.CanBeSensed(SenseTargetQualityNeededToSense());
                case SensingPermissions.DontSenseIfSenseTargetHidden: 
                    return target.CanBeSensed();
                case SensingPermissions.SenseEvenIfSenseTargetHidden:
                    break;
            }

            return true;
        }

        private bool CanSeeTarget(Collider[] targetColliders, Vector3 forward)
        {
            m_ValidColliders.Clear();
            for (int i = 0; i < targetColliders.Length; i++)
            {
                Collider collider = targetColliders[i];
                if (collider != null && collider.enabled && collider.gameObject.activeInHierarchy)
                {
                    m_ValidColliders.Add(collider);
                }
            }

            if (m_ValidColliders.Count == 0)
            {
                return false;
            }

            for (int fieldIndex = 0; fieldIndex < m_EyeFields.Count; fieldIndex++)
            {
                EyeField eyeField = m_EyeFields[fieldIndex];
                if (eyeField.Distance <= 0f || eyeField.Angle <= 0f)
                {
                    continue;
                }

                Vector3 eyePosition = m_EyeTransform.position + forward * eyeField.ForwardOffsetFromTransform;

                for (int colliderIndex = 0; colliderIndex < m_ValidColliders.Count; colliderIndex++)
                {
                    Collider targetCollider = m_ValidColliders[colliderIndex];
                    Vector3 targetPoint = targetCollider.ClosestPoint(eyePosition);
                    Vector3 toTarget = targetPoint - eyePosition;
                    float distance = toTarget.magnitude;

                    if (distance <= Mathf.Epsilon)
                    {
                        return true;
                    }

                    if (distance > eyeField.Distance)
                    {
                        continue;
                    }

                    Vector3 direction = toTarget / distance;
                    if (Vector3.Angle(forward, direction) > eyeField.Angle * 0.5f)
                    {
                        continue;
                    }

                    if (HasLineOfSight(eyePosition, direction, distance, targetCollider))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool HasLineOfSight(Vector3 origin, Vector3 direction, float targetDistance, Collider targetCollider)
        {
            const float raycastExtraDistance = 0.01f;
            RaycastHit[] hits = Physics.RaycastAll(
                origin,
                direction,
                targetDistance + raycastExtraDistance,
                m_EyeRaycastLayer,
                QueryTriggerInteraction.Ignore);

            if (hits.Length == 0)
            {
                return false;
            }

            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                Collider hitCollider = hits[i].collider;
                if (hitCollider == null)
                {
                    continue;
                }

                // Ignore the sensing object's own colliders.
                if (m_SenseController != null && hitCollider.transform.IsChildOf(m_SenseController.transform))
                {
                    continue;
                }

                return hitCollider == targetCollider || hitCollider.transform.IsChildOf(targetCollider.transform);
            }

            return false;
        }

        public override Type SenseTargetQualityNeededToSense()
        {
            return typeof(SeeSenseTargetQuality);
        }

        public override void Draw()
        {
            if (!m_DrawSense || m_EyeTransform == null || m_EyeFields == null || m_EyeFields.Count == 0)
            {
                return;
            }

            Vector3 forward = m_EyeTransform.TransformDirection(m_ForwardEyeVector);

            if (forward.sqrMagnitude <= Mathf.Epsilon)
            {
                forward = m_EyeTransform.forward;
            }

            forward.Normalize();

            Color previousColor = Gizmos.color;
            Gizmos.color = Color.yellow;

            const int circleSegments = 32;
            const int arcSegments = 16;

            // Find two perpendicular vectors to forward.
            Vector3 right = Vector3.Cross(forward, Vector3.up);

            if (right.sqrMagnitude <= Mathf.Epsilon)
            {
                right = Vector3.Cross(forward, Vector3.right);
            }

            right.Normalize();

            Vector3 up = Vector3.Cross(right, forward).normalized;

            for (int fieldIndex = 0; fieldIndex < m_EyeFields.Count; fieldIndex++)
            {
                EyeField eyeField = m_EyeFields[fieldIndex];

                if (eyeField.Distance <= 0f || eyeField.Angle <= 0f)
                {
                    continue;
                }

                Vector3 eyePosition =
                    m_EyeTransform.position +
                    forward * eyeField.ForwardOffsetFromTransform;

                float halfAngle = eyeField.Angle * 0.5f;

                // Draw eye position.
                Gizmos.DrawWireSphere(
                    eyePosition,
                    Mathf.Max(0.025f, eyeField.Distance * 0.01f));

                // 360 degrees means the sense covers the entire sphere.
                if (eyeField.Angle >= 359.9f)
                {
                    Gizmos.DrawWireSphere(eyePosition, eyeField.Distance);
                    continue;
                }

                // Draw the forward direction.
                Gizmos.DrawLine(
                    eyePosition,
                    eyePosition + forward * eyeField.Distance);

                // Draw rays around the outer boundary of the vision cone.
                Vector3 firstBoundaryPoint = Vector3.zero;
                Vector3 previousBoundaryPoint = Vector3.zero;

                for (int i = 0; i <= circleSegments; i++)
                {
                    float rotationAroundForward =
                        (360f / circleSegments) * i;

                    Vector3 radialAxis =
                        Quaternion.AngleAxis(rotationAroundForward, forward) * up;

                    Vector3 boundaryDirection =
                        Quaternion.AngleAxis(halfAngle, radialAxis) * forward;

                    boundaryDirection.Normalize();

                    Vector3 boundaryPoint =
                        eyePosition +
                        boundaryDirection * eyeField.Distance;

                    if (i == 0)
                    {
                        firstBoundaryPoint = boundaryPoint;
                    }
                    else
                    {
                        Gizmos.DrawLine(
                            previousBoundaryPoint,
                            boundaryPoint);
                    }

                    // Draw some of the lines from the eye to the cone edge.
                    // Drawing every line would make the gizmo unnecessarily dense.
                    if (i < circleSegments && i % 4 == 0)
                    {
                        Gizmos.DrawLine(
                            eyePosition,
                            boundaryPoint);
                    }

                    previousBoundaryPoint = boundaryPoint;
                }

                // Close the boundary circle.
                Gizmos.DrawLine(previousBoundaryPoint, firstBoundaryPoint);

                // Draw four arcs from forward towards the edge.
                for (int side = 0; side < 4; side++)
                {
                    float rotationAroundForward = 90f * side;

                    Vector3 radialAxis =
                        Quaternion.AngleAxis(rotationAroundForward, forward) * up;

                    Vector3 previousPoint =
                        eyePosition + forward * eyeField.Distance;

                    for (int arcIndex = 1; arcIndex <= arcSegments; arcIndex++)
                    {
                        float t = arcIndex / (float)arcSegments;
                        float currentAngle = halfAngle * t;

                        Vector3 direction =
                            Quaternion.AngleAxis(currentAngle, radialAxis) * forward;

                        Vector3 currentPoint =
                            eyePosition +
                            direction.normalized * eyeField.Distance;

                        Gizmos.DrawLine(previousPoint, currentPoint);

                        previousPoint = currentPoint;
                    }
                }
            }

            Gizmos.color = previousColor;
        }
    }
}
