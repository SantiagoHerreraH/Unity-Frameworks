using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using SilverPillar.Core;


#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

using Vector3 = UnityEngine.Vector3;
using Quaternion = UnityEngine.Quaternion;
using Sirenix.Serialization;
using System.Runtime.InteropServices;

namespace SilverPillar.Spline
{
    [ExecuteAlways]
    public class SplineModifier : SerializedMonoBehaviour
    {
        [Serializable]
        public struct KnotData
        {
            [NonSerialized, HideInInspector]
            private SplineContainer m_SplineContainer;

            [SerializeField, PropertyRange(0, nameof(MaxSplineIndex))]
            private int m_SplineIndex;

            [SerializeField, PropertyRange(0, nameof(MaxKnotIndex))]
            private int m_KnotIndex;

            [SerializeField]
            private TangentMode m_TangentMode;

            [SerializeField, ShowIf(nameof(UsesAutoSmooth)), Range(0f, 1f)]
            private float m_AutoSmoothTension;

            [SerializeField, ShowIf(nameof(UsesMainTangent))]
            private BezierTangent m_MainTangent;

            [SerializeField]
            private Transform m_Center;

            [SerializeField, Tooltip("AKA left tangent handle"), ShowIf(nameof(UsesManualTangents))]
            private Transform m_InTangent;

            [SerializeField, Tooltip("AKA right tangent handle"), ShowIf(nameof(UsesManualTangents))]
            private Transform m_OutTangent;

            private bool UsesManualTangents =>
                m_TangentMode != TangentMode.AutoSmooth &&
                m_TangentMode != TangentMode.Linear;

            private bool UsesAutoSmooth => m_TangentMode == TangentMode.AutoSmooth;

            private bool UsesMainTangent =>
                m_TangentMode == TangentMode.Continuous ||
                m_TangentMode == TangentMode.Mirrored;

            private int MaxSplineIndex =>
                m_SplineContainer == null || m_SplineContainer.Splines == null
                    ? 0
                    : Mathf.Max(0, m_SplineContainer.Splines.Count - 1);

            private int MaxKnotIndex
            {
                get
                {
                    if (!TryGetSpline(out UnityEngine.Splines.Spline spline))
                    {
                        return 0;
                    }

                    return Mathf.Max(0, spline.Count - 1);
                }
            }

            public KnotData(SplineContainer splineContainer)
                : this(splineContainer, 0, 0)
            {
            }

            public KnotData(SplineContainer splineContainer, int splineIndex, int knotIndex)
            {
                m_SplineContainer = splineContainer;
                m_SplineIndex = splineIndex;
                m_KnotIndex = knotIndex;
                m_TangentMode = TangentMode.AutoSmooth;
                m_AutoSmoothTension = SplineUtility.DefaultTension;
                m_MainTangent = BezierTangent.Out;
                m_Center = null;
                m_InTangent = null;
                m_OutTangent = null;

                ClampIndices();
            }

            public void SetSplineContainer(SplineContainer splineContainer)
            {
                m_SplineContainer = splineContainer;
            }

            public void ClampIndices()
            {
                m_SplineIndex = Mathf.Clamp(m_SplineIndex, 0, MaxSplineIndex);
                m_KnotIndex = Mathf.Clamp(m_KnotIndex, 0, MaxKnotIndex);
                m_AutoSmoothTension = Mathf.Clamp01(m_AutoSmoothTension);
            }
            public bool Matches(int splineIndex, int knotIndex)
            {
                return m_SplineIndex == splineIndex && m_KnotIndex == knotIndex;
            }

            public void TranslateAssignedTransforms(Vector3 worldDelta)
            {
                if (m_Center != null)
                {
                    m_Center.position += worldDelta;
                }

                if (m_InTangent != null)
                {
                    m_InTangent.position += worldDelta;
                }

                if (m_OutTangent != null)
                {
                    m_OutTangent.position += worldDelta;
                }
            }
            public bool TryApply()
            {
                if (m_SplineContainer == null || m_Center == null)
                {
                    return false;
                }

                if (!TryGetSpline(out UnityEngine.Splines.Spline spline))
                {
                    return false;
                }

                ClampIndices();

                BezierKnot currentKnot = spline[m_KnotIndex];
                TangentMode currentMode = spline.GetTangentMode(m_KnotIndex);

                BezierKnot targetKnot = currentKnot;
                targetKnot.Position = GetLocalPosition(m_SplineContainer, m_Center);
                targetKnot.Rotation = GetLocalRotation(m_SplineContainer, m_Center);

                ApplyTangentsToTargetKnot(ref targetKnot, currentKnot);

                bool changed =
                    currentMode != m_TangentMode ||
                    !Approximately(currentKnot, targetKnot);

                if (m_TangentMode == TangentMode.AutoSmooth)
                {
                    changed |= !Mathf.Approximately(
                        spline.GetAutoSmoothTension(m_KnotIndex),
                        m_AutoSmoothTension
                    );
                }

                if (!changed)
                {
                    return false;
                }

                spline.SetKnot(m_KnotIndex, targetKnot, m_MainTangent);
                spline.SetTangentMode(m_KnotIndex, m_TangentMode, m_MainTangent);

                if (m_TangentMode == TangentMode.AutoSmooth)
                {
                    spline.SetAutoSmoothTension(m_KnotIndex, m_AutoSmoothTension);
                }

                return true;
            }

