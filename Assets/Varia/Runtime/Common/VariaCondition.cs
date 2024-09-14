using System;
using UnityEngine;

namespace Varia
{
    [Serializable]
    public class VariaCondition
    {
        public VariaConditionType conditionType;

        [Range(0, 1)]
        public float randomChance = 1;

        public VariaComparison comparison;

        [Min(0)]
        public int depth = 3;
    }
}