using System;
using UnityEngine;

namespace SilverPillar.Core
{

    [CreateAssetMenu(fileName = "SaveableScriptableObject", menuName = "Scriptable Objects/AnimationCurveData")]
    public class AnimationCurveData : SaveableScriptableObject
    {
        public AnimationCurve Curve;
    }

    [Serializable]
    public struct ValueTransformation
    {
        public float MinInput;
        public float MaxInput;

        public SO_Ref<AnimationCurveData> RemapTo;

        public float MinOutput;
        public float MaxOutput;

        public float TransformValue(float input)
        {

            float clamped = Mathf.Clamp(input, MinInput, MaxInput);
            float defaultPercentage = RemapTo.Get().Curve.Evaluate(clamped / MaxInput);
            return ((MaxOutput - MinOutput) * defaultPercentage) + MinOutput;
        }
    }
}