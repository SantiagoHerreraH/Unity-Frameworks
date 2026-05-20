using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class SetLineRendererPosition_RaycastAction : IRaycastAction
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

        [Title("Which points to set")]
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

        public enum WhichHitToUseAsReference
        {
            FirstInArray,
            ChooseBasedOnScoring
        }

        [Title("Which Hit To Use as Reference")]
        [SerializeField]
        private WhichHitToUseAsReference m_WhichHitToUseAsReference;

        [OdinSerialize, ShowInInspector, ShowIf(nameof(m_WhichHitToUseAsReference), WhichHitToUseAsReference.ChooseBasedOnScoring)]
        private IRaycastScoring m_Scoring;

        [SerializeField, ShowIf(nameof(m_WhichHitToUseAsReference), WhichHitToUseAsReference.ChooseBasedOnScoring)]
        private WhichScoreToChoose m_WhichScoreToChoose;

        private GameObject m_GameObject;

        public IRaycastAction Clone()
        {
            return new SetLineRendererPosition_RaycastAction
            {
                m_LineRendererFrom = m_LineRendererFrom,
                m_LineRenderer = m_LineRenderer,

                m_WhichPointsToSet = m_WhichPointsToSet,
                m_SpecificPointIndex = m_SpecificPointIndex,
                m_RangeStartIndex = m_RangeStartIndex,
                m_RangeEndIndex = m_RangeEndIndex,
                m_PointIndexes = m_PointIndexes != null ? new List<int>(m_PointIndexes) : new List<int>(),

                m_WhichHitToUseAsReference = m_WhichHitToUseAsReference,
                m_Scoring = m_Scoring?.Clone(),
                m_WhichScoreToChoose = m_WhichScoreToChoose,

                m_GameObject = m_GameObject
            };
        }

        public void Execute(RaycastHit[] hits)
        {
            LineRenderer lineRenderer = GetLineRenderer();

            if (lineRenderer == null)
                return;

            if (hits == null || hits.Length == 0)
                return;

            if (!TryGetReferenceHit(hits, out RaycastHit referenceHit))
                return;

            SetPositions(lineRenderer, referenceHit.point);
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

            if (m_Scoring != null)
                m_Scoring.SetGameObject(gameObj);

            if (m_LineRendererFrom == SelfType.ThisGameObject)
                return GetLineRenderer() != null;

            return true;
        }

        private void SetPositions(LineRenderer lineRenderer, Vector3 position)
        {
            switch (m_WhichPointsToSet)
            {
                case WhichPointsToSet.First:
                    SetPositionIfValid(lineRenderer, 0, position);
                    break;

                case WhichPointsToSet.Last:
                    SetPositionIfValid(lineRenderer, lineRenderer.positionCount - 1, position);
                    break;

                case WhichPointsToSet.Specific:
                    SetPositionIfValid(lineRenderer, m_SpecificPointIndex, position);
                    break;

                case WhichPointsToSet.Range:
                    SetRange(lineRenderer, position);
                    break;

                case WhichPointsToSet.List:
                    SetList(lineRenderer, position);
                    break;
            }
        }

        private void SetRange(LineRenderer lineRenderer, Vector3 position)
        {
            int start = Mathf.Min(m_RangeStartIndex, m_RangeEndIndex);
            int end = Mathf.Max(m_RangeStartIndex, m_RangeEndIndex);

            for (int i = start; i <= end; i++)
            {
                SetPositionIfValid(lineRenderer, i, position);
            }
        }

        private void SetList(LineRenderer lineRenderer, Vector3 position)
        {
            if (m_PointIndexes == null)
                return;

            for (int i = 0; i < m_PointIndexes.Count; i++)
            {
                SetPositionIfValid(lineRenderer, m_PointIndexes[i], position);
            }
        }

        private void SetPositionIfValid(LineRenderer lineRenderer, int index, Vector3 position)
        {
            if (index < 0)
                return;

            if (index >= lineRenderer.positionCount)
                return;

            Vector3 finalPosition = position;

            if (!lineRenderer.useWorldSpace)
            {
                finalPosition = lineRenderer.transform.InverseTransformPoint(position);
            }

            lineRenderer.SetPosition(index, finalPosition);
        }

        private bool TryGetReferenceHit(RaycastHit[] hits, out RaycastHit hit)
        {
            hit = default;

            if (hits == null || hits.Length == 0)
                return false;

            switch (m_WhichHitToUseAsReference)
            {
                case WhichHitToUseAsReference.FirstInArray:
                    hit = hits[0];
                    return true;

                case WhichHitToUseAsReference.ChooseBasedOnScoring:
                    return TryGetBestScoredHit(hits, out hit);

                default:
                    hit = hits[0];
                    return true;
            }
        }

        private bool TryGetBestScoredHit(RaycastHit[] hits, out RaycastHit bestHit)
        {
            bestHit = default;

            if (hits == null || hits.Length == 0)
                return false;

            if (m_Scoring == null)
                return false;

            bool hasValidHit = false;
            float selectedScore = 0f;

            for (int i = 0; i < hits.Length; i++)
            {
                float score = m_Scoring.CalculateScore(hits[i]);

                if (!hasValidHit || ShouldReplaceSelectedScore(score, selectedScore))
                {
                    selectedScore = score;
                    bestHit = hits[i];
                    hasValidHit = true;
                }
            }

            return hasValidHit;
        }

        private bool ShouldReplaceSelectedScore(float newScore, float currentScore)
        {
            return m_WhichScoreToChoose switch
            {
                WhichScoreToChoose.Highest => newScore > currentScore,
                WhichScoreToChoose.Lowest => newScore < currentScore,
                _ => newScore > currentScore
            };
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