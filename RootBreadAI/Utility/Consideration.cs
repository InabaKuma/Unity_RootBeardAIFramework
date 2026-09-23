using System;
using UnityEngine;

namespace RootBeard.Framework
{
    /// <summary>
    /// 效用考虑因素：将一个 0~1 的输入值乘以权重，得到该因素的得分。
    /// </summary>
    public class Consideration
    {
        public float Weight { get; }
        private readonly Func<float> input;
        private readonly AnimationCurve curve;

        public Consideration(Func<float> input, float weight, AnimationCurve curve = null)
        {
            this.input = input;
            this.Weight = weight;
            this.curve = curve;
        }

        public float GetScore()
        {
            if (input == null) return 0f;

            float value = Mathf.Clamp01(input());
            if (curve != null)
            {
                value = Mathf.Clamp01(curve.Evaluate(value));
            }
            return value * Weight;
        }
    }
}