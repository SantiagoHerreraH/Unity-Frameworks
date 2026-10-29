using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
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
            public SeeSenseTargetQuality SeeSenseTargetQuality;
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
        //the fileds below are the ones that are null at runtime
        private Dictionary<SenseTarget, int> m_RegisteredTargets_To_Index = new();
        private List<RegisteredData> m_Targets = new();
        private SensedInformation m_SensedInformation = new();
        private List<Collider> m_ValidColliders = new();

        // Reused for raycasts, avoiding a new RaycastHit[] for every visibility test.
        // Grows as needed so that no raycast hits are ever silently discarded.
        [NonSerialized]
        private RaycastHit[] m_RaycastHits = new RaycastHit[16];

        private sealed class RaycastHitDistanceComparer : IComparer<RaycastHit>
        {
            public static readonly RaycastHitDistanceComparer Instance = new();

            public int Compare(RaycastHit a, RaycastHit b)
            {
                return a.distance.CompareTo(b.distance);
            }
        }

        [OnDeserialized]
        private void InitializeRuntimeFields()
        {
            m_RegisteredTargets_To_Index ??= new Dictionary<SenseTarget, int>();
            m_Targets ??= new List<RegisteredData>();
            m_SensedInformation ??= new SensedInformation();
            m_ValidColliders ??= new List<Collider>();
            m_RaycastHits ??= new RaycastHit[16];
        }

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
                // Read each RegisteredData struct only once from the list.
                RegisteredData registeredData = m_Targets[i];
                SenseTarget target = registeredData.SenseTarget;

                if (!IsAllowedToSeeTarget(target))
                {
                    continue;
                }

                if (CanSeeTarget(registeredData.Colliders, forward))
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
            if (m_SenseController.gameObject == senseTarget.gameObject ||
                senseTarget == null ||
                m_RegisteredTargets_To_Index.ContainsKey(senseTarget) ||
                m_SenseController.transform.IsChildOf(senseTarget.transform) ||
                senseTarget.transform.IsChildOf(m_SenseController.transform))
            {
                return false;
            }

            SeeSenseTargetQuality targetQuality = senseTarget.GetSenseQuality<SeeSenseTargetQuality>();

            if (targetQuality == null || !targetQuality.CanBeSeen())
            {
                return false;
            }

            Collider[] colliders = senseTarget.GetComponentsInChildren<Collider>(true);
            if (colliders == null || colliders.Length == 0)
            {
                return false;
            }

            m_RegisteredTargets_To_Index.Add(senseTarget, m_Targets.Count);
            m_Targets.Add(new RegisteredData { SenseTarget = senseTarget, SeeSenseTargetQuality = targetQuality, Colliders = colliders });
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

            int validColliderCount = m_ValidColliders.Count;
            if (validColliderCount == 0)
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
                float halfAngle = eyeField.Angle * 0.5f;

                for (int colliderIndex = 0; colliderIndex < validColliderCount; colliderIndex++)
                {
                    Collider targetCollider = m_ValidColliders[colliderIndex];
                    Vector3 targetPoint = targetCollider.ClosestPoint(eyePosition);
                    Vector3 toTarget = targetPoint - eyePosition;
                    float distance = toTarget.magnitude;

                    // Preserve the original immediate-success rule, even when
                    // the collider does not directly have a SenseTarget component.
                    if (distance <= Mathf.Epsilon)
                    {
                        return true;
                    }

                    if (distance > eyeField.Distance)
                    {
                        continue;
                    }

                    Vector3 direction = toTarget / distance;

                    // Keep Vector3.Angle to preserve threshold/rounding behavior.
                    if (Vector3.Angle(forward, direction) > halfAngle)
                    {
                        continue;
                    }

                    // The original HasLineOfSight required a SenseTarget directly
                    // on this collider's GameObject (NOT in a parent). Resolve it
                    // before the expensive physics query without changing results.
                    SenseTarget targetSenseTarget = targetCollider.GetComponent<SenseTarget>();
                    if (targetSenseTarget == null)
                    {
                        continue;
                    }

                    if (HasLineOfSight(eyePosition, direction, distance, targetSenseTarget))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool HasLineOfSight(Vector3 origin, Vector3 direction, float targetDistance, SenseTarget targetSenseTarget)
        {
            const float raycastExtraDistance = 0.05f;

            // Runtime fields may not be initialized by every serialization path.
            m_RaycastHits ??= new RaycastHit[16];

            int hitCount;
            do
            {
                hitCount = Physics.RaycastNonAlloc(
                    origin,
                    direction,
                    m_RaycastHits,
                    targetDistance + raycastExtraDistance,
                    m_EyeRaycastLayer,
                    QueryTriggerInteraction.Ignore);

                // Physics.RaycastNonAlloc can return a truncated set of hits when
                // the supplied buffer is full. Grow and repeat until complete.
                if (hitCount < m_RaycastHits.Length)
                {
                    break;
                }

                Array.Resize(ref m_RaycastHits, m_RaycastHits.Length * 2);
            }
            while (true);

            if (hitCount == 0)
            {
                return false;
            }

            // Same distance ordering as Physics.RaycastAll + Array.Sort.
            if (hitCount > 1)
            {
                Array.Sort(m_RaycastHits, 0, hitCount, RaycastHitDistanceComparer.Instance);
            }

            Transform selfTransform = m_SenseController != null
                ? m_SenseController.transform
                : null;

            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider = m_RaycastHits[i].collider;
                if (hitCollider == null)
                {
                    continue;
                }

                // Ignore colliders belonging to the sensing object.
                if (selfTransform != null && hitCollider.transform.IsChildOf(selfTransform))
                {
                    continue;
                }

                // Preserve GetComponent (not GetComponentInParent) semantics.
                SenseTarget hitSenseTarget = hitCollider.GetComponent<SenseTarget>();
                if (hitSenseTarget == null || !hitSenseTarget.CanBeSensed())
                {
                    continue;
                }

                SeeSenseTargetQuality quality = hitSenseTarget.GetSenseQuality<SeeSenseTargetQuality>();
                if (quality == null || !quality.CanBeSensed())
                {
                    continue;
                }

                if (hitSenseTarget == targetSenseTarget)
                {
                    return true;
                }

                if (quality.CanBlockSight())
                {
                    return false;
                }
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
