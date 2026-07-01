using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class GridPlacement_CachedGameAction : ICachedGameAction
    {
        [Title("GameObject to Place")]
        [SerializeField]
        private SelfType m_WhichGameObjectToPlace;

        [SerializeField, ShowIf(nameof(m_WhichGameObjectToPlace), SelfType.CustomGameObject)]
        private GameObject m_GameObject;

        [Title("Grid Position")]
        [SerializeField, Tooltip("If null will be Self")]
        private Transform m_AnchorPosition;

        [SerializeField, Range(0f, 1f), Tooltip("0 is left 1 is right.")]
        private float m_AnchorPosInX;

        [SerializeField, Range(0f, 1f), Tooltip("0 is Bottom 1 is Top.")]
        private float m_AnchorPosInY;

        [Title("Grid Settings")]
        [SerializeField, Min(1)]
        private int m_RowNumber = 1;

        [SerializeField, Min(1)]
        private int m_ColumnNumber = 1;

        [SerializeField, Min(float.Epsilon)]
        private float m_CellSizeX = 1f;

        [SerializeField, Min(float.Epsilon)]
        private float m_CellSizeY = 1f;

        [SerializeField, Min(0)]
        private float m_CellSpaceInBetweenX;

        [SerializeField, Min(0)]
        private float m_CellSpaceInBetweenY;

        public enum GridIndexAdvanceType
        {
            InOrder,
            Random
        }

        public enum WhatHappensWhenHitsRange
        {
            Loop,
            ChangeDirection
        }

        [Title("Placement Settings")]
        [SerializeField, Tooltip("Negative is Random."), PropertyRange(-1, nameof(m_MaxRowIndex))]
        private int m_StartRowIndex;

        [SerializeField, Tooltip("Negative is Random."), PropertyRange(-1, nameof(m_MaxColumnIndex))]
        private int m_StartColumnIndex;

        [Title("Row Advancement Settings")]
        [SerializeField]
        private GridIndexAdvanceType m_RowAdvanceType;

        [SerializeField, ShowIf(nameof(m_RowAdvanceType), GridIndexAdvanceType.InOrder)]
        private WhatHappensWhenHitsRange m_WhatHappensWhenRowHitsRange;

        [SerializeField, ShowIf(nameof(m_RowAdvanceType), GridIndexAdvanceType.InOrder)]
        private int m_RowAdvancesPerCall;

        [Title("Column Advancement Settings")]
        [SerializeField]
        private GridIndexAdvanceType m_ColAdvanceType;

        [SerializeField, ShowIf(nameof(m_ColAdvanceType), GridIndexAdvanceType.InOrder)]
        private WhatHappensWhenHitsRange m_WhatHappensWhenColHitsRange;

        [SerializeField, ShowIf(nameof(m_ColAdvanceType), GridIndexAdvanceType.InOrder)]
        private int m_ColumnAdvancesPerCall;

        private int m_MaxColumnIndex => Mathf.Max(0, m_ColumnNumber - 1);
        private int m_MaxRowIndex => Mathf.Max(0, m_RowNumber - 1);

        private GameObject m_CachedGameObject;

        private bool m_HasInitialized;
        private int m_CurrentRowIndex;
        private int m_CurrentColumnIndex;

        private int m_CurrentRowDirection = 1;
        private int m_CurrentColumnDirection = 1;

        public ICachedGameAction Clone()
        {
            return new GridPlacement_CachedGameAction
            {
                m_WhichGameObjectToPlace = m_WhichGameObjectToPlace,
                m_GameObject = m_GameObject,

                m_AnchorPosition = m_AnchorPosition,
                m_AnchorPosInX = m_AnchorPosInX,
                m_AnchorPosInY = m_AnchorPosInY,

                m_RowNumber = m_RowNumber,
                m_ColumnNumber = m_ColumnNumber,
                m_CellSizeX = m_CellSizeX,
                m_CellSizeY = m_CellSizeY,
                m_CellSpaceInBetweenX = m_CellSpaceInBetweenX,
                m_CellSpaceInBetweenY = m_CellSpaceInBetweenY,

                m_StartRowIndex = m_StartRowIndex,
                m_StartColumnIndex = m_StartColumnIndex,

                m_RowAdvanceType = m_RowAdvanceType,
                m_WhatHappensWhenRowHitsRange = m_WhatHappensWhenRowHitsRange,
                m_RowAdvancesPerCall = m_RowAdvancesPerCall,

                m_ColAdvanceType = m_ColAdvanceType,
                m_WhatHappensWhenColHitsRange = m_WhatHappensWhenColHitsRange,
                m_ColumnAdvancesPerCall = m_ColumnAdvancesPerCall,

                m_CachedGameObject = m_CachedGameObject,

                m_HasInitialized = m_HasInitialized,
                m_CurrentRowIndex = m_CurrentRowIndex,
                m_CurrentColumnIndex = m_CurrentColumnIndex,
                m_CurrentRowDirection = m_CurrentRowDirection,
                m_CurrentColumnDirection = m_CurrentColumnDirection
            };
        }

        public void Execute()
        {
            GameObject gameObjectToPlace = GetGameObject();

            if (gameObjectToPlace == null)
            {
                Debug.LogError($"gameObjectToPlace is null in GridPlacement Cached Game Action");
                return;
            }

            if (m_AnchorPosition == null)
                m_AnchorPosition = m_CachedGameObject.transform;

            ValidateGridValues();

            if (!m_HasInitialized)
                InitializePlacementIndices();

            Vector3 targetPosition = GetWorldPositionFromGridIndex(m_CurrentRowIndex, m_CurrentColumnIndex);
            gameObjectToPlace.transform.position = targetPosition;

            AdvanceIndices();
        }

        public GameObject GetGameObject()
        {
            return m_CachedGameObject;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            if (gameObj == null)
                return false;

            m_CachedGameObject = gameObj;
            return true;
        }

        private void ValidateGridValues()
        {
            m_RowNumber = Mathf.Max(1, m_RowNumber);
            m_ColumnNumber = Mathf.Max(1, m_ColumnNumber);

            m_CellSizeX = Mathf.Max(float.Epsilon, m_CellSizeX);
            m_CellSizeY = Mathf.Max(float.Epsilon, m_CellSizeY);

            m_CellSpaceInBetweenX = Mathf.Max(0f, m_CellSpaceInBetweenX);
            m_CellSpaceInBetweenY = Mathf.Max(0f, m_CellSpaceInBetweenY);
        }

        private void InitializePlacementIndices()
        {
            m_CurrentRowIndex = ResolveStartIndex(m_StartRowIndex, m_RowNumber);
            m_CurrentColumnIndex = ResolveStartIndex(m_StartColumnIndex, m_ColumnNumber);

            m_CurrentRowDirection = GetInitialDirection(m_RowAdvancesPerCall);
            m_CurrentColumnDirection = GetInitialDirection(m_ColumnAdvancesPerCall);

            m_HasInitialized = true;
        }

        private int ResolveStartIndex(int index, int maxExclusive)
        {
            if (maxExclusive <= 0)
                return 0;

            if (index < 0)
                return GetRandomIndex(maxExclusive);

            return Mathf.Clamp(index, 0, maxExclusive - 1);
        }

        private int GetInitialDirection(int advance)
        {
            if (advance < 0)
                return -1;

            return 1;
        }

        private Vector3 GetWorldPositionFromGridIndex(int rowIndex, int columnIndex)
        {
            float totalGridWidth =
                m_ColumnNumber * m_CellSizeX +
                (m_ColumnNumber - 1) * m_CellSpaceInBetweenX;

            float totalGridHeight =
                m_RowNumber * m_CellSizeY +
                (m_RowNumber - 1) * m_CellSpaceInBetweenY;

            Vector3 right = m_AnchorPosition.right;
            Vector3 up = m_AnchorPosition.up;

            Vector3 bottomLeftCorner =
                m_AnchorPosition.position -
                right * (totalGridWidth * m_AnchorPosInX) -
                up * (totalGridHeight * m_AnchorPosInY);

            float xOffset =
                columnIndex * (m_CellSizeX + m_CellSpaceInBetweenX) +
                m_CellSizeX * 0.5f;

            float yOffset =
                rowIndex * (m_CellSizeY + m_CellSpaceInBetweenY) +
                m_CellSizeY * 0.5f;

            return bottomLeftCorner + right * xOffset + up * yOffset;
        }

        private void AdvanceIndices()
        {
            m_CurrentRowIndex = GetNextIndex(
                m_CurrentRowIndex,
                m_RowNumber,
                m_RowAdvanceType,
                m_WhatHappensWhenRowHitsRange,
                m_RowAdvancesPerCall,
                ref m_CurrentRowDirection);

            m_CurrentColumnIndex = GetNextIndex(
                m_CurrentColumnIndex,
                m_ColumnNumber,
                m_ColAdvanceType,
                m_WhatHappensWhenColHitsRange,
                m_ColumnAdvancesPerCall,
                ref m_CurrentColumnDirection);
        }

        private int GetNextIndex(
            int currentIndex,
            int maxExclusive,
            GridIndexAdvanceType advanceType,
            WhatHappensWhenHitsRange whatHappensWhenHitsRange,
            int advancesPerCall,
            ref int currentDirection)
        {
            if (maxExclusive <= 1)
                return 0;

            switch (advanceType)
            {
                case GridIndexAdvanceType.Random:
                    return GetRandomIndex(maxExclusive);

                case GridIndexAdvanceType.InOrder:
                    return GetNextInOrderIndex(
                        currentIndex,
                        maxExclusive,
                        whatHappensWhenHitsRange,
                        advancesPerCall,
                        ref currentDirection);

                default:
                    return currentIndex;
            }
        }

        private int GetNextInOrderIndex(
            int currentIndex,
            int maxExclusive,
            WhatHappensWhenHitsRange whatHappensWhenHitsRange,
            int advancesPerCall,
            ref int currentDirection)
        {
            if (advancesPerCall == 0)
                return currentIndex;

            switch (whatHappensWhenHitsRange)
            {
                case WhatHappensWhenHitsRange.Loop:
                    return WrapIndex(currentIndex + advancesPerCall, maxExclusive);

                case WhatHappensWhenHitsRange.ChangeDirection:
                    return GetBouncedIndex(
                        currentIndex,
                        maxExclusive,
                        advancesPerCall,
                        ref currentDirection);

                default:
                    return currentIndex;
            }
        }

        private int GetBouncedIndex(
            int currentIndex,
            int maxExclusive,
            int advancesPerCall,
            ref int currentDirection)
        {
            if (maxExclusive <= 1)
                return 0;

            if (currentDirection == 0)
                currentDirection = GetInitialDirection(advancesPerCall);

            int maxIndex = maxExclusive - 1;
            int period = maxIndex * 2;

            int stepAmount = Mathf.Abs(advancesPerCall);

            if (period > 0)
                stepAmount %= period;

            for (int i = 0; i < stepAmount; i++)
            {
                int nextIndex = currentIndex + currentDirection;

                if (nextIndex < 0 || nextIndex > maxIndex)
                {
                    currentDirection *= -1;
                    nextIndex = currentIndex + currentDirection;
                }

                currentIndex = nextIndex;
            }

            return currentIndex;
        }

        private int WrapIndex(int index, int maxExclusive)
        {
            if (maxExclusive <= 0)
                return 0;

            index %= maxExclusive;

            if (index < 0)
                index += maxExclusive;

            return index;
        }

        private int GetRandomIndex(int maxExclusive)
        {
            if (maxExclusive <= 0)
                return 0;

            return RandomController.Instance.Range(0, maxExclusive);
        }
    }
}