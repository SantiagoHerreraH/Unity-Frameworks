using System;
using UnityEngine;

namespace SilverPillar.Core
{

    [CreateAssetMenu(fileName = "AnimationCurveData", menuName = "SilverPillar/Core/AnimationCurveData")]
    public class AnimationCurveData : SaveableScriptableObject
    {
        public AnimationCurve Curve;
    }

    [Serializable]
    public struct ValueTransformation
    {
        public float MinInput;
        public float MaxInput;

        public AnimationCurve RemapCurve;

        public float MinOutput;
        public float MaxOutput;

        public float TransformValue(float input)
        {

            float clamped = Mathf.Clamp(input, MinInput, MaxInput);
            float defaultPercentage = RemapCurve.Evaluate(clamped / MaxInput);
            return ((MaxOutput - MinOutput) * defaultPercentage) + MinOutput;
        }
    }
}