using Pathfinding;
using SilverPillar.Core;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace SilverPillar.Integrations.AStar
{
    /// <summary>
    /// Changes an A* Pathfinding Project graph's position, rotation, and/or scale
    /// to match the transform of a reference GameObject.
    /// Works with GridGraph, RecastGraph, NavMeshGraph, and generic NavGraphs (via Matrix4x4).
    /// </summary>
    [Serializable]
    public class ChangeGraphTransform_CachedGameAction : ICachedGameAction
    {
        // ──────────────────────────────────────────────────────────────
        // Serialized fields
        // ──────────────────────────────────────────────────────────────

        [Header("Transform Reference")]
        [SerializeField]
        private SelfType m_WhoIsTheReferenceTransform;

        [SerializeField, ShowIf(nameof(m_WhoIsTheReferenceTransform), SelfType.CustomGameObject)]
        private GameObject m_GameObject;

        [Header("Transform Changes")]
        [SerializeField]
        private Space m_Space;

        [SerializeField]
        private bool m_ChangeGraphPosition;

        [SerializeField]
        private bool m_ChangeGraphRotation;

        [SerializeField]
        private bool m_ChangeGraphScale;

        // ──────────────────────────────────────────────────────────────
        // ICachedGameAction — boilerplate
        // ──────────────────────────────────────────────────────────────

        /// <inheritdoc/>
        public ICachedGameAction Clone()
        {
            // Shallow clone is sufficient; all fields are value-types or Unity references.
            return (ChangeGraphTransform_CachedGameAction)MemberwiseClone();
        }

        /// <inheritdoc/>
        public GameObject GetGameObject()
        {
            return m_GameObject;
        }

        /// <inheritdoc/>
        public bool SetGameObject(GameObject gameObj)
        {
            if (m_WhoIsTheReferenceTransform == SelfType.CustomGameObject)
                return m_GameObject != null;

            m_GameObject = gameObj;
            return true;
        }

        // ──────────────────────────────────────────────────────────────
        // Core logic
        // ──────────────────────────────────────────────────────────────

        /// <inheritdoc/>
        public void Execute()
        {
            if (AstarPath.active == null)
            {
                Debug.LogWarning("[ChangeGraphTransform] AstarPath.active is null – no graph to move.");
                return;
            }

            Transform referenceTransform = ResolveReferenceTransform();
            if (referenceTransform == null)
            {
                Debug.LogWarning("[ChangeGraphTransform] Could not resolve a reference transform.");
                return;
            }

            // Collect the values we'll apply (world-space or local-space depending on m_Space).
            Vector3 targetPosition = m_Space == Space.World
                ? referenceTransform.position
                : referenceTransform.localPosition;

            Quaternion targetRotation = m_Space == Space.World
                ? referenceTransform.rotation
                : referenceTransform.localRotation;

            Vector3 targetScale = referenceTransform.lossyScale; // lossyScale is always world-space

            // Apply to every graph registered with A*.
            AstarPath.active.AddWorkItem(() =>
            {
                foreach (NavGraph graph in AstarPath.active.data.graphs)
                {
                    if (graph == null) continue;
                    ApplyToGraph(graph, targetPosition, targetRotation, targetScale);
                }
            });
        }

        // ──────────────────────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────────────────────

        /// <summary>Returns the Transform that acts as the movement reference.</summary>
        private Transform ResolveReferenceTransform()
        {
            switch (m_WhoIsTheReferenceTransform)
            {
                case SelfType.ThisGameObject:
                    // "Self" means the GameObject stored on this action (set via SetGameObject).
                    return m_GameObject != null ? m_GameObject.transform : null;

                case SelfType.CustomGameObject:
                    return m_GameObject != null ? m_GameObject.transform : null;

                default:
                    Debug.LogWarning($"[ChangeGraphTransform] Unknown SelfType value: {m_WhoIsTheReferenceTransform}");
                    return null;
            }
        }

        /// <summary>
        /// Applies the desired transform components to <paramref name="graph"/>.
        /// Handles GridGraph, RecastGraph, NavMeshGraph, and the generic NavGraph
        /// (point graph / custom graphs) via a TRS matrix.
        /// </summary>
        private void ApplyToGraph(
            NavGraph graph,
            Vector3 newPosition,
            Quaternion newRotation,
            Vector3 newScale)
        {
            switch (graph)
            {
                // ── GridGraph (and LayerGridGraph which inherits from it) ──────────
                case GridGraph gridGraph:
                    {
                        Vector3 center = m_ChangeGraphPosition ? newPosition : gridGraph.center;
                        Quaternion rotation = m_ChangeGraphRotation ? newRotation : Quaternion.Euler(gridGraph.rotation);
                        float nodeSize = m_ChangeGraphScale ? newScale.x : gridGraph.nodeSize;
                        // nodeSize is the primary scalar for grid graphs; we use the X component.
                        gridGraph.RelocateNodes(center, rotation, nodeSize);
                        break;
                    }

                // ── RecastGraph ──────────────────────────────────────────────────
                case RecastGraph recastGraph:
                    {
                        if (m_ChangeGraphPosition)
                            recastGraph.forcedBoundsCenter = newPosition;

                        if (m_ChangeGraphRotation)
                            recastGraph.rotation = newRotation.eulerAngles;

                        // RecastGraph doesn't expose a direct "scale" field, but
                        // forcedBoundsSize can encode the desired extent.
                        if (m_ChangeGraphScale)
                            recastGraph.forcedBoundsSize = Vector3.Scale(recastGraph.forcedBoundsSize, newScale);

                        recastGraph.RelocateNodes(recastGraph.CalculateTransform());
                        break;
                    }

                // ── NavMeshGraph ─────────────────────────────────────────────────
                case NavMeshGraph navMeshGraph:
                    {
                        if (m_ChangeGraphPosition)
                            navMeshGraph.offset = newPosition;

                        if (m_ChangeGraphRotation)
                            navMeshGraph.rotation = newRotation.eulerAngles;

                        // NavMeshGraph has no explicit scale property; skip silently.

                        navMeshGraph.RelocateNodes(navMeshGraph.CalculateTransform());
                        break;
                    }

                // ── Generic / Point graph fallback ───────────────────────────────
                default:
                    {
                        // NavGraph.RelocateNodes takes a single deltaMatrix that is multiplied
                        // against every node's current position. We build the delta as:
                        //   delta = newMatrix * inverse(oldMatrix)
                        // where oldMatrix is the identity (nodes are already in world space)
                        // and newMatrix encodes only the axes we want to change.
                        Vector3 deltaPos = m_ChangeGraphPosition ? newPosition : Vector3.zero;
                        Quaternion deltaRot = m_ChangeGraphRotation ? newRotation : Quaternion.identity;
                        Vector3 deltaScale = m_ChangeGraphScale ? newScale : Vector3.one;

                        Matrix4x4 deltaMatrix = Matrix4x4.TRS(deltaPos, deltaRot, deltaScale);

                        graph.RelocateNodes(deltaMatrix);
                        break;
                    }
            }
        }
    }
}