            public bool TrySnapTransformsToSpline()
            {
                if (m_SplineContainer == null)
                {
                    return false;
                }

                if (!TryGetSpline(out UnityEngine.Splines.Spline spline))
                {
                    return false;
                }

                ClampIndices();

                BezierKnot knot = spline[m_KnotIndex];
                m_TangentMode = spline.GetTangentMode(m_KnotIndex);

                if (m_TangentMode == TangentMode.AutoSmooth)
                {
                    m_AutoSmoothTension = spline.GetAutoSmoothTension(m_KnotIndex);
                }

                Transform containerTransform = m_SplineContainer.transform;

                Vector3 centerWorld = containerTransform.TransformPoint(ToVector3(knot.Position));
                Vector3 leftWorld = containerTransform.TransformPoint(ToVector3(knot.Position + knot.TangentIn));
                Vector3 rightWorld = containerTransform.TransformPoint(ToVector3(knot.Position + knot.TangentOut));

                Quaternion centerRotation = containerTransform.rotation * ToQuaternion(knot.Rotation);

                if (m_Center != null)
                {
                    m_Center.SetPositionAndRotation(centerWorld, centerRotation);
                }

                if (m_InTangent != null)
                {
                    m_InTangent.position = leftWorld;
                }

                if (m_OutTangent != null)
                {
                    m_OutTangent.position = rightWorld;
                }

                return true;
            }

            public bool TryCreateMissingTransforms(Transform parent)
            {
                if (m_SplineContainer == null || parent == null)
                {
                    return false;
                }

                if (!TryGetSpline(out UnityEngine.Splines.Spline spline))
                {
                    return false;
                }

                ClampIndices();

                bool createdAny = false;

                if (m_Center == null)
                {
                    m_Center = CreateChild(parent, $"Spline {m_SplineIndex} Knot {m_KnotIndex} Center");
                    createdAny = true;
                }

                if (UsesManualTangents && m_InTangent == null)
                {
                    m_InTangent = CreateChild(parent, $"Spline {m_SplineIndex} Knot {m_KnotIndex} Left");
                    createdAny = true;
                }

                if (UsesManualTangents && m_OutTangent == null)
                {
                    m_OutTangent = CreateChild(parent, $"Spline {m_SplineIndex} Knot {m_KnotIndex} Right");
                    createdAny = true;
                }

                TrySnapTransformsToSpline();
                return createdAny;
            }

            private void ApplyTangentsToTargetKnot(ref BezierKnot targetKnot, BezierKnot currentKnot)
            {
                if (m_TangentMode == TangentMode.AutoSmooth)
                {
                    targetKnot.TangentIn = currentKnot.TangentIn;
                    targetKnot.TangentOut = currentKnot.TangentOut;
                    return;
                }

                if (m_TangentMode == TangentMode.Linear)
                {
                    targetKnot.TangentIn = float3.zero;
                    targetKnot.TangentOut = float3.zero;
                    return;
                }

                if (m_TangentMode == TangentMode.Broken)
                {
                    if (m_InTangent != null)
                    {
                        targetKnot.TangentIn = GetLocalTangent(m_SplineContainer, m_InTangent, targetKnot.Position);
                    }

                    if (m_OutTangent != null)
                    {
                        targetKnot.TangentOut = GetLocalTangent(m_SplineContainer, m_OutTangent, targetKnot.Position);
                    }

                    return;
                }

                if (UsesMainTangent)
                {
                    if (m_MainTangent == BezierTangent.In && m_InTangent != null)
                    {
                        targetKnot.TangentIn = GetLocalTangent(m_SplineContainer, m_InTangent, targetKnot.Position);
                    }
                    else if (m_MainTangent == BezierTangent.Out && m_OutTangent != null)
                    {
                        targetKnot.TangentOut = GetLocalTangent(m_SplineContainer, m_OutTangent, targetKnot.Position);
                    }
                }
            }

