using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class DistanceComparison_CachedCondition : ICachedCondition
    {
        [Title("GameObjects")]
        [SerializeField]
        private SelfType m_From;
        [OdinSerialize, ShowInInspector, ShowIf(nameof(m_From), SelfType.CustomGameObject)]
        private GameObjectCalculatorData m_FromGameObject;

        [OdinSerialize, ShowInInspector]
        private GameObjectCalculatorData m_ToGameObject;


        [Title("Distance")]
        [SerializeField, Tooltip("If distance is (operation) than distance comparison")]
        FloatComparison.OperationType m_ComparisonOperation;
        [OdinSerialize, ShowInInspector]
        private CachedScoreData m_DistanceComparison;

        private GameObject m_Self;


        public DistanceComparison_CachedCondition() { }
        public DistanceComparison_CachedCondition(DistanceComparison_CachedCondition other)
        {
            m_From = other.m_From;
            m_FromGameObject = other.m_FromGameObject.CloneData();
            m_ToGameObject = other.m_ToGameObject.CloneData();
            m_ComparisonOperation = other.m_ComparisonOperation;
            m_DistanceComparison = other.m_DistanceComparison.CloneData();
            m_Self = other.m_Self;
        }

        public ICachedCondition Clone()
        {
            return new DistanceComparison_CachedCondition(this);
        }

        public GameObject GetGameObject()
        {
            return m_Self;
        }

        public bool IsFulfilled()
        {

            GameObject from = m_FromGameObject.CalculateGameObject();
            GameObject to   = m_ToGameObject.CalculateGameObject();
            float distanceComparison  = m_DistanceComparison.CalculateScore();
            distanceComparison *= distanceComparison; //optimization

            if (from == null)
            {
                Debug.LogError($"{nameof(DistanceComparison_CachedCondition)} from game object is null");
                return false;
            }
            if (to == null)
            {
                Debug.LogError($"{nameof(DistanceComparison_CachedCondition)} to game object is null");
                return false;
            }

            Vector3 distance = from.transform.position - to.transform.position;

            switch (m_ComparisonOperation)
            {
                case FloatComparison.OperationType.Less:
                    return distance.sqrMagnitude < distanceComparison;

                case FloatComparison.OperationType.Greater:
                    return distance.sqrMagnitude > distanceComparison;

                case FloatComparison.OperationType.Equal:
                    return distance.sqrMagnitude == distanceComparison;

                case FloatComparison.OperationType.LessOrEqual:
                    return distance.sqrMagnitude <= distanceComparison;

                case FloatComparison.OperationType.GreaterOrEqual:
                    return distance.sqrMagnitude >= distanceComparison;

                case FloatComparison.OperationType.NotEqual:
                    return distance.sqrMagnitude != distanceComparison;

                default:
                    break;
            }

            return false;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            if (gameObj == null)
            {
                Debug.LogError($"{nameof(DistanceComparison_CachedCondition)} input game object is null");
                return false;
            }
            m_Self = gameObj;
            bool allGood = true;
            allGood &= m_FromGameObject.SetGameObject(gameObj);
            allGood &= m_ToGameObject.SetGameObject(gameObj);
            allGood &= m_DistanceComparison.SetGameObject(gameObj);

            return allGood;

        }
    }
}
