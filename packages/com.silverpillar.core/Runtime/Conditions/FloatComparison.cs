using UnityEngine;

namespace SilverPillar.Core
{
    public class FloatComparison
    {
        public enum OperationType
        {
            Less,
            Greater,
            Equal,
            LessOrEqual,
            MoreOrEqual
        }

        public static bool Compare(float first, OperationType operationType, float second)
        {
            switch (operationType)
            {
                case OperationType.Less:
                    return first < second;
                case OperationType.Greater:
                    return first > second;
                case OperationType.Equal:
                    return Mathf.Approximately(first, second);
                case OperationType.LessOrEqual:
                    return first <= second;
                case OperationType.MoreOrEqual:
                    return first >= second;
                default:
                    break;
            }
            return false;
        }

    }
}