            private bool TryGetSpline(out UnityEngine.Splines.Spline spline)
            {
                spline = null;

                if (m_SplineContainer == null || m_SplineContainer.Splines == null)
                {
                    return false;
                }

                if (m_SplineContainer.Splines.Count == 0)
                {
                    return false;
                }

                m_SplineIndex = Mathf.Clamp(m_SplineIndex, 0, m_SplineContainer.Splines.Count - 1);
                spline = m_SplineContainer.Splines[m_SplineIndex];

                if (spline == null || spline.Count == 0)
                {
                    return false;
                }

                m_KnotIndex = Mathf.Clamp(m_KnotIndex, 0, spline.Count - 1);
                return true;
            }


            private static Transform CreateChild(Transform parent, string name)
            {
                GameObject gameObject = new GameObject(name);

#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    Undo.RegisterCreatedObjectUndo(gameObject, $"Create {name}");
                }
#endif

                gameObject.transform.SetParent(parent, false);
                return gameObject.transform;
            }

            private static float3 GetLocalPosition(SplineContainer container, Transform point)
            {
                Vector3 localPosition = container.transform.InverseTransformPoint(point.position);
                return ToFloat3(localPosition);
            }

            private static float3 GetLocalTangent(SplineContainer container, Transform handle, float3 knotPosition)
            {
                return GetLocalPosition(container, handle) - knotPosition;
            }

            private static quaternion GetLocalRotation(SplineContainer container, Transform point)
            {
                Quaternion localRotation = Quaternion.Inverse(container.transform.rotation) * point.rotation;
                return ToQuaternion(localRotation);
            }

            private static bool Approximately(BezierKnot a, BezierKnot b)
            {
                const float positionTolerance = 0.0001f;
                const float rotationTolerance = 0.0001f;

                return math.lengthsq(a.Position - b.Position) <= positionTolerance * positionTolerance &&
                       math.lengthsq(a.TangentIn - b.TangentIn) <= positionTolerance * positionTolerance &&
                       math.lengthsq(a.TangentOut - b.TangentOut) <= positionTolerance * positionTolerance &&
                       math.lengthsq(a.Rotation.value - b.Rotation.value) <= rotationTolerance * rotationTolerance;
            }

            private static float3 ToFloat3(Vector3 value)
            {
                return new float3(value.x, value.y, value.z);
            }

            private static Vector3 ToVector3(float3 value)
            {
                return new Vector3(value.x, value.y, value.z);
            }

            private static quaternion ToQuaternion(Quaternion value)
            {
                return new quaternion(value.x, value.y, value.z, value.w);
            }

            private static Quaternion ToQuaternion(quaternion value)
            {
                return new Quaternion(value.value.x, value.value.y, value.value.z, value.value.w);
            }
        }
        [Serializable]
        public class LinkData
        {
            [NonSerialized, HideInInspector]
            private SplineContainer m_SplineContainer;

            [Title("Link Identity")]

            [SerializeField, PropertyRange(0, nameof(MaxSplineIndex))]
            private int m_SplineIndex;

            [SerializeField, PropertyRange(0, nameof(MaxKnotIndex))]
            private int m_FromKnotIndex;

            [SerializeField, PropertyRange(0, nameof(MaxKnotIndex))]
            private int m_ToKnotIndex;


            [Title("Constraints")]

            [OdinSerialize, ShowInInspector, Tooltip("If null, min distance is 0.")]
            private ICachedScore m_MinDistanceBetweenKnots;

            [OdinSerialize, ShowInInspector, Tooltip("If null, max distance is infinity.")]
            private ICachedScore m_MaxDistanceBetweenKnots;

            [SerializeField,
             Tooltip("If false, the max and min distance will be a straight line between knots. " +
                     "If true, the max and min distance will take into account the bend between knots.")]
            private bool m_LimitBendBasedOnMaxAndMinDistance;
            public bool LimitBendBasedOnMaxAndMinDistance => m_LimitBendBasedOnMaxAndMinDistance;

            [SerializeField, Min(2), ShowIf(nameof(m_LimitBendBasedOnMaxAndMinDistance))]
            private int m_SamplesPerCurve;

            public enum TransformMovementConstraints
            {
                OnlyApplyConstraintsWithoutConstrainingTransformMovement,
                ConstraintTransformMovementBasedOnConstraints
            }
            [Title("Transform Movement Constraints")]
            [SerializeField]
            private TransformMovementConstraints m_TransformMovementConstraints;

            public int SamplesPerCurve => m_SamplesPerCurve;

