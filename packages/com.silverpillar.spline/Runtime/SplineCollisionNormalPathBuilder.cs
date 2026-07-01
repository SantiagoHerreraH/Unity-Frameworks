using Sirenix.OdinInspector;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

using Vector3 = UnityEngine.Vector3;
using Quaternion = UnityEngine.Quaternion;

namespace SilverPillar.Spline
{
    public class SplineCollisionNormalPathBuilder : MonoBehaviour
    {
        private const float MinSqrMagnitude = 0.0000001f;
        private const float MinDistance = 0.0001f;

        [Title("Source Spline")]
        [SerializeField]
        private SplineContainer m_SourceSplineContainer;

        [SerializeField, PropertyRange(0, nameof(MaxSourceSplineIndex))]
        private int m_SourceSplineIndex;

        [SerializeField, Min(2), Tooltip("Higher values make raycasting and generated source path more accurate.")]
        private int m_SourceSamplesPerCurve = 16;

        [Title("Generated Spline")]
        [SerializeField]
        private SplineContainer m_OutputSplineContainer;

        [SerializeField, Tooltip("If true and Output Spline Container is null, a new GameObject with a SplineContainer will be created.")]
        private bool m_CreateOutputContainerIfNull = true;

        [SerializeField]
        private TangentMode m_OutputTangentMode = TangentMode.AutoSmooth;

        [SerializeField, ShowIf(nameof(UsesAutoSmoothOutput)), Range(0f, 1f)]
        private float m_AutoSmoothTension = SplineUtility.DefaultTension;

        [SerializeField, Min(0f), Tooltip("Minimum distance between generated spline points. Helps avoid creating too many close knots.")]
        private float m_MinOutputPointDistance = 0.05f;

        [Title("Raycast")]
        [SerializeField]
        private LayerMask m_RaycastLayerMask = ~0;

        [SerializeField]
        private QueryTriggerInteraction m_QueryTriggerInteraction = QueryTriggerInteraction.Ignore;

        [SerializeField, Min(0f), Tooltip("Offsets the next ray origin slightly along the ray direction to avoid immediately hitting the same surface.")]
        private float m_RaycastOriginOffset = 0.01f;

        [SerializeField, Min(1), Tooltip("Safety limit to avoid infinite normal-ray loops.")]
        private int m_MaxNormalRaycastIterations = 32;

        [SerializeField, Tooltip("If a normal ray misses, a final point will be added in the ray direction to complete the threshold distance.")]
        private bool m_AddFinalPointWhenRayMisses = true;

        [Title("Distance Threshold")]
        [SerializeField, Min(0.001f), Tooltip("The generated path will try to reach this total world-space distance.")]
        private float m_TotalPathDistanceThreshold = 10f;

        [Title("Animation")]
        [SerializeField]
        private SplineAnimate m_SplineAnimate;

        [SerializeField, Tooltip("If true, the generated spline container will be assigned to the SplineAnimate after building.")]
        private bool m_AssignGeneratedSplineToAnimator = true;

        [SerializeField, Tooltip("If true, BuildAssignAndPlay will restart the SplineAnimate from the beginning.")]
        private bool m_RestartAnimatorWhenPlaying = true;

        [Title("Debug")]
        [SerializeField]
        private bool m_DrawDebugGizmos = true;

        [SerializeField]
        private bool m_LogBuildResult;

        [ShowInInspector, ReadOnly]
        private float m_LastGeneratedDistance;

        [ShowInInspector, ReadOnly]
        private int m_LastGeneratedPointCount;

        [ShowInInspector, ReadOnly]
        private bool m_LastBuildHitSomething;

        private readonly List<Vector3> m_LastGeneratedWorldPoints = new List<Vector3>();
        private readonly List<DebugRayData> m_DebugRays = new List<DebugRayData>();

        private bool UsesAutoSmoothOutput => m_OutputTangentMode == TangentMode.AutoSmooth;

