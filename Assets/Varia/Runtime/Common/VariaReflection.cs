using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

namespace Varia
{
    /// <summary>
    /// A cut down version of System.Reflection.PropertyInfo
    /// </summary>
    public class VariaProperty
    {
        public string name;

        public string expression;

        public Type propertyType;

        public bool canWrite;

        public bool canRead;
    }

    /// <summary>
    /// Abstraction for a a readable or writable property of a given instance.
    /// </summary>
    public class VariaMirror
    {
        public Type propertyType;

        public Action<object, object> setValue;

        public Func<object, object> getValue;
    }

    /// <summary>
    /// VariaReflection is a simplified version of C# reflection, with an emphasis on reading and writing.
    /// 
    /// The main feature is given a expression, it lets you read and write values for the field corresponding to that expression.
    /// An expression is build as follows:
    ///   expression ::= property_name |
    ///                  property_name "." expression |
    ///                  "propertyBlock" "." material_property_name "." type
    ///                  
    /// The first form indicates a property of the target object itself.
    /// The second form evaluates the sub-expression on the value of named property of the target object.
    /// The third form corresponds to Renderer.SetPropertyBlock (https://docs.unity3d.com/ScriptReference/Renderer.SetPropertyBlock.html)
    /// 
    /// Material property block properties behave particularly strangely:
    ///  * They must have the type encoded in the name as it's not available at runtime.
    ///  * They are not listed when exploring the properties of an object (though you can use VariaMaterialPropertyBlockReflection to get them)
    /// 
    /// </summary>
    public static class VariaReflection
    {
        private static Dictionary<Type, List<VariaProperty>> propertyCache;

        private static Dictionary<Type, Dictionary<string, VariaMirror>> mirrorCache;

        private static HashSet<Type> recursible;

        static VariaReflection()
        {
            // Setup properties
            recursible = new HashSet<Type>();
            propertyCache = new Dictionary<Type, List<VariaProperty>>();
            mirrorCache = new Dictionary<Type, Dictionary<string, VariaMirror>>();

            /*
            VariaProperty MakeProperty<T>(string name)
            {
                var propInfo = typeof(T).GetProperty(name);
                return new VariaProperty { name = name, expression = name, propertyType = propInfo.PropertyType, canRead = propInfo.CanRead, canWrite = propInfo.CanWrite };
            }
            */
            VariaProperty MakeField<T>(string name)
            {
                var propInfo = typeof(T).GetField(name);
                return new VariaProperty { name = name, expression = name, propertyType = propInfo.FieldType, canRead = true, canWrite = true };
            }

            recursible.Add(typeof(Vector3));
            propertyCache[typeof(Vector3)] = new List<VariaProperty>
            {
                MakeField<Vector3>("x"),
                MakeField<Vector3>("y"),
                MakeField<Vector3>("z"),
            };
        }

        public static List<VariaProperty> GetProperties(Type targetType)
        {
            if(propertyCache.TryGetValue(targetType, out var properties))
            {
                return properties;
            }

            return propertyCache[targetType] = GetPropertiesInner(targetType).ToList();
        }

        public static void SetValue(object o, string expression, object value)
        {
            EvalExpression(o.GetType(), expression).setValue(o, value);
        }

        public static object GetValue(object o, string expression)
        {
            return EvalExpression(o.GetType(), expression).getValue(o);
        }

        private static IEnumerable<VariaProperty> GetPropertiesInner(Type targetType)
        {
            foreach (var memberInfo in targetType.GetMembers(BindingFlags.Instance | BindingFlags.Public))
            {
                VariaProperty variaProperty;
                if (memberInfo.MemberType.HasFlag(MemberTypes.Field))
                {
                    var fieldInfo = (FieldInfo)memberInfo;
                    variaProperty = new VariaProperty
                    {
                        name = fieldInfo.Name,
                        expression = fieldInfo.Name,
                        propertyType = fieldInfo.FieldType,
                        canRead = true,
                        canWrite = true,
                    };
                }
                else if (memberInfo.MemberType.HasFlag(MemberTypes.Property))
                {
                    var propertyInfo = (PropertyInfo)memberInfo;
                    variaProperty = new VariaProperty
                    {
                        name = propertyInfo.Name,
                        expression = propertyInfo.Name,
                        propertyType = propertyInfo.PropertyType,
                        canRead = propertyInfo.CanWrite,
                        canWrite = propertyInfo.CanRead,
                    };
                }
                else
                {
                    continue;
                }

                yield return variaProperty;

                // For now we only recurse on a few types.
                if (recursible.Contains(variaProperty.propertyType) && variaProperty.canRead)
                {
                    // Recurse
                    foreach(var subProperty in GetProperties(variaProperty.propertyType))
                    {
                        yield return new VariaProperty
                        {
                            name = subProperty.name,
                            expression = variaProperty.name + "." + subProperty.expression,
                            propertyType = subProperty.propertyType,
                            canRead = variaProperty.canRead && subProperty.canRead,
                            canWrite = variaProperty.canRead && variaProperty.canWrite && subProperty.canWrite,
                        };
                    }
                }
            }
        }

        public static VariaMirror EvalExpressionOrThrow(Type targetType, string expression)
        {
            var mirror = EvalExpression(targetType, expression);
            if(mirror == null)
            {
                throw new Exception($"Couldn't find property {expression} on {targetType.FullName}");
            }
            return mirror;
        }


        public static VariaMirror EvalExpression(Type targetType, string expression)
        {
            if (targetType == null || string.IsNullOrEmpty(expression))
                return null;

            // TODO: Fancier parsing?
            var propertyNames = expression.Split(new[] { '.' });
            if(propertyNames.Length == 0)
            {
                return null;
            }

            // Special case for material properties
            if (propertyNames[0].StartsWith("propertyBlock[") &&
                typeof(Renderer).IsAssignableFrom(targetType) &&
                Regex.Match(propertyNames[0], @"propertyBlock\[(\d+)\]") is Match m)
            {
                return GetPropertyBlockMirror(int.Parse(m.Groups[1].Value), propertyNames[1], propertyNames[2]);
            }
            if (propertyNames[0].StartsWith("material[") &&
                typeof(Renderer).IsAssignableFrom(targetType) &&
                Regex.Match(propertyNames[0], @"material\[(\d+)\]") is Match m2)
            {
                return GetMaterialPropertyMirror(int.Parse(m2.Groups[1].Value), propertyNames[1], propertyNames[2]);
            }

            VariaMirror Recurse(int i, Type subType)
            {
                var mirror = GetMirror(subType, propertyNames[i]);
                if (mirror == null)
                    return null;

                if (i == propertyNames.Length -1)
                {
                    return mirror;
                }
                var subMirror = Recurse(i + 1, mirror.propertyType);
                return new VariaMirror
                {
                    propertyType = subMirror.propertyType,
                    setValue = (t, v) =>
                    {
                        var subValue = mirror.getValue(t);
                        subMirror.setValue(subValue, v);
                        mirror.setValue(t, subValue);
                    },
                    getValue = t => subMirror.getValue(mirror.getValue(t)),
                };

            }

            return Recurse(0, targetType);
        }

        #region Materials

        // Implicitly on type Renderer
        private static VariaMirror GetPropertyBlockMirror(int materialIndex, string propertyName, string typeName)
        {
            // Values match MaterialProperty.PropType
            Type type;
            switch (typeName)
            {
                case "Color":
                    type = typeof(Color); break;
                case "Float":
                    type = typeof(float); break;
                case "Range":
                     type = typeof(float[]); break;
                case "Texture":
                    type = typeof(Texture); break;
                case "Vector":
                    type = typeof(Vector4); break;
                default:
                    throw new Exception($"Unknown type name for property block: {typeName}");
            }
            object Get(object o)
            {
                var r = (Renderer)o;
                var pb = new MaterialPropertyBlock();
                if (r.sharedMaterials.Length <= materialIndex)
                    throw new Exception($"Couldn't find material with index {materialIndex} on {r}");
                r.GetPropertyBlock(pb, materialIndex);
                switch (typeName)
                {
                    case "Color":
                        return pb.GetColor(propertyName);
                    case "Float":
                        return pb.GetFloat(propertyName);
                    case "Range":
                        return pb.GetFloatArray(propertyName);
                    case "Texture":
                        return pb.GetTexture(propertyName);
                    case "Vector":
                        return pb.GetVector(propertyName);
                    default:
                        throw new Exception($"Unknown type name for property block: {typeName}");
                }
            }
            void Set(object o, object value)
            {
                var r = (Renderer)o;
                var pb = new MaterialPropertyBlock();
                if (r.sharedMaterials.Length <= materialIndex)
                    throw new Exception($"Couldn't find material with index {materialIndex} on {r}");
                r.GetPropertyBlock(pb, materialIndex);
                switch (typeName)
                {
                    case "Color":
                        pb.SetColor(propertyName, (Color)value); break;
                    case "Float":
                        pb.SetFloat(propertyName, (float)value); break;
                    case "Range":
                        pb.SetFloatArray(propertyName, (float[])value); break;
                    case "Texture":
                        pb.SetTexture(propertyName, (Texture)value); break;
                    case "Vector":
                        pb.SetVector(propertyName, (Vector4)value); break;
                    default:
                        throw new Exception($"Unknown type name for property block: {typeName}");
                }
                r.SetPropertyBlock(pb, materialIndex);
            }
            return new VariaMirror
            {
                propertyType = type,
                getValue = Get,
                setValue = Set,
            };
        }

        // Implicitly on type Renderer
        private static VariaMirror GetMaterialPropertyMirror(int materialIndex, string propertyName, string typeName)
        {
            // Values match MaterialProperty.PropType
            Type type;
            switch (typeName)
            {
                case "Color":
                    type = typeof(Color); break;
                case "Float":
                    type = typeof(float); break;
                case "Range":
                    type = typeof(float[]); break;
                case "Texture":
                    type = typeof(Texture); break;
                case "Vector":
                    type = typeof(Vector4); break;
                default:
                    throw new Exception($"Unknown type name for property block: {typeName}");
            }
            object Get(object o)
            {
                var r = (Renderer)o;
                if (r.sharedMaterials.Length <= materialIndex)
                    throw new Exception($"Couldn't find material with index {materialIndex} on {r}");
                var material = r.sharedMaterials[materialIndex];
                switch (typeName)
                {
                    case "Color":
                        return material.GetColor(propertyName);
                    case "Float":
                        return material.GetFloat(propertyName);
                    case "Range":
                        return material.GetFloatArray(propertyName);
                    case "Texture":
                        return material.GetTexture(propertyName);
                    case "Vector":
                        return material.GetVector(propertyName);
                    default:
                        throw new Exception($"Unknown type name for property block: {typeName}");
                }
            }
            void Set(object o, object value)
            {
                if (!Application.isPlaying)
                {
                    Debug.LogWarning("Material properties cannot be set in the editor. Consider using property blocks instead.");
                    return;
                }
                var r = (Renderer)o;
                if (r.sharedMaterials.Length <= materialIndex)
                    throw new Exception($"Couldn't find material with index {materialIndex} on {r}");
                var material = r.materials[materialIndex];
                switch (typeName)
                {
                    case "Color":
                        material.SetColor(propertyName, (Color)value); break;
                    case "Float":
                        material.SetFloat(propertyName, (float)value); break;
                    case "Range":
                        material.SetFloatArray(propertyName, (float[])value); break;
                    case "Texture":
                        material.SetTexture(propertyName, (Texture)value); break;
                    case "Vector":
                        material.SetVector(propertyName, (Vector4)value); break;
                    default:
                        throw new Exception($"Unknown type name for property block: {typeName}");
                }
            }
            return new VariaMirror
            {
                propertyType = type,
                getValue = Get,
                setValue = Set,
            };
        }
        #endregion

        private static VariaMirror GetMirror(Type targetType, string propertyName)
        {
            if(!mirrorCache.TryGetValue(targetType, out var mirrors))
            {
                mirrors = mirrorCache[targetType] = new Dictionary<string, VariaMirror>();
            }

            if(mirrors.TryGetValue(propertyName, out var mirror))
            {
                return mirror;
            }

            var memberInfos = targetType.GetMember(propertyName);
            foreach (var memberInfo in memberInfos)
            {
                if (memberInfo.MemberType == MemberTypes.Property)
                {
                    var propertyInfo = (PropertyInfo)memberInfo;
                    return mirrors[propertyName] = new VariaMirror
                    {
                        propertyType = propertyInfo.PropertyType,
                        getValue = propertyInfo.GetValue,
                        setValue = propertyInfo.SetValue,
                    };
                }

                if (memberInfo.MemberType == MemberTypes.Field)
                {
                    var fieldInfo = (FieldInfo)memberInfo;
                    return mirrors[propertyName] = new VariaMirror
                    {
                        propertyType = fieldInfo.FieldType,
                        getValue = fieldInfo.GetValue,
                        setValue = fieldInfo.SetValue,
                    };
                }
            }
            return null;
        }
    }
}