            public int SplineIndex => m_SplineIndex;
            public int FromKnotIndex => m_FromKnotIndex;
            public int ToKnotIndex => m_ToKnotIndex;

            public int StartKnotIndex => Mathf.Min(m_FromKnotIndex, m_ToKnotIndex);
            public int EndKnotIndex => Mathf.Max(m_FromKnotIndex, m_ToKnotIndex);

            public bool HasValidRange => m_FromKnotIndex != m_ToKnotIndex;

            private int MaxSplineIndex =>
                m_SplineContainer == null || m_SplineContainer.Splines == null
                    ? 0
                    : Mathf.Max(0, m_SplineContainer.Splines.Count - 1);

            public bool ShouldConstrainTransformMovement =>
            m_TransformMovementConstraints == TransformMovementConstraints.ConstraintTransformMovementBasedOnConstraints;

            private int MaxKnotIndex
            {
                get
                {
                    if (!TryGetSpline(out UnityEngine.Splines.Spline spline))
                    {
                        return 0;
                    }

                    return Mathf.Max(0, spline.Count - 1);
                }
            }

            public LinkData(SplineContainer splineContainer)
                : this(splineContainer, 0, 0, 1)
            {
            }

            public LinkData(SplineContainer splineContainer, int splineIndex, int fromKnotIndex, int toKnotIndex)
            {
                m_SplineContainer = splineContainer;
                m_SplineIndex = splineIndex;
                m_FromKnotIndex = fromKnotIndex;
                m_ToKnotIndex = toKnotIndex;
                m_MinDistanceBetweenKnots = null;
                m_MaxDistanceBetweenKnots = null;
                m_LimitBendBasedOnMaxAndMinDistance = false;
                m_SamplesPerCurve = 12;
                m_TransformMovementConstraints =
                    TransformMovementConstraints.OnlyApplyConstraintsWithoutConstrainingTransformMovement;

                ClampIndices();
            }

            public void SetSplineContainer(SplineContainer splineContainer)
            {
                m_SplineContainer = splineContainer;
            }

            public void ClampIndices()
            {
                m_SplineIndex = Mathf.Clamp(m_SplineIndex, 0, MaxSplineIndex);
                m_FromKnotIndex = Mathf.Clamp(m_FromKnotIndex, 0, MaxKnotIndex);
                m_ToKnotIndex = Mathf.Clamp(m_ToKnotIndex, 0, MaxKnotIndex);
                m_SamplesPerCurve = Mathf.Max(2, m_SamplesPerCurve);
            }

            public bool Overlaps(LinkData other)
            {
                if (m_SplineIndex != other.m_SplineIndex)
                {
                    return false;
                }

                // Treat links as segment ranges: [StartKnotIndex, EndKnotIndex).
                // This means [0, 1] and [1, 2] are adjacent, not overlapping.
                return StartKnotIndex < other.EndKnotIndex &&
                       other.StartKnotIndex < EndKnotIndex;
            }

            public string GetRangeLabel()
            {
                return $"Spline {m_SplineIndex}, Knots [{StartKnotIndex}, {EndKnotIndex}]";
            }

            public bool TryApply(SplineModifier owner)
            {
                if (owner == null || m_SplineContainer == null)
                {
                    return false;
                }

                if (!TryGetSpline(out UnityEngine.Splines.Spline spline))
                {
                    return false;
                }

                ClampIndices();

                if (!HasValidRange)
                {
                    throw new InvalidOperationException(
                        $"Invalid LinkData range in {owner.name}: From and To cannot be the same knot. {GetRangeLabel()}");
                }

                float minDistance = owner.EvaluateScoreOrDefault(m_MinDistanceBetweenKnots, 0f);
                float maxDistance = owner.EvaluateScoreOrDefault(m_MaxDistanceBetweenKnots, float.PositiveInfinity);

                minDistance = Mathf.Max(0f, minDistance);

                if (!float.IsPositiveInfinity(maxDistance))
                {
                    maxDistance = Mathf.Max(0f, maxDistance);
                }

                if (maxDistance < minDistance)
                {
                    throw new InvalidOperationException(
                        $"Invalid LinkData distance range in {owner.name}: max distance is smaller than min distance. {GetRangeLabel()}");
                }

                return owner.TryApplyLinkDistanceConstraint(this, spline, minDistance, maxDistance);
            }

