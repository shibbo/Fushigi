using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace Fushigi.Byml.Serializer
{
    public static class BymlSerialize
    {
        public static T Deserialize<T>(IBymlNode node)
        {
            T instance = (T)CreateInstance(typeof(T));
            Deserialize(instance, node);
            return instance;
        }

        public static void Deserialize(object obj, IBymlNode node)
        {
            var hashTable = node as BymlHashTable;

            var properties = obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            for (int i = 0; i < properties.Length; i++)
            {
                //Only load properties with byaml attributes
                var byamlAttribute = properties[i].GetCustomAttribute<BymlProperty>();
                var bymlIgnoreAttribute = properties[i].GetCustomAttribute<BymlIgnore>();

                if (bymlIgnoreAttribute != null)
                    continue;

                Type type = properties[i].PropertyType;
                Type nullableType = Nullable.GetUnderlyingType(type);
                if (nullableType != null)
                    type = nullableType;

                //Set custom keys as property name if used
                string name = byamlAttribute != null && byamlAttribute.Key != null ? byamlAttribute.Key : properties[i].Name;

                //Skip properties that are not present
                if (!hashTable.ContainsKey(name))
                    continue;

                SetValues(properties[i], type, obj, hashTable[name]);
            }
        }

        static void SetValues(object property, Type type, object section, dynamic value)
        {
             if (value is BymlArrayNode)
            {
                if (type == typeof(System.Numerics.Vector3))
                {
                    var values = value as BymlArrayNode;
                    var vec3 = new System.Numerics.Vector3(
                        ((dynamic)values[0]).Data,
                        ((dynamic)values[0]).Data,
                        ((dynamic)values[0]).Data);
                    SetValue(property, section, vec3);
                }
                else
                {
                    var list = (value as BymlArrayNode).Array;
                    var array = InstantiateType<IList>(type);

                    Type elementType = type.GetTypeInfo().GetElementType();
                    if (type.IsGenericType && elementType == null)
                        elementType = type.GetGenericArguments()[0];

                    for (int j = 0; j < list.Count; j++)
                    {
                        if (list[j] is BymlHashTable)
                        {
                            var instance = CreateInstance(elementType);
                            Deserialize(instance, list[j] as BymlHashTable);
                            array.Add(instance);
                        }
                        else if (list[j] is BymlArrayNode)
                        {
                            var subList = list[j] as BymlArrayNode;

                            var instance = CreateInstance(elementType);
                            if (instance is IList)
                            {
                                for (int k = 0; k < subList.Array.Count; k++)
                                    ((IList)instance).Add(subList.Array[k]);
                            }
                            array.Add(instance);
                        }
                        else
                            array.Add(((dynamic)list[j]).Data);
                    }
                    SetValue(property, section, array);
                }
            }
            else if (value is BymlHashTable)
            {
                if (type == typeof(System.Numerics.Vector3))
                {
                    var values = value as BymlHashTable;
                    var vec3 = new System.Numerics.Vector3(
                        ((dynamic)values["X"]).Data,
                        ((dynamic)values["Y"]).Data,
                        ((dynamic)values["Z"]).Data);
                    SetValue(property, section, vec3);
                }
                else if (type == typeof(Dictionary<string, dynamic>))
                {
                    var values = value as BymlHashTable;

                    Dictionary<string, dynamic> dict = new Dictionary<string, dynamic>();
                    foreach (var pair in values.Pairs)
                        dict.Add(pair.Name, ((dynamic)pair.Value).Data);

                    SetValue(property, section, dict);
                }
                else
                {
                    var instance = CreateInstance(type);
                    Deserialize(instance, value);
                    SetValue(property, section, instance);
                }
            }
            else
                SetValue(property, section, value.Data);
        }

        static void SetValue(object property, object instance, object value)
        {
            if (property is PropertyInfo)
            {
                Type nullableType = Nullable.GetUnderlyingType(((PropertyInfo)property).PropertyType);
                if (nullableType != null && nullableType.GetTypeInfo().IsEnum)
                {
                    value = Enum.ToObject(nullableType, value);
                }
            }

            if (property is PropertyInfo)
                ((PropertyInfo)property).SetValue(instance, value);
            else if (property is FieldInfo)
                ((FieldInfo)property).SetValue(instance, value);
        }

        public static BymlHashTable Serialize(object section)
        {
            return SetHashTable(section);
        }

        static BymlHashTable SetHashTable(object section)
        {
            BymlHashTable bymlProperties = new BymlHashTable();

            var properties = section.GetType().GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            for (int i = 0; i < properties.Length; i++)
            {
                //Only load properties with byaml attributes
                var byamlAttribute = properties[i].GetCustomAttribute<BymlProperty>();
                var bymlIgnoreAttribute = properties[i].GetCustomAttribute<BymlIgnore>();

                if (bymlIgnoreAttribute != null)
                    continue;

                var value = properties[i].GetValue(section);

                if (byamlAttribute != null)
                {
                    //Skip null optional values
                    if (byamlAttribute.Optional && value == null)
                        continue;

                    //If value is null, use a default value
                    if (value == null)
                        value = byamlAttribute.DefaultValue;
                }

                //Set custom keys as property name if used
                string name = byamlAttribute != null && byamlAttribute.Key != null ? byamlAttribute.Key : properties[i].Name;

                if (value == null)
                    continue;

                var node = SetBymlValue(properties[i].GetValue(section));
                bymlProperties.AddNode(node.Id, node, name); 
            }
            return bymlProperties;
        }

        static IBymlNode SetBymlValue(object value)
        {
            Type type = value.GetType();
            Type nullableType = Nullable.GetUnderlyingType(type);
            if (nullableType != null && nullableType.GetTypeInfo().IsEnum)
                type = nullableType;
            if (type.IsEnum)
                type = Enum.GetUnderlyingType(type);

            if (type == typeof(bool)) return new BymlNode<bool>(BymlNodeId.Bool, (bool)value);
            else if (type == typeof(float)) return new BymlNode<float>(BymlNodeId.Float, (float)value);
            else if (type == typeof(int)) return new BymlNode<int>(BymlNodeId.Int, (int)value);
            else if (type == typeof(uint)) return new BymlNode<uint>(BymlNodeId.UInt, (uint)value);
            else if (type == typeof(string)) return new BymlNode<string>(BymlNodeId.String, (string)value);
            else if (type == typeof(double)) return new BymlBigDataNode<double>(BymlNodeId.Double, (double)value);
            else if (type == typeof(ulong)) return new BymlBigDataNode<ulong>(BymlNodeId.UInt64, (ulong)value);
            else if (type == typeof(long)) return new BymlBigDataNode<long>(BymlNodeId.Int64, (long)value);
            else if (type == typeof(byte[])) return new BymlBigDataNode<byte[]>(BymlNodeId.Bin, (byte[])value);
            else if (typeof(IList).GetTypeInfo().IsAssignableFrom(type))
            {
                BymlArrayNode savedValues = new BymlArrayNode();
                savedValues.Array = new List<IBymlNode>();

                foreach (var val in ((IList)value))
                    savedValues.Array.Add(SetBymlValue(val));
                return savedValues;
            }
            else if (IsTypeSerializableObject(type))
                return SetHashTable(value);

            throw new Exception($"Type {type.Name} is not supported as BYAML data.");
        }


        static bool IsTypeSerializableObject(Type type)
        {
            return Attribute.IsDefined(type, typeof(SerializableAttribute));
        }


        private static bool IsTypeList(Type type)
        {
            return type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(IList));
        }

        private static T InstantiateType<T>(Type type)
        {
            // Validate if the given type is compatible with the required one.
            if (!typeof(T).GetTypeInfo().IsAssignableFrom(type))
            {
                throw new Exception($"Type {type.Name} cannot be used as BYAML object data.");
            }
            // Return a new instance.
            return (T)CreateInstance(type);
        }

        static object CreateInstance(Type type)
        {
            return Activator.CreateInstance(type, true);
        }
    }
}