        private int MaxSourceSplineIndex
        {
            get
            {
                if (m_SourceSplineContainer == null ||
                    m_SourceSplineContainer.Splines == null ||
                    m_SourceSplineContainer.Splines.Count == 0)
                {
                    return 0;
                }

                return Mathf.Max(0, m_SourceSplineContainer.Splines.Count - 1);
            }
        }

        private void Reset()
        {
            TryGetComponent(out m_SourceSplineContainer);
            TryGetComponent(out m_SplineAnimate);

            if (m_SplineAnimate != null && m_SourceSplineContainer == null)
            {
                m_SourceSplineContainer = m_SplineAnimate.Container;
            }
        }

        [Title("Actions")]
        [Button(ButtonSizes.Medium)]
        public void BuildAssignAndPlay()
        {
            if (!BuildAndAssign())
            {
                return;
            }

            if (m_SplineAnimate == null)
            {
                return;
            }

            if (m_RestartAnimatorWhenPlaying)
            {
                m_SplineAnimate.Restart(true);
            }
            else
            {
                m_SplineAnimate.Play();
            }
        }

        [Button(ButtonSizes.Medium)]
        public bool BuildAndAssign()
        {
            bool built = BuildGeneratedSpline();

            if (!built)
            {
                return false;
            }

            if (m_AssignGeneratedSplineToAnimator && m_SplineAnimate != null)
            {
                m_SplineAnimate.Container = m_OutputSplineContainer;
            }

            return true;
        }