            private bool TryGetSpline(out UnityEngine.Splines.Spline spline)
            {
                spline = null;

                if (m_SplineContainer == null || m_SplineContainer.Splines == null)
                {
                    return false;
                }

                if (m_SplineContainer.Splines.Count == 0)
                {
                    return false;
                }

                m_SplineIndex = Mathf.Clamp(m_SplineIndex, 0, m_SplineContainer.Splines.Count - 1);
                spline = m_SplineContainer.Splines[m_SplineIndex];

                if (spline == null || spline.Count == 0)
                {
                    return false;
                }

                m_FromKnotIndex = Mathf.Clamp(m_FromKnotIndex, 0, spline.Count - 1);
                m_ToKnotIndex = Mathf.Clamp(m_ToKnotIndex, 0, spline.Count - 1);

                return true;
            }
        }

        private bool CanShowList =>
            m_SplineContainer != null &&
            m_SplineContainer.Splines != null &&
            m_SplineContainer.Splines.Count > 0;

        [Title("Spline")]
        [SerializeField]
        private SplineContainer m_SplineContainer;

        [SerializeField]
        private Transform m_HandleParent;

        [Title("Update")]
        [SerializeField]
        private bool m_ApplyEveryFrame = true;

        [SerializeField, ShowIf(nameof(m_ApplyEveryFrame))]
        private bool m_ApplyInEditMode = true;

        [SerializeField, ShowIf(nameof(m_ApplyEveryFrame))]
        private bool m_ApplyInPlayMode = true;

        [Title("Knots")]
        [SerializeField, ShowIf(nameof(CanShowList))]
        private List<KnotData> m_KnotData = new List<KnotData>();

        [Title("Links")]
        [SerializeField, ShowIf(nameof(CanShowList))]
        private List<LinkData> m_LinkData = new List<LinkData>();


        private void Reset()
        {
            TryGetComponent(out m_SplineContainer);
            m_HandleParent = transform;
            RefreshDataReferences();
        }

        private void OnEnable()
        {
            if (m_SplineContainer == null)
            {
                TryGetComponent(out m_SplineContainer);
            }

            if (m_HandleParent == null)
            {
                m_HandleParent = transform;
            }

            RefreshDataReferences();
        }

        private void OnValidate()
        {
            if (m_SplineContainer == null)
            {
                TryGetComponent(out m_SplineContainer);
            }

            if (m_HandleParent == null)
            {
                m_HandleParent = transform;
            }

            RefreshDataReferences();
        }

        private void LateUpdate()
        {
            if (!m_ApplyEveryFrame)
            {
                return;
            }

            if (Application.isPlaying && !m_ApplyInPlayMode)
            {
                return;
            }

            if (!Application.isPlaying && !m_ApplyInEditMode)
            {
                return;
            }

            Apply();
        }

        [Title("Buttons")]
        [Button(ButtonSizes.Medium)]
        public void Apply()
        {
            if (m_SplineContainer == null)
            {
                return;
            }

            RefreshDataReferences();
            ValidateLinkRangesOrThrow();

            bool changedAny = false;

            if (m_KnotData != null)
            {
                for (int i = 0; i < m_KnotData.Count; i++)
                {
                    KnotData knotData = m_KnotData[i];
                    knotData.SetSplineContainer(m_SplineContainer);

                    if (knotData.TryApply())
                    {
                        changedAny = true;
                    }

                    m_KnotData[i] = knotData;
                }
            }

            if (m_LinkData != null)
            {
                for (int i = 0; i < m_LinkData.Count; i++)
                {
                    LinkData linkData = m_LinkData[i];
                    linkData.SetSplineContainer(m_SplineContainer);

                    if (linkData.TryApply(this))
                    {
                        changedAny = true;
                    }

                    m_LinkData[i] = linkData;
                }
            }

            if (changedAny)
            {
                MarkDirty(m_SplineContainer);
                MarkDirty(this);
            }
        }

        [Button(ButtonSizes.Medium)]
        public void SnapTransformsToSpline()
        {
            if (m_SplineContainer == null || m_KnotData == null)
            {
                return;
            }

            RefreshKnotDataReferences();

            for (int i = 0; i < m_KnotData.Count; i++)
            {
                KnotData knotData = m_KnotData[i];
                knotData.SetSplineContainer(m_SplineContainer);
                knotData.TrySnapTransformsToSpline();
                m_KnotData[i] = knotData;
            }
        }

        [Button(ButtonSizes.Medium)]
        public void CreateMissingTransforms()
        {
            if (m_SplineContainer == null || m_KnotData == null)
            {
                return;
            }

            if (m_HandleParent == null)
            {
                m_HandleParent = transform;
            }

            RefreshKnotDataReferences();

            for (int i = 0; i < m_KnotData.Count; i++)
            {
                KnotData knotData = m_KnotData[i];
                knotData.SetSplineContainer(m_SplineContainer);
                knotData.TryCreateMissingTransforms(m_HandleParent);
                m_KnotData[i] = knotData;
            }
        }

