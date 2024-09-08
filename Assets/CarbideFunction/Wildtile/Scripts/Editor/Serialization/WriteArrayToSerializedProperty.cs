using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;

namespace CarbideFunction.Wildtile.Editor
{
    internal static class WriteArrayToSerializedProperty
    {
        public static void Vector3Array(IList<Vector3> vectorArray, SerializedProperty property)
        {
            GenericArray(vectorArray, property, (vector, elementProperty) => {
                elementProperty.vector3Value = vector;
            });
        }

        public static void Vector4Array(IList<Vector4> vectorArray, SerializedProperty property)
        {
            GenericArray(vectorArray, property, (vector, elementProperty) => {
                elementProperty.vector4Value = vector;
            });
        }

        public static void IntArray(IList<int> vectorArray, SerializedProperty property)
        {
            GenericArray(vectorArray, property, (vector, elementProperty) => {
                elementProperty.intValue = vector;
            });
        }

        public static void GenericArray<SourceType>
        (
            IList<SourceType> array,
            SerializedProperty property,
            Action<SourceType, SerializedProperty> writeSingleElementToProperty
        )
        {
            Assert.IsNotNull(property);
            Assert.IsNotNull(array);
            Assert.IsTrue(property.isArray);
            property.arraySize = array.Count;

            for (var i = 0; i < array.Count; ++i)
            {
                var elementProperty = property.GetArrayElementAtIndex(i);
                var sourceElement = array[i];
                writeSingleElementToProperty(sourceElement, elementProperty);
            }
        }
    }
}