        [Button(ButtonSizes.Medium)]
        public bool BuildGeneratedSpline()
        {
            m_LastGeneratedWorldPoints.Clear();
            m_DebugRays.Clear();

            m_LastGeneratedDistance = 0f;
            m_LastGeneratedPointCount = 0;
            m_LastBuildHitSomething = false;

            if (!TryGetSourceSpline(out UnityEngine.Splines.Spline sourceSpline))
            {
                Debug.LogError($"{nameof(SplineCollisionNormalPathBuilder)} on {name} could not build. Source spline is invalid.", this);
                return false;
            }

            if (!EnsureOutputSplineContainer())
            {
                Debug.LogError($"{nameof(SplineCollisionNormalPathBuilder)} on {name} could not build. Output spline is invalid.", this);
                return false;
            }

            List<SplineSample> samples = new List<SplineSample>();

            if (!SampleSplineWorld(
                    m_SourceSplineContainer,
                    sourceSpline,
                    Mathf.Max(2, m_SourceSamplesPerCurve),
                    samples))
            {
                Debug.LogError($"{nameof(SplineCollisionNormalPathBuilder)} on {name} could not build. Source spline needs at least 2 knots.", this);
                return false;
            }

            float sourceLength = samples[samples.Count - 1].DistanceFromStart;

            if (m_TotalPathDistanceThreshold <= MinDistance)
            {
                Debug.LogError($"{nameof(SplineCollisionNormalPathBuilder)} on {name} has an invalid distance threshold.", this);
                return false;
            }

            RaycastHit firstHit;
            float distanceAlongSplineToHit;
            Vector3 splineDirectionAtHit;

            bool hitAlongSourceSpline = TryRaycastAlongSplineSamples(
                samples,
                out firstHit,
                out distanceAlongSplineToHit,
                out splineDirectionAtHit);

            if (hitAlongSourceSpline && distanceAlongSplineToHit <= m_TotalPathDistanceThreshold)
            {
                AddSampledSplinePointsUpToDistance(
                    samples,
                    distanceAlongSplineToHit,
                    m_LastGeneratedWorldPoints);

                AddPoint(m_LastGeneratedWorldPoints, firstHit.point, true);

                m_LastGeneratedDistance = distanceAlongSplineToHit;
                m_LastBuildHitSomething = true;

                ExtendUsingCollisionNormals(
                    firstHit.point,
                    firstHit.normal,
                    ref m_LastGeneratedDistance,
                    m_LastGeneratedWorldPoints);
            }
            else
            {
                float initialDistance = Mathf.Min(sourceLength, m_TotalPathDistanceThreshold);

                AddSampledSplinePointsUpToDistance(
                    samples,
                    initialDistance,
                    m_LastGeneratedWorldPoints);

                m_LastGeneratedDistance = initialDistance;

                if (m_LastGeneratedDistance < m_TotalPathDistanceThreshold - MinDistance)
                {
                    Vector3 sourceEndPoint = samples[samples.Count - 1].Position;
                    Vector3 sourceEndDirection = GetSafeDirection(samples[samples.Count - 1].Tangent, sourceEndPoint);

                    RaycastHit endRayHit;

                    bool hitFromSourceEnd = CastTrackedRay(
                        sourceEndPoint,
                        sourceEndDirection,
                        m_TotalPathDistanceThreshold - m_LastGeneratedDistance,
                        out endRayHit);

                    if (hitFromSourceEnd)
                    {
                        float distanceToFirstHit = Vector3.Distance(sourceEndPoint, endRayHit.point);

                        if (m_LastGeneratedDistance + distanceToFirstHit > m_TotalPathDistanceThreshold)
                        {
                            Vector3 finalPoint = sourceEndPoint + sourceEndDirection.normalized *
                                (m_TotalPathDistanceThreshold - m_LastGeneratedDistance);

                            AddPoint(m_LastGeneratedWorldPoints, finalPoint, true);
                            m_LastGeneratedDistance = m_TotalPathDistanceThreshold;
                        }
                        else
                        {
                            AddPoint(m_LastGeneratedWorldPoints, endRayHit.point, true);

                            m_LastGeneratedDistance += distanceToFirstHit;
                            m_LastBuildHitSomething = true;

                            ExtendUsingCollisionNormals(
                                endRayHit.point,
                                endRayHit.normal,
                                ref m_LastGeneratedDistance,
                                m_LastGeneratedWorldPoints);
                        }
                    }
                    else if (m_AddFinalPointWhenRayMisses)
                    {
                        Vector3 finalPoint = sourceEndPoint + sourceEndDirection.normalized *
                            (m_TotalPathDistanceThreshold - m_LastGeneratedDistance);

                        AddPoint(m_LastGeneratedWorldPoints, finalPoint, true);
                        m_LastGeneratedDistance = m_TotalPathDistanceThreshold;
                    }
                }
            }

            if (m_LastGeneratedWorldPoints.Count < 2)
            {
                Debug.LogError($"{nameof(SplineCollisionNormalPathBuilder)} on {name} could not build. Generated path has less than 2 points.", this);
                return false;
            }

            WriteWorldPointsToOutputSpline(m_LastGeneratedWorldPoints);

            m_LastGeneratedPointCount = m_LastGeneratedWorldPoints.Count;

            if (m_LogBuildResult)
            {
                Debug.Log(
                    $"{nameof(SplineCollisionNormalPathBuilder)} on {name} built path. " +
                    $"Points: {m_LastGeneratedPointCount}. " +
                    $"Distance: {m_LastGeneratedDistance}. " +
                    $"Hit something: {m_LastBuildHitSomething}.",
                    this);
            }

            MarkDirty(m_OutputSplineContainer);
            MarkDirty(this);

            return true;
        }