        [Button(ButtonSizes.Medium)]
        public void RebuildKnotDataFromSpline()
        {
            if (m_SplineContainer == null || m_SplineContainer.Splines == null)
            {
                return;
            }

            if (m_KnotData == null)
            {
                m_KnotData = new List<KnotData>();
            }

            m_KnotData.Clear();

            for (int splineIndex = 0; splineIndex < m_SplineContainer.Splines.Count; splineIndex++)
            {
                UnityEngine.Splines.Spline spline = m_SplineContainer.Splines[splineIndex];

                if (spline == null)
                {
                    continue;
                }

                for (int knotIndex = 0; knotIndex < spline.Count; knotIndex++)
                {
                    m_KnotData.Add(new KnotData(m_SplineContainer, splineIndex, knotIndex));
                }
            }
        }
        [Button(ButtonSizes.Medium)]
        public void ValidateLinks()
        {
            RefreshDataReferences();
            ValidateLinkRangesOrThrow();
        }

        [Button(ButtonSizes.Medium)]
        public void RebuildLinkDataFromSpline()
        {
            if (m_SplineContainer == null || m_SplineContainer.Splines == null)
            {
                return;
            }

            if (m_LinkData == null)
            {
                m_LinkData = new List<LinkData>();
            }

            m_LinkData.Clear();

            for (int splineIndex = 0; splineIndex < m_SplineContainer.Splines.Count; splineIndex++)
            {
                UnityEngine.Splines.Spline spline = m_SplineContainer.Splines[splineIndex];

                if (spline == null)
                {
                    continue;
                }

                for (int knotIndex = 0; knotIndex < spline.Count - 1; knotIndex++)
                {
                    m_LinkData.Add(new LinkData(m_SplineContainer, splineIndex, knotIndex, knotIndex + 1));
                }
            }
        }

        private void RefreshDataReferences()
        {
            RefreshKnotDataReferences();
            RefreshLinkDataReferences();
        }

        private void RefreshLinkDataReferences()
        {
            if (m_LinkData == null)
            {
                m_LinkData = new List<LinkData>();
                return;
            }

            for (int i = 0; i < m_LinkData.Count; i++)
            {
                LinkData linkData = m_LinkData[i];
                linkData.SetSplineContainer(m_SplineContainer);
                linkData.ClampIndices();
                m_LinkData[i] = linkData;
            }
        }

