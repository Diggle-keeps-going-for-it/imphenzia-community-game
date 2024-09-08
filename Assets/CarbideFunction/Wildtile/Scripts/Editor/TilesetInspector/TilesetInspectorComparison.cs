using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace CarbideFunction.Wildtile.Editor
{
    internal static class TilesetInspectorComparison
    {
        public class ComparisonResult<Value>
        {
            public List<KeyValuePair<Value, Value>> validComponents = new List<KeyValuePair<Value, Value>>();
            public List<Value> invalidComponentsOnCurrent =   new List<Value>();
            public List<Value> invalidComponentsOnConnected = new List<Value>();
        }

        public delegate HashResult Hash<HashResult, Input>(Input input);

        public static ComparisonResult<Value> Compare<HashResult, Value>(Hash<HashResult, Value> hasher, IEnumerable<Value> currentComponents, IEnumerable<Value> connectedComponents) where HashResult : IComparable
        {
            var result = new ComparisonResult<Value>();
            var currentComponentsHashZip = HashAndZip(hasher, currentComponents);
            var connectedComponentsHashZip = HashAndZip(hasher, connectedComponents);

            var currentComponentIndex = 0;
            var connectedComponentIndex = 0;

            while (currentComponentIndex < currentComponentsHashZip.Count
                && connectedComponentIndex < connectedComponentsHashZip.Count)
            {
                var currentComponent = currentComponentsHashZip[currentComponentIndex];
                var connectedComponent = connectedComponentsHashZip[connectedComponentIndex];

                var compareResult = currentComponent.Key.CompareTo(connectedComponent.Key);
                if (compareResult < 0)
                {
                    result.invalidComponentsOnCurrent.Add(currentComponent.Value);
                    currentComponentIndex++;
                }
                else if (compareResult > 0)
                {
                    result.invalidComponentsOnConnected.Add(connectedComponent.Value);
                    connectedComponentIndex++;
                }
                else
                {
                    Assert.AreEqual(compareResult, 0);
                    currentComponentIndex++;
                    connectedComponentIndex++;
                    result.validComponents.Add(new KeyValuePair<Value, Value>(currentComponent.Value, connectedComponent.Value));
                }
            }

            result.invalidComponentsOnCurrent.AddRange(currentComponentsHashZip.Skip(currentComponentIndex).Select(kvPair => kvPair.Value));
            result.invalidComponentsOnConnected.AddRange(connectedComponentsHashZip.Skip(connectedComponentIndex).Select(kvPair => kvPair.Value));

            return result;
        }

        private static List<KeyValuePair<HashResult, Value>> HashAndZip<HashResult, Value>(Hash<HashResult, Value> hasher, IEnumerable<Value> values)
        {
            return values
                .Select(component => new KeyValuePair<HashResult, Value>(hasher(component), component))
                .OrderBy(kvPair => kvPair.Key)
                .ToList();
        }

        public class PreviouslyMatchedComparisonResult<Value>
        {
            public List<KeyValuePair<Value, Value>> validComponents = null;
            public List<KeyValuePair<Value, Value>> invalidPriorMatchedComponents = null;
            public List<Value> invalidComponentsOnCurrent = null;
            public List<Value> invalidComponentsOnConnected = null;

            public bool IsCompletelyValid =>
                   invalidPriorMatchedComponents.Count == 0
                && invalidComponentsOnCurrent.Count == 0
                && invalidComponentsOnConnected.Count == 0;
        }

        public static PreviouslyMatchedComparisonResult<Value> CompareAfterMatching<HashResult, Value>(Hash<HashResult, Value> priorMatchHash, Hash<HashResult, Value> hasher, IEnumerable<Value> currentComponents, IEnumerable<Value> connectedComponents) where HashResult : IComparable
        {
            var result = new PreviouslyMatchedComparisonResult<Value>();

            var perfectMatchResults = Compare(hasher, currentComponents, connectedComponents);

            result.validComponents = perfectMatchResults.validComponents;

            var secondaryMatchResults = Compare(priorMatchHash, perfectMatchResults.invalidComponentsOnCurrent, perfectMatchResults.invalidComponentsOnConnected);

            result.invalidPriorMatchedComponents = secondaryMatchResults.validComponents;
            result.invalidComponentsOnCurrent = secondaryMatchResults.invalidComponentsOnCurrent;
            result.invalidComponentsOnConnected = secondaryMatchResults.invalidComponentsOnConnected;

            return result;
        }
    }
}
