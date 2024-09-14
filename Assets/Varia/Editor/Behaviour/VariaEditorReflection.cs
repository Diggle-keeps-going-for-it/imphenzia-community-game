using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Varia
{
    public static class VariaEditorReflection
    {
        public static IEnumerable<VariaProperty> GetVariaProperties(Renderer r, bool materialProperties = true, bool propertyBlockProperties = true)
        {
            for (var materialIndex = 0; materialIndex < r.sharedMaterials.Length; materialIndex++)
            {
                var material = r.sharedMaterials[materialIndex];
                if (material != null)
                {
                    foreach (var materialProperty in MaterialEditor.GetMaterialProperties(new[] { material }))
                    {
                        if (materialProperties)
                            yield return ToMaterialVariaProperty(materialIndex, materialProperty);
                        if (propertyBlockProperties)
                            yield return ToPropertyBlockVariaProperty(materialIndex, materialProperty);
                    }
                }
            }
        }

        public static VariaProperty ToPropertyBlockVariaProperty(int materialIndex, MaterialProperty mp)
        {
            return new VariaProperty
            {
                name = mp.name,
                expression = GetPropertyBlockExpression(materialIndex, mp),
                canRead = true,
                canWrite = true,
                propertyType = ToType(mp.type),
            };
        }

        public static string GetPropertyBlockExpression(int materialIndex, MaterialProperty mp)
        {
            return $"propertyBlock[{materialIndex}].{mp.name}.{mp.type}";
        }

        public static VariaProperty ToMaterialVariaProperty(int materialIndex, MaterialProperty mp)
        {
            return new VariaProperty
            {
                name = mp.name,
                expression = GetMaterialExpression(materialIndex, mp),
                canRead = true,
                canWrite = true,
                propertyType = ToType(mp.type),
            };
        }

        public static string GetMaterialExpression(int materialIndex, MaterialProperty mp)
        {
            return $"material[{materialIndex}].{mp.name}.{mp.type}";
        }


        private static System.Type ToType(MaterialProperty.PropType propType)
        {
            switch(propType)
            {
                case MaterialProperty.PropType.Color:
                    return typeof(Color);
                case MaterialProperty.PropType.Float:
                    return typeof(float);
                case MaterialProperty.PropType.Range:
                    return typeof(float[]);
                case MaterialProperty.PropType.Texture:
                    return typeof(Texture);
                case MaterialProperty.PropType.Vector:
                    return typeof(Vector4);
                default:
                    throw new System.Exception($"Unknown type {propType}");
            }
        }
    }
}