        private void ValidateLinkRangesOrThrow()
        {
            if (m_LinkData == null)
            {
                return;
            }

            for (int i = 0; i < m_LinkData.Count; i++)
            {
                LinkData a = m_LinkData[i];

                if (!a.HasValidRange)
                {
                    throw new InvalidOperationException(
                        $"Invalid LinkData range in {name}: From and To cannot be the same knot. Link {i}: {a.GetRangeLabel()}");
                }

                for (int j = i + 1; j < m_LinkData.Count; j++)
                {
                    LinkData b = m_LinkData[j];

                    if (!b.HasValidRange)
                    {
                        throw new InvalidOperationException(
                            $"Invalid LinkData range in {name}: From and To cannot be the same knot. Link {j}: {b.GetRangeLabel()}");
                    }

                    if (a.Overlaps(b))
                    {
                        throw new InvalidOperationException(
                            $"Overlapping LinkData ranges in {name}: Link {i} {a.GetRangeLabel()} overlaps Link {j} {b.GetRangeLabel()}.");
                    }
                }
            }
        }
        private bool TryApplyLinkDistanceConstraint(
    LinkData linkData,
    UnityEngine.Splines.Spline spline,
    float minDistance,
    float maxDistance)
        {
            BezierKnot fromKnot = spline[linkData.FromKnotIndex];
            BezierKnot toKnot = spline[linkData.ToKnotIndex];

            Transform containerTransform = m_SplineContainer.transform;

            Vector3 fromWorld = containerTransform.TransformPoint(ToVector3(fromKnot.Position));
            Vector3 toWorld = containerTransform.TransformPoint(ToVector3(toKnot.Position));

            float currentDistance = linkData.LimitBendBasedOnMaxAndMinDistance
                ? CalculateRangeLengthWorld(spline, linkData, -1, default)
                : Vector3.Distance(fromWorld, toWorld);

            float desiredDistance = currentDistance;

            if (currentDistance < minDistance)
            {
                desiredDistance = minDistance;
            }
            else if (currentDistance > maxDistance)
            {
                desiredDistance = maxDistance;
            }
            else
            {
                return false;
            }

            Vector3 targetWorldPosition;

            if (linkData.LimitBendBasedOnMaxAndMinDistance)
            {
                targetWorldPosition = FindToWorldPositionForBendDistance(
                    linkData,
                    spline,
                    desiredDistance,
                    fromWorld,
                    toWorld,
                    currentDistance);
            }
            else
            {
                Vector3 direction = GetSafeDirection(fromWorld, toWorld);
                targetWorldPosition = fromWorld + direction * desiredDistance;
            }

            return TrySetKnotWorldPositionAndOptionallySyncTransforms(
                linkData.SplineIndex,
                linkData.ToKnotIndex,
                targetWorldPosition,
                linkData.ShouldConstrainTransformMovement);
        }
        private bool TrySetKnotWorldPositionAndOptionallySyncTransforms(
    int splineIndex,
    int knotIndex,
    Vector3 newWorldPosition,
    bool syncAssignedTransforms)
        {
            if (!TryGetSpline(splineIndex, out UnityEngine.Splines.Spline spline))
            {
                return false;
            }

            Transform containerTransform = m_SplineContainer.transform;

            BezierKnot knot = spline[knotIndex];

            Vector3 oldWorldPosition = containerTransform.TransformPoint(ToVector3(knot.Position));
            Vector3 worldDelta = newWorldPosition - oldWorldPosition;

            if (worldDelta.sqrMagnitude <= 0.00000001f)
            {
                return false;
            }

            TangentMode tangentMode = spline.GetTangentMode(knotIndex);

            float autoSmoothTension = tangentMode == TangentMode.AutoSmooth
                ? spline.GetAutoSmoothTension(knotIndex)
                : SplineUtility.DefaultTension;

            knot.Position = ToFloat3(containerTransform.InverseTransformPoint(newWorldPosition));

            spline.SetKnot(knotIndex, knot, BezierTangent.Out);
            spline.SetTangentMode(knotIndex, tangentMode, BezierTangent.Out);

            if (tangentMode == TangentMode.AutoSmooth)
            {
                spline.SetAutoSmoothTension(knotIndex, autoSmoothTension);
            }

            if (syncAssignedTransforms)
            {
                TranslateKnotDataTransforms(splineIndex, knotIndex, worldDelta);
            }

            return true;
        }

        private void TranslateKnotDataTransforms(int splineIndex, int knotIndex, Vector3 worldDelta)
        {
            if (m_KnotData == null)
            {
                return;
            }

            for (int i = 0; i < m_KnotData.Count; i++)
            {
                KnotData knotData = m_KnotData[i];

                if (!knotData.Matches(splineIndex, knotIndex))
                {
                    continue;
                }

                knotData.TranslateAssignedTransforms(worldDelta);
                m_KnotData[i] = knotData;
            }
        }

        private bool TryGetSpline(int splineIndex, out UnityEngine.Splines.Spline spline)
        {
            spline = null;

            if (m_SplineContainer == null || m_SplineContainer.Splines == null)
            {
                return false;
            }

            if (splineIndex < 0 || splineIndex >= m_SplineContainer.Splines.Count)
            {
                return false;
            }

            spline = m_SplineContainer.Splines[splineIndex];

            return spline != null && spline.Count > 0;
        }

        private Vector3 FindToWorldPositionForBendDistance(
            LinkData linkData,
            UnityEngine.Splines.Spline spline,
            float targetLength,
            Vector3 fromWorld,
            Vector3 currentToWorld,
            float currentLength)
        {
            Vector3 direction = GetSafeDirection(fromWorld, currentToWorld);
            float currentStraightDistance = Vector3.Distance(fromWorld, currentToWorld);

            if (currentLength > targetLength)
            {
                float low = 0f;
                float high = currentStraightDistance;
                Vector3 best = currentToWorld;

                for (int i = 0; i < 16; i++)
                {
                    float mid = (low + high) * 0.5f;
                    Vector3 candidate = fromWorld + direction * mid;

                    float length = CalculateRangeLengthWorld(
                        spline,
                        linkData,
                        linkData.ToKnotIndex,
                        candidate);

                    best = candidate;

                    if (length > targetLength)
                    {
                        high = mid;
                    }
                    else
                    {
                        low = mid;
                    }
                }

                return best;
            }
            else
            {
                float low = currentStraightDistance;
                float high = Mathf.Max(currentStraightDistance * 2f, currentStraightDistance + targetLength, 0.001f);

                for (int i = 0; i < 8; i++)
                {
                    Vector3 candidate = fromWorld + direction * high;

                    float length = CalculateRangeLengthWorld(
                        spline,
                        linkData,
                        linkData.ToKnotIndex,
                        candidate);

                    if (length >= targetLength)
                    {
                        break;
                    }

                    low = high;
                    high *= 2f;
                }

                Vector3 best = fromWorld + direction * high;

                for (int i = 0; i < 16; i++)
                {
                    float mid = (low + high) * 0.5f;
                    Vector3 candidate = fromWorld + direction * mid;

                    float length = CalculateRangeLengthWorld(
                        spline,
                        linkData,
                        linkData.ToKnotIndex,
                        candidate);

                    if (length < targetLength)
                    {
                        low = mid;
                    }
                    else
                    {
                        high = mid;
                        best = candidate;
                    }
                }

                return best;
            }
        }

        private float EvaluateScoreOrDefault(ICachedScore score, float defaultValue)
        {
            if (score == null)
            {
                return defaultValue;
            }

            GameObject scoreGameObject = score.GetGameObject();

            if (scoreGameObject == null)
            {
                bool successfullySetGameObject = score.SetGameObject(gameObject);

                if (!successfullySetGameObject)
                {
                    throw new InvalidOperationException(
                        $"Could not set GameObject on ICachedScore of type {score.GetType().Name} in {name}.");
                }
            }

            return score.CalculateScore();
        }

        private float CalculateRangeLengthWorld(
            UnityEngine.Splines.Spline spline,
            LinkData linkData,
            int overrideKnotIndex,
            Vector3 overrideKnotWorldPosition)
        {
            int start = linkData.StartKnotIndex;
            int end = linkData.EndKnotIndex;
            int samplesPerCurve = Mathf.Max(2, linkData.SamplesPerCurve);

            float length = 0f;

            for (int segmentIndex = start; segmentIndex < end; segmentIndex++)
            {
                BezierKnot a = spline[segmentIndex];
                BezierKnot b = spline[segmentIndex + 1];

                if (segmentIndex == overrideKnotIndex)
                {
                    a.Position = ToFloat3(m_SplineContainer.transform.InverseTransformPoint(overrideKnotWorldPosition));
                }

                if (segmentIndex + 1 == overrideKnotIndex)
                {
                    b.Position = ToFloat3(m_SplineContainer.transform.InverseTransformPoint(overrideKnotWorldPosition));
                }

                Vector3 previousPoint = EvaluateBezierWorld(a, b, 0f);

                for (int sample = 1; sample <= samplesPerCurve; sample++)
                {
                    float t = sample / (float)samplesPerCurve;
                    Vector3 currentPoint = EvaluateBezierWorld(a, b, t);
                    length += Vector3.Distance(previousPoint, currentPoint);
                    previousPoint = currentPoint;
                }
            }

            return length;
        }

        private Vector3 EvaluateBezierWorld(BezierKnot from, BezierKnot to, float t)
        {
            float oneMinusT = 1f - t;

            float3 p0 = from.Position;
            float3 p1 = from.Position + from.TangentOut;
            float3 p2 = to.Position + to.TangentIn;
            float3 p3 = to.Position;

            float3 localPoint =
                oneMinusT * oneMinusT * oneMinusT * p0 +
                3f * oneMinusT * oneMinusT * t * p1 +
                3f * oneMinusT * t * t * p2 +
                t * t * t * p3;

            return m_SplineContainer.transform.TransformPoint(ToVector3(localPoint));
        }

        private Vector3 GetSafeDirection(Vector3 fromWorld, Vector3 toWorld)
        {
            Vector3 direction = toWorld - fromWorld;

            if (direction.sqrMagnitude > 0.00000001f)
            {
                return direction.normalized;
            }

            if (m_SplineContainer != null)
            {
                return m_SplineContainer.transform.forward;
            }

            return Vector3.forward;
        }

        private static float3 ToFloat3(Vector3 value)
        {
            return new float3(value.x, value.y, value.z);
        }

        private static Vector3 ToVector3(float3 value)
        {
            return new Vector3(value.x, value.y, value.z);
        }
        private void RefreshKnotDataReferences()
        {
            if (m_KnotData == null)
            {
                m_KnotData = new List<KnotData>();
                return;
            }

            for (int i = 0; i < m_KnotData.Count; i++)
            {
                KnotData knotData = m_KnotData[i];
                knotData.SetSplineContainer(m_SplineContainer);
                knotData.ClampIndices();
                m_KnotData[i] = knotData;
            }
        }

        private static void MarkDirty(UnityEngine.Object target)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && target != null)
            {
                EditorUtility.SetDirty(target);

                if (target is Component component)
                {
                    EditorSceneManager.MarkSceneDirty(component.gameObject.scene);
                }
            }
#endif
        }
    }
}