using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SilverPillar.Core
{
    public class Median
    {
        public static float Calculate(List<float> source)
        {
            var sorted = source.OrderBy(n => n).ToList();
            int count = sorted.Count;
            int mid = count / 2;
            return (count % 2 != 0)
                ? sorted[mid]
                : (sorted[mid - 1] + sorted[mid]) / 2f;
        }
    }
}

