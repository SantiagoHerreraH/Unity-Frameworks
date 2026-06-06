using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class SetLineRendererPosition_CachedGameAction : ICachedGameAction
    {
        [Title("References")]
        [SerializeField]
        private SelfType m_LineRendererFrom;

        [SerializeField, ShowIf(nameof(m_LineRendererFrom), SelfType.CustomGameObject)]
        private LineRenderer m_LineRenderer;

        public enum WhichPointsToSet
        {
            First,
            Last,
            Specific,
            Range,
            List
        }

        [Title("Which Points To Set")]
        [SerializeField]
        private WhichPointsToSet m_WhichPointsToSet;

        [SerializeField, ShowIf(nameof(m_WhichPointsToSet), WhichPointsToSet.Specific), Min(0)]
        private int m_SpecificPointIndex;

        [SerializeField, ShowIf(nameof(m_WhichPointsToSet), WhichPointsToSet.Range), Min(0)]
        private int m_RangeStartIndex;

        [SerializeField, ShowIf(nameof(m_WhichPointsToSet), WhichPointsToSet.Range), Min(0)]
        private int m_RangeEndIndex;

        [SerializeField, ShowIf(nameof(m_WhichPointsToSet), WhichPointsToSet.List)]
        private List<int> m_PointIndexes = new();

        public enum WhereToGetPositionFrom
        {
            SelfTransform,
            CustomTransform,
            CustomPosition
        }

        [Title("Position")]
        [SerializeField]
        private WhereToGetPositionFrom m_WhereToGetPositionFrom;

        [SerializeField, ShowIf(nameof(m_WhereToGetPositionFrom), WhereToGetPositionFrom.CustomTransform)]
        private Transform m_CustomTransform;

        [SerializeField, ShowIf(nameof(m_WhereToGetPositionFrom), WhereToGetPositionFrom.CustomPosition)]
        private Vector3 m_CustomPosition;

        private GameObject m_GameObject;

        public ICachedGameAction Clone()
        {
            return new SetLineRendererPosition_CachedGameAction
            {
                m_LineRendererFrom = m_LineRendererFrom,
                m_LineRenderer = m_LineRenderer,

                m_WhichPointsToSet = m_WhichPointsToSet,
                m_SpecificPointIndex = m_SpecificPointIndex,
                m_RangeStartIndex = m_RangeStartIndex,
                m_RangeEndIndex = m_RangeEndIndex,
                m_PointIndexes = m_PointIndexes != null ? new List<int>(m_PointIndexes) : new List<int>(),

                m_WhereToGetPositionFrom = m_WhereToGetPositionFrom,
                m_CustomTransform = m_CustomTransform,
                m_CustomPosition = m_CustomPosition,

                m_GameObject = m_GameObject
            };
        }

        public void Execute()
        {
            LineRenderer lineRenderer = GetLineRenderer();

            if (lineRenderer == null)
                return;

            Vector3 position = GetWorldPosition();

            SetPositions(lineRenderer, position);
        }

        public GameObject GetGameObject()
        {
            return m_GameObject;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            if (gameObj == null)
                return false;

            m_GameObject = gameObj;

            if (m_LineRendererFrom == SelfType.ThisGameObject)
                return GetLineRenderer() != null;

            return true;
        }

        private Vector3 GetWorldPosition()
        {
            switch (m_WhereToGetPositionFrom)
            {
                case WhereToGetPositionFrom.SelfTransform:
                    if (m_GameObject == null)
                        return Vector3.zero;

                    return m_GameObject.transform.position;

                case WhereToGetPositionFrom.CustomTransform:
                    if (m_CustomTransform == null)
                        return Vector3.zero;

                    return m_CustomTransform.position;

                case WhereToGetPositionFrom.CustomPosition:
                    return m_CustomPosition;

                default:
                    return Vector3.zero;
            }
        }

        private void SetPositions(LineRenderer lineRenderer, Vector3 worldPosition)
        {
            switch (m_WhichPointsToSet)
            {
                case WhichPointsToSet.First:
                    SetPositionIfValid(lineRenderer, 0, worldPosition);
                    break;

                case WhichPointsToSet.Last:
                    SetPositionIfValid(lineRenderer, lineRenderer.positionCount - 1, worldPosition);
                    break;

                case WhichPointsToSet.Specific:
                    SetPositionIfValid(lineRenderer, m_SpecificPointIndex, worldPosition);
                    break;

                case WhichPointsToSet.Range:
                    SetRange(lineRenderer, worldPosition);
                    break;

                case WhichPointsToSet.List:
                    SetList(lineRenderer, worldPosition);
                    break;
            }
        }

        private void SetRange(LineRenderer lineRenderer, Vector3 worldPosition)
        {
            int start = Mathf.Min(m_RangeStartIndex, m_RangeEndIndex);
            int end = Mathf.Max(m_RangeStartIndex, m_RangeEndIndex);

            for (int i = start; i <= end; i++)
                SetPositionIfValid(lineRenderer, i, worldPosition);
        }

        private void SetList(LineRenderer lineRenderer, Vector3 worldPosition)
        {
            if (m_PointIndexes == null)
                return;

            for (int i = 0; i < m_PointIndexes.Count; i++)
                SetPositionIfValid(lineRenderer, m_PointIndexes[i], worldPosition);
        }

        private void SetPositionIfValid(LineRenderer lineRenderer, int index, Vector3 worldPosition)
        {
            if (index < 0)
                return;

            if (index >= lineRenderer.positionCount)
                return;

            Vector3 finalPosition = worldPosition;

            if (!lineRenderer.useWorldSpace)
                finalPosition = lineRenderer.transform.InverseTransformPoint(worldPosition);

            lineRenderer.SetPosition(index, finalPosition);
        }

        private LineRenderer GetLineRenderer()
        {
            switch (m_LineRendererFrom)
            {
                case SelfType.ThisGameObject:
                    if (m_GameObject == null)
                        return null;

                    return m_GameObject.GetComponent<LineRenderer>();

                case SelfType.CustomGameObject:
                    return m_LineRenderer;

                default:
                    return null;
            }
        }
    }
}