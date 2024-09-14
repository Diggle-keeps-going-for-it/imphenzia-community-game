using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace Varia
{
    // Roughly matches UnityEditor.SerializedPropertyType
    public enum VariaSerializedValueType
    {
        Generic = -1,
        Integer = 0,
        Boolean = 1,
        Float = 2,
        String = 3,
        Color = 4,
        ObjectReference = 5,
        LayerMask = 6,
        Enum = 7,
        Vector2 = 8,
        Vector3 = 9,
        Vector4 = 10,
        Rect = 11,
        ArraySize = 12,
        Character = 13,
        AnimationCurve = 14,
        Bounds = 15,
        Gradient = 16,
        Quaternion = 17,
        ExposedReference = 18,
        FixedBufferSize = 19,
        Vector2Int = 20,
        Vector3Int = 21,
        RectInt = 22,
        BoundsInt = 23,
        ManagedReference = 24
    }

    // Holds a value of most any type. Works around unity serialization shortcomings.
    // Note, SerializeReference is only available on later version of Unity, and it doesn't support primitive types like float
    // So for now we're ignoring it
    [System.Serializable]
    public class VariaWeightedValue : ISerializationCallbackReceiver
    {
        [System.NonSerialized]
        public object value;

        [SerializeField]
        private Object objectReference;

        [SerializeField]
        [SerializeReference]
        private object managedReference;

        [SerializeField]
        private string serializedValue;

        [SerializeField]
        private VariaSerializedValueType serializedValueType = VariaSerializedValueType.ObjectReference;

        public float weight = 1;

        public void OnBeforeSerialize()
        {
            objectReference = null;
            managedReference = null;
            serializedValueType = VariaSerializedValueType.Generic;
            serializedValue = null;

            if (value == null)
            {
                serializedValueType = VariaSerializedValueType.ObjectReference;
                return;
            }

            var type = value.GetType();

            if(type == typeof(Object) || type.IsSubclassOf(typeof(Object)))
            {
                objectReference = (Object)value;
                serializedValueType = VariaSerializedValueType.ObjectReference;
                return;
            }
            else if (type == typeof(int))
            {
                serializedValue = value.ToString();
                serializedValueType = VariaSerializedValueType.Integer;
                return;
            }
            else if (type == typeof(bool))
            {
                serializedValue = value.ToString();
                serializedValueType = VariaSerializedValueType.Boolean;
                return;
            }
            else if (type == typeof(float))
            {
                serializedValue = value.ToString();
                serializedValueType = VariaSerializedValueType.Float;
                return;
            }
            else if (type == typeof(string))
            {
                serializedValue = value.ToString();
                serializedValueType = VariaSerializedValueType.String;
                return;
            }
            else if (type == typeof(Color))
            {
                var color = (Color)value;
                serializedValue = color.r.ToString(CultureInfo.InvariantCulture) + "," + color.g.ToString(CultureInfo.InvariantCulture) + "," + color.b.ToString(CultureInfo.InvariantCulture) + "," + color.a.ToString(CultureInfo.InvariantCulture);
                serializedValueType = VariaSerializedValueType.Color;
                return;
            }
            else if (type == typeof(LayerMask))
            {
                serializedValue = ((LayerMask)value).value.ToString();
                serializedValueType = VariaSerializedValueType.LayerMask;
                return;
            }
            else if (type.IsEnum)
            {
                serializedValue = ((int)value).ToString();
                serializedValueType = VariaSerializedValueType.Enum;
                return;
            }
            else if (type == typeof(Vector2))
            {
                var v = (Vector2)value;
                serializedValue = v.x.ToString(CultureInfo.InvariantCulture) + "," + v.y.ToString(CultureInfo.InvariantCulture);
                serializedValueType = VariaSerializedValueType.Vector2;
                return;
            }
            else if (type == typeof(Vector3))
            {
                var v = (Vector3)value;
                serializedValue = v.x.ToString(CultureInfo.InvariantCulture) + "," + v.y.ToString(CultureInfo.InvariantCulture) + "," + v.z.ToString(CultureInfo.InvariantCulture);
                serializedValueType = VariaSerializedValueType.Vector3;
                return;
            }
            else if (type == typeof(Vector4))
            {
                var v = (Vector4)value;
                serializedValue = v.x.ToString(CultureInfo.InvariantCulture) + "," + v.y.ToString(CultureInfo.InvariantCulture) + "," + v.z.ToString(CultureInfo.InvariantCulture) + "," + v.w.ToString(CultureInfo.InvariantCulture);
                serializedValueType = VariaSerializedValueType.Vector4;
                return;
            }
            else if (type == typeof(Rect))
            {
                var v = (Rect)value;
                serializedValue = v.x.ToString(CultureInfo.InvariantCulture) + "," + v.y.ToString(CultureInfo.InvariantCulture) + "," + v.width.ToString(CultureInfo.InvariantCulture) + "," + v.height.ToString(CultureInfo.InvariantCulture);
                serializedValueType = VariaSerializedValueType.Rect;
                return;
            }
            // TODO: ArraySize?
            else if (type == typeof(char))
            {
                serializedValue = value.ToString();
                serializedValueType = VariaSerializedValueType.Character;
                return;
            }
            // TODO: Animation Curve
            else if (type == typeof(Bounds))
            {
                var v = (Bounds)value;
                serializedValue = v.center.x.ToString(CultureInfo.InvariantCulture) + "," + v.center.y.ToString(CultureInfo.InvariantCulture) + "," + v.center.y.ToString(CultureInfo.InvariantCulture) + "," +
                    v.size.x.ToString(CultureInfo.InvariantCulture) + "," + v.size.y.ToString(CultureInfo.InvariantCulture) + "," + v.size.y.ToString(CultureInfo.InvariantCulture);
                serializedValueType = VariaSerializedValueType.Bounds;
                return;
            }
            // TODO: Gradient
            else if (type == typeof(Quaternion))
            {
                var v = (Quaternion)value;

                serializedValue = v.x.ToString(CultureInfo.InvariantCulture) + "," + v.y.ToString(CultureInfo.InvariantCulture) + "," + v.z.ToString(CultureInfo.InvariantCulture) + "," + v.w.ToString(CultureInfo.InvariantCulture);
                serializedValueType = VariaSerializedValueType.Quaternion;
                return;
            }

            else if (System.Attribute.GetCustomAttribute(type, typeof(System.SerializableAttribute)) != null)
            {
                managedReference = value;
                serializedValueType = VariaSerializedValueType.ManagedReference;
                return;
            }

            throw new System.Exception($"Cannot serialize type {type.FullName} {type.IsSubclassOf(typeof(UnityEngine.Object))}");
        }

        public void OnAfterDeserialize()
        {
            switch (serializedValueType)
            {
                case VariaSerializedValueType.ObjectReference:
                    value = objectReference;
                    objectReference = null;
                    break;
                case VariaSerializedValueType.ManagedReference:
                    value = managedReference;
                    managedReference = null;
                    break;
                case VariaSerializedValueType.Integer:
                    value = int.Parse(serializedValue);
                    serializedValue = null;
                    break;
                case VariaSerializedValueType.Boolean:
                    value = bool.Parse(serializedValue);
                    serializedValue = null;
                    break;
                case VariaSerializedValueType.Float:
                    value = float.Parse(serializedValue);
                    serializedValue = null;
                    break;
                case VariaSerializedValueType.String:
                    value = serializedValue;
                    serializedValue = null;
                    break;
                case VariaSerializedValueType.Color:
                    {
                        var values = serializedValue.Split(',').Select(float.Parse).ToList();
                        value = new Color(values[0], values[1], values[2], values[3]);
                        serializedValue = null;
                        break;
                    }
                case VariaSerializedValueType.LayerMask:
                    var lm = new LayerMask();
                    lm.value = int.Parse(serializedValue);
                    value = lm;
                    serializedValue = null;
                    break;
                case VariaSerializedValueType.Enum:
                    // TODO?
                    value = int.Parse(serializedValue);
                    serializedValue = null;
                    break;
                case VariaSerializedValueType.Vector2:
                    {
                        var values = serializedValue.Split(',').Select(float.Parse).ToList();
                        value = new Vector2(values[0], values[1]);
                        serializedValue = null;
                        break;
                    }
                case VariaSerializedValueType.Vector3:
                    {
                        var values = serializedValue.Split(',').Select(float.Parse).ToList();
                        value = new Vector3(values[0], values[1], values[2]);
                        serializedValue = null;
                        break;
                    }
                case VariaSerializedValueType.Vector4:
                    {
                        var values = serializedValue.Split(',').Select(float.Parse).ToList();
                        value = new Vector4(values[0], values[1], values[2], values[3]);
                        serializedValue = null;
                        break;
                    }
                case VariaSerializedValueType.Rect:
                    {
                        var values = serializedValue.Split(',').Select(float.Parse).ToList();
                        value = new Rect(values[0], values[1], values[2], values[3]);
                        serializedValue = null;
                        break;
                    }
                case VariaSerializedValueType.Character:
                    {
                        value = serializedValue[0];
                        serializedValue = null;
                        break;
                    }
                case VariaSerializedValueType.Bounds:
                    {
                        var values = serializedValue.Split(',').Select(float.Parse).ToList();
                        value = new Bounds(new Vector3(values[0], values[1], values[2]), new Vector3(values[3], values[4], values[5]));
                        serializedValue = null;
                        break;
                    }
                case VariaSerializedValueType.Quaternion:
                    {
                        var values = serializedValue.Split(',').Select(float.Parse).ToList();
                        value = new Quaternion(values[0], values[1], values[2], values[3]);
                        serializedValue = null;
                        break;
                    }
                default:
                    throw new System.Exception($"Cannot deserialize {serializedValueType}");
            }
        }

        public static object GetDefault(System.Type t)
        {
            if (t == null)
            {
                return null;
            }
            if (t == typeof(Color))
            {
                return Color.white;
            }
            if (t.IsValueType)
            {
                return System.Activator.CreateInstance(t);
            }
            return null;
        }

        public static bool CanSerialize(System.Type type)
        {
            if (type == typeof(Object) || type.IsSubclassOf(typeof(Object)))
            {
                return true;
            }
            else if (type == typeof(int))
            {
                return true;
            }
            else if (type == typeof(bool))
            {
                return true;
            }
            else if (type == typeof(float))
            {
                return true;
            }
            else if (type == typeof(string))
            {
                return true;
            }
            else if (type == typeof(Color))
            {
                return true;
            }
            else if (type == typeof(LayerMask))
            {
                return true;
            }
            else if (type.IsEnum)
            {
                return true;
            }
            else if (type == typeof(Vector2))
            {
                return true;
            }
            else if (type == typeof(Vector3))
            {
                return true;
            }
            else if (type == typeof(Vector4))
            {
                return true;
            }
            else if (type == typeof(Rect))
            {
                return true;
            }
            else if (type == typeof(char))
            {
                return true;
            }
            else if (type == typeof(Bounds))
            {
                return true;
            }
            else if (type == typeof(Quaternion))
            {
                return true;
            }
            else if (System.Attribute.GetCustomAttribute(type, typeof(System.SerializableAttribute)) != null)
            {
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Sets any property of any component to a value chosen randomly from a list.
    /// Only properties of types subclassing UnityEngine.Object are supported currently.
    /// </summary>
    [AddComponentMenu("Varia/Varia Random Value")]
    public class VariaRandomValue : VariaBehaviour
    {
        /// <summary>
        /// Specifices the specific component to set the value on.
        /// </summary>
        public Component target;

        /// <summary>
        /// The name of the property on the target component.
        /// </summary>
        public string property;

        /// <summary>
        /// The list of values to randomly choose from
        /// </summary>
        public List<VariaWeightedValue> values;

        /// <summary>
        /// If true, the random choice from <see cref="values"/> is weighted, otherwise they are chosen uniformly.
        /// </summary>
        public bool useWeights;

        public override void Apply(VariaContext context)
        {
            if (target == null)
            {
                return;
            }
            if(string.IsNullOrEmpty(property))
            {
                return;
            }

            // Pick value

            var r = UnityEngine.Random.value * values.Sum(x => useWeights ? x.weight : 1.0f);
            VariaWeightedValue value = null;
            foreach (var w in values)
            {
                r -= useWeights ? w.weight : 1.0f;
                if (r <= 0)
                {
                    value = w;
                    break;
                }
            }

            if (value != null)
            {
                var mirror = Mirror;
                var v = value.value;
                // Strip unity fake nulls
                if (v is Object o && o == null) v = null;
                mirror.setValue(target, v);

            }
        }

        public VariaMirror Mirror => string.IsNullOrEmpty(property) ? null : VariaReflection.EvalExpressionOrThrow(target.GetType(), property);
    }
}