        private void ExtendUsingCollisionNormals(
            Vector3 firstCollisionPoint,
            Vector3 firstCollisionNormal,
            ref float currentTotalDistance,
            List<Vector3> points)
        {
            Vector3 previousPoint = firstCollisionPoint;
            Vector3 direction = GetSafeDirection(firstCollisionNormal, previousPoint);

            for (int i = 0; i < m_MaxNormalRaycastIterations; i++)
            {
                float remainingDistance = m_TotalPathDistanceThreshold - currentTotalDistance;

                if (remainingDistance <= MinDistance)
                {
                    currentTotalDistance = m_TotalPathDistanceThreshold;
                    return;
                }

                RaycastHit hit;

                bool didHit = CastTrackedRay(
                    previousPoint,
                    direction,
                    remainingDistance,
                    out hit);

                if (!didHit)
                {
                    if (m_AddFinalPointWhenRayMisses)
                    {
                        Vector3 finalPoint = previousPoint + direction.normalized * remainingDistance;

                        AddPoint(points, finalPoint, true);
                        currentTotalDistance = m_TotalPathDistanceThreshold;
                    }

                    return;
                }

                float distanceBetweenCollisionPoints = Vector3.Distance(previousPoint, hit.point);

                if (distanceBetweenCollisionPoints <= MinDistance)
                {
                    direction = GetSafeDirection(hit.normal, direction);
                    previousPoint = hit.point;
                    continue;
                }

                if (currentTotalDistance + distanceBetweenCollisionPoints > m_TotalPathDistanceThreshold)
                {
                    Vector3 finalPoint = previousPoint + direction.normalized *
                        (m_TotalPathDistanceThreshold - currentTotalDistance);

                    AddPoint(points, finalPoint, true);
                    currentTotalDistance = m_TotalPathDistanceThreshold;
                    return;
                }

                AddPoint(points, hit.point, true);

                currentTotalDistance += distanceBetweenCollisionPoints;
                previousPoint = hit.point;
                direction = GetSafeDirection(hit.normal, direction);

                m_LastBuildHitSomething = true;
            }
        }

        private bool TryRaycastAlongSplineSamples(
            List<SplineSample> samples,
            out RaycastHit hit,
            out float distanceAlongSplineToHit,
            out Vector3 directionAtHit)
        {
            hit = default;
            distanceAlongSplineToHit = 0f;
            directionAtHit = Vector3.right;

            for (int i = 0; i < samples.Count - 1; i++)
            {
                Vector3 from = samples[i].Position;
                Vector3 to = samples[i + 1].Position;

                Vector3 segment = to - from;
                float segmentLength = segment.magnitude;

                if (segmentLength <= MinDistance)
                {
                    continue;
                }

                Vector3 direction = segment / segmentLength;

                DebugRayData debugRay = new DebugRayData
                {
                    Origin = from,
                    Direction = direction,
                    Distance = segmentLength,
                    DidHit = false
                };

                bool didHit = Physics.Raycast(
                    from,
                    direction,
                    out hit,
                    segmentLength,
                    m_RaycastLayerMask,
                    m_QueryTriggerInteraction);

                if (didHit)
                {
                    debugRay.DidHit = true;
                    debugRay.HitPoint = hit.point;
                    debugRay.HitNormal = hit.normal;

                    m_DebugRays.Add(debugRay);

                    distanceAlongSplineToHit =
                        samples[i].DistanceFromStart +
                        Vector3.Distance(from, hit.point);

                    directionAtHit = direction;
                    return true;
                }

                m_DebugRays.Add(debugRay);
            }

            return false;
        }

        private bool CastTrackedRay(
            Vector3 previousPoint,
            Vector3 direction,
            float maxDistanceFromPreviousPoint,
            out RaycastHit hit)
        {
            hit = default;

            direction = GetSafeDirection(direction, previousPoint);

            float originOffset = Mathf.Max(0f, m_RaycastOriginOffset);
            float rayDistance = Mathf.Max(0f, maxDistanceFromPreviousPoint - originOffset);

            if (rayDistance <= MinDistance)
            {
                return false;
            }

            Vector3 origin = previousPoint + direction * originOffset;

            DebugRayData debugRay = new DebugRayData
            {
                Origin = origin,
                Direction = direction,
                Distance = rayDistance,
                DidHit = false
            };

            bool didHit = Physics.Raycast(
                origin,
                direction,
                out hit,
                rayDistance,
                m_RaycastLayerMask,
                m_QueryTriggerInteraction);

            if (didHit)
            {
                debugRay.DidHit = true;
                debugRay.HitPoint = hit.point;
                debugRay.HitNormal = hit.normal;
            }

            m_DebugRays.Add(debugRay);

            return didHit;
        }

        private void AddSampledSplinePointsUpToDistance(
            List<SplineSample> samples,
            float targetDistance,
            List<Vector3> points)
        {
            if (samples == null || samples.Count == 0)
            {
                return;
            }

            targetDistance = Mathf.Max(0f, targetDistance);

            AddPoint(points, samples[0].Position, true);

            for (int i = 1; i < samples.Count; i++)
            {
                SplineSample previous = samples[i - 1];
                SplineSample current = samples[i];

                if (current.DistanceFromStart < targetDistance)
                {
                    AddPoint(points, current.Position, false);
                    continue;
                }

                float segmentDistance = current.DistanceFromStart - previous.DistanceFromStart;

                if (segmentDistance <= MinDistance)
                {
                    AddPoint(points, current.Position, true);
                    return;
                }

                float t = Mathf.InverseLerp(
                    previous.DistanceFromStart,
                    current.DistanceFromStart,
                    targetDistance);

                Vector3 interpolatedPoint = Vector3.Lerp(previous.Position, current.Position, t);

                AddPoint(points, interpolatedPoint, true);
                return;
            }

            AddPoint(points, samples[samples.Count - 1].Position, true);
        }

        private void AddPoint(List<Vector3> points, Vector3 point, bool force)
        {
            if (points == null)
            {
                return;
            }

            if (points.Count == 0)
            {
                points.Add(point);
                return;
            }

            float minDistance = Mathf.Max(0f, m_MinOutputPointDistance);

            if (!force && Vector3.Distance(points[points.Count - 1], point) < minDistance)
            {
                return;
            }

            if (Vector3.Distance(points[points.Count - 1], point) <= MinDistance)
            {
                return;
            }

            points.Add(point);
        }

        private void WriteWorldPointsToOutputSpline(List<Vector3> worldPoints)
        {
            UnityEngine.Splines.Spline outputSpline = GetOrCreateOutputSpline();

            outputSpline.Clear();
            outputSpline.Closed = false;

            Transform outputTransform = m_OutputSplineContainer.transform;

            for (int i = 0; i < worldPoints.Count; i++)
            {
                Vector3 worldPosition = worldPoints[i];
                Vector3 worldDirection = GetPointDirection(worldPoints, i);

                Quaternion worldRotation = GetLookRotationSafe(worldDirection);
                Quaternion localRotation = Quaternion.Inverse(outputTransform.rotation) * worldRotation;

                BezierKnot knot = new BezierKnot
                {
                    Position = ToFloat3(outputTransform.InverseTransformPoint(worldPosition)),
                    TangentIn = float3.zero,
                    TangentOut = float3.zero,
                    Rotation = ToQuaternion(localRotation)
                };

                outputSpline.Add(knot);
            }

            for (int i = 0; i < outputSpline.Count; i++)
            {
                outputSpline.SetTangentMode(i, m_OutputTangentMode, BezierTangent.Out);

                if (m_OutputTangentMode == TangentMode.AutoSmooth)
                {
                    outputSpline.SetAutoSmoothTension(i, m_AutoSmoothTension);
                }
            }
        }

        private Vector3 GetPointDirection(List<Vector3> points, int index)
        {
            if (points == null || points.Count < 2)
            {
                return Vector3.right;
            }

            if (index <= 0)
            {
                return GetSafeDirection(points[1] - points[0], points[0]);
            }

            if (index >= points.Count - 1)
            {
                return GetSafeDirection(points[index] - points[index - 1], points[index]);
            }

            Vector3 direction = points[index + 1] - points[index - 1];
            return GetSafeDirection(direction, points[index]);
        }

        private bool SampleSplineWorld(
            SplineContainer container,
            UnityEngine.Splines.Spline spline,
            int samplesPerCurve,
            List<SplineSample> samples)
        {
            samples.Clear();

            if (container == null || spline == null || spline.Count < 2)
            {
                return false;
            }

            samplesPerCurve = Mathf.Max(2, samplesPerCurve);

            int segmentCount = spline.Closed ? spline.Count : spline.Count - 1;
            float accumulatedDistance = 0f;

            for (int segmentIndex = 0; segmentIndex < segmentCount; segmentIndex++)
            {
                BezierKnot from = spline[segmentIndex];
                BezierKnot to = spline[(segmentIndex + 1) % spline.Count];

                int firstSampleIndex = segmentIndex == 0 ? 0 : 1;

                for (int sampleIndex = firstSampleIndex; sampleIndex <= samplesPerCurve; sampleIndex++)
                {
                    float t = sampleIndex / (float)samplesPerCurve;

                    Vector3 position = EvaluateBezierWorld(container.transform, from, to, t);
                    Vector3 tangent = EvaluateBezierTangentWorld(container.transform, from, to, t);

                    if (samples.Count > 0)
                    {
                        accumulatedDistance += Vector3.Distance(
                            samples[samples.Count - 1].Position,
                            position);
                    }

                    samples.Add(new SplineSample
                    {
                        Position = position,
                        Tangent = tangent,
                        DistanceFromStart = accumulatedDistance
                    });
                }
            }

            return samples.Count >= 2;
        }

        private bool TryGetSourceSpline(out UnityEngine.Splines.Spline sourceSpline)
        {
            sourceSpline = null;

            if (m_SourceSplineContainer == null ||
                m_SourceSplineContainer.Splines == null ||
                m_SourceSplineContainer.Splines.Count == 0)
            {
                return false;
            }

            m_SourceSplineIndex = Mathf.Clamp(
                m_SourceSplineIndex,
                0,
                m_SourceSplineContainer.Splines.Count - 1);

            sourceSpline = m_SourceSplineContainer.Splines[m_SourceSplineIndex];

            return sourceSpline != null && sourceSpline.Count >= 2;
        }

        private bool EnsureOutputSplineContainer()
        {
            if (m_OutputSplineContainer != null)
            {
                return true;
            }

            if (!m_CreateOutputContainerIfNull)
            {
                return false;
            }

            GameObject outputGameObject = new GameObject($"{name}_GeneratedSpline");

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Undo.RegisterCreatedObjectUndo(outputGameObject, "Create Generated Spline Container");
            }
#endif

            if (m_SourceSplineContainer != null)
            {
                Transform sourceTransform = m_SourceSplineContainer.transform;

                outputGameObject.transform.SetParent(sourceTransform.parent, false);
                outputGameObject.transform.SetPositionAndRotation(
                    sourceTransform.position,
                    sourceTransform.rotation);

                outputGameObject.transform.localScale = sourceTransform.localScale;
            }
            else
            {
                outputGameObject.transform.SetPositionAndRotation(transform.position, transform.rotation);
            }

            m_OutputSplineContainer = outputGameObject.AddComponent<SplineContainer>();
            return m_OutputSplineContainer != null;
        }

        private UnityEngine.Splines.Spline GetOrCreateOutputSpline()
        {
            if (m_OutputSplineContainer.Splines != null &&
                m_OutputSplineContainer.Splines.Count > 0 &&
                m_OutputSplineContainer.Splines[0] != null)
            {
                return m_OutputSplineContainer.Splines[0];
            }

            m_OutputSplineContainer.Spline = new UnityEngine.Splines.Spline();
            return m_OutputSplineContainer.Spline;
        }

        private static Vector3 EvaluateBezierWorld(
            Transform transform,
            BezierKnot from,
            BezierKnot to,
            float t)
        {
            float3 localPoint = EvaluateBezierLocal(from, to, t);
            return transform.TransformPoint(ToVector3(localPoint));
        }

        private static Vector3 EvaluateBezierTangentWorld(
            Transform transform,
            BezierKnot from,
            BezierKnot to,
            float t)
        {
            float3 localTangent = EvaluateBezierTangentLocal(from, to, t);
            return transform.TransformVector(ToVector3(localTangent));
        }

        private static float3 EvaluateBezierLocal(
            BezierKnot from,
            BezierKnot to,
            float t)
        {
            float oneMinusT = 1f - t;

            float3 p0 = from.Position;
            float3 p1 = from.Position + from.TangentOut;
            float3 p2 = to.Position + to.TangentIn;
            float3 p3 = to.Position;

            return
                oneMinusT * oneMinusT * oneMinusT * p0 +
                3f * oneMinusT * oneMinusT * t * p1 +
                3f * oneMinusT * t * t * p2 +
                t * t * t * p3;
        }

        private static float3 EvaluateBezierTangentLocal(
            BezierKnot from,
            BezierKnot to,
            float t)
        {
            float oneMinusT = 1f - t;

            float3 p0 = from.Position;
            float3 p1 = from.Position + from.TangentOut;
            float3 p2 = to.Position + to.TangentIn;
            float3 p3 = to.Position;

            return
                3f * oneMinusT * oneMinusT * (p1 - p0) +
                6f * oneMinusT * t * (p2 - p1) +
                3f * t * t * (p3 - p2);
        }

        private static Vector3 GetSafeDirection(Vector3 direction, Vector3 fallbackReference)
        {
            if (direction.sqrMagnitude > MinSqrMagnitude)
            {
                return direction.normalized;
            }

            if (fallbackReference.sqrMagnitude > MinSqrMagnitude)
            {
                return fallbackReference.normalized;
            }

            return Vector3.right;
        }

        private static Quaternion GetLookRotationSafe(Vector3 direction)
        {
            if (direction.sqrMagnitude <= MinSqrMagnitude)
            {
                direction = Vector3.right;
            }

            direction.Normalize();

            Vector3 up = Vector3.up;

            if (Mathf.Abs(Vector3.Dot(direction, up)) > 0.99f)
            {
                up = Vector3.forward;
            }

            return Quaternion.LookRotation(direction, up);
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

        private void OnDrawGizmosSelected()
        {
            if (!m_DrawDebugGizmos)
            {
                return;
            }

            for (int i = 0; i < m_DebugRays.Count; i++)
            {
                DebugRayData ray = m_DebugRays[i];

                Gizmos.DrawLine(
                    ray.Origin,
                    ray.Origin + ray.Direction.normalized * ray.Distance);

                if (ray.DidHit)
                {
                    Gizmos.DrawSphere(ray.HitPoint, 0.05f);
                    Gizmos.DrawLine(ray.HitPoint, ray.HitPoint + ray.HitNormal.normalized * 0.35f);
                }
            }

            if (m_LastGeneratedWorldPoints == null)
            {
                return;
            }

            for (int i = 0; i < m_LastGeneratedWorldPoints.Count - 1; i++)
            {
                Gizmos.DrawSphere(m_LastGeneratedWorldPoints[i], 0.04f);
                Gizmos.DrawLine(m_LastGeneratedWorldPoints[i], m_LastGeneratedWorldPoints[i + 1]);
            }

            if (m_LastGeneratedWorldPoints.Count > 0)
            {
                Gizmos.DrawSphere(
                    m_LastGeneratedWorldPoints[m_LastGeneratedWorldPoints.Count - 1],
                    0.06f);
            }
        }

        private static void MarkDirty(Object target)
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

        private struct SplineSample
        {
            public Vector3 Position;
            public Vector3 Tangent;
            public float DistanceFromStart;
        }

        private struct DebugRayData
        {
            public Vector3 Origin;
            public Vector3 Direction;
            public float Distance;

            public bool DidHit;
            public Vector3 HitPoint;
            public Vector3 HitNormal;
        }
    }
}