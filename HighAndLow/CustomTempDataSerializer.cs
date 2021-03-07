using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure;
using System.Buffers;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Collections;

namespace HighAndLow
{
    public class CustomTempDataSerializer : TempDataSerializer
    {

        private const string TYPE_PROPERTY_NAME = "TypeDiscriminator";
        private const string VALUE_PROPERTY_NAME = "ValueObject";

        private static JsonSerializerOptions _SerializerOptions;
        public override IDictionary<string, object> Deserialize(byte[] value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (value.Length == 0)
            {
                return new Dictionary<string, object>();
            }

            using var jsonDocument = JsonDocument.Parse(value);
            var rootElement = jsonDocument.RootElement;
            return DeserializeDictionary(rootElement);
        }

        private IDictionary<string, object> DeserializeDictionary(JsonElement rootElement)
        {
            var deserialized = new Dictionary<string, object>(StringComparer.Ordinal);

            foreach (var item in rootElement.EnumerateObject())
            {
                object deserializedValue = null;
                switch (item.Value.ValueKind)
                {
                    case JsonValueKind.False:
                    case JsonValueKind.True:
                        deserializedValue = item.Value.GetBoolean();
                        break;

                    case JsonValueKind.Number:
                        if (item.Value.TryGetInt32(out var intValue))
                        {
                            deserializedValue = intValue;
                        }
                        else if (item.Value.TryGetInt64(out var longValue))
                        {
                            deserializedValue = longValue;
                        }
                        else
                        {
                            deserializedValue = item.Value.GetDecimal();
                        }

                        break;

                    case JsonValueKind.String:
                        if (item.Value.TryGetGuid(out var guid))
                        {
                            deserializedValue = guid;
                        }
                        else if (item.Value.TryGetDateTime(out var dateTime))
                        {
                            deserializedValue = dateTime;
                        }
                        else
                        {
                            deserializedValue = item.Value.GetString();
                        }
                        break;

                    case JsonValueKind.Null:
                        deserializedValue = null;
                        break;

                    case JsonValueKind.Object:
                        Type objectType = null;
                        JsonElement? objectValue = null;
                        foreach (var objectElement in item.Value.EnumerateObject())
                        {
                            switch (objectElement.Name)
                            {
                                case TYPE_PROPERTY_NAME:
                                    objectType = Type.GetType(objectElement.Value.GetString());
                                    break;

                                case VALUE_PROPERTY_NAME:
                                    objectValue = objectElement.Value;
                                    break;

                                default:
                                    break;
                            }
                        }

                        if (objectType == null || objectValue == null)
                        {
                            continue;
                        }

                        if (objectType.IsEnum)
                        {
                            deserializedValue = Enum.Parse(objectType, objectValue.Value.GetString());
                        }
                        else
                        {
                            deserializedValue = DeserializeObject(objectType, (JsonElement)objectValue);
                        }

                        break;

                    default:
                        throw new InvalidOperationException(DeserializeErrorMessage(item.Value.ValueKind.ToString()));
                }

                deserialized[item.Name] = deserializedValue;
            }

            return deserialized;
        }

        private static object DeserializeObject(Type objectType, JsonElement objectValue)
        {
            if (typeof(IList).IsAssignableFrom(objectType))
            {
                var deserializedList = (IList)Activator.CreateInstance(objectType);

                foreach (var item in objectValue.EnumerateArray())
                {
                    Type elementType = Type.GetType(item.GetProperty(TYPE_PROPERTY_NAME).GetString());
                    string elementValue = item.GetProperty(VALUE_PROPERTY_NAME).GetString();
                    object deserializedElement = JsonSerializer.Deserialize(elementValue, elementType, GetSerializerOptions());

                    deserializedList.Add(deserializedElement);
                }
                return deserializedList;
            }
            else
            {
                return JsonSerializer.Deserialize(objectValue.GetString(), objectType, GetSerializerOptions());
            }
        }

        public override byte[] Serialize(IDictionary<string, object> values)
        {
            if (values == null || values.Count == 0)
            {
                return Array.Empty<byte>();
            }

            var bufferWriter = new ArrayBufferWriter<byte>();
            try
            {
                using var writer = new Utf8JsonWriter(bufferWriter);
                writer.WriteStartObject();
                foreach (var (key, value) in values)
                {
                    if (value == null)
                    {
                        writer.WriteNull(key);
                        continue;
                    }

                    if (!CanSerializeType(value.GetType()))
                    {
                        throw new InvalidOperationException(SerializeErrorMessage(value.GetType().ToString()));
                    }

                    switch (value)
                    {
                        case Enum _:
                            writer.WriteStartObject(key);
                            writer.WriteString(TYPE_PROPERTY_NAME, value.GetType().FullName);
                            writer.WriteString(VALUE_PROPERTY_NAME, value.ToString());
                            writer.WriteEndObject();
                            break;

                        case string stringValue:
                            writer.WriteString(key, stringValue);
                            break;

                        case int intValue:
                            writer.WriteNumber(key, intValue);
                            break;

                        case long longValue:
                            writer.WriteNumber(key, longValue);
                            break;

                        case decimal decimalValue:
                            writer.WriteNumber(key, decimalValue);
                            break;

                        case bool boolValue:
                            writer.WriteBoolean(key, boolValue);
                            break;

                        case DateTime dateTime:
                            writer.WriteString(key, dateTime);
                            break;

                        case Guid guid:
                            writer.WriteString(key, guid);
                            break;

                        case IJsonSerializable jsonSerializable:
                            writer.WriteStartObject(key);
                            writer.WriteString(TYPE_PROPERTY_NAME, jsonSerializable.GetType().FullName);
                            writer.WriteString(VALUE_PROPERTY_NAME, GetJson(jsonSerializable));
                            writer.WriteEndObject();
                            break;

                        case IList list:
                            writer.WriteStartObject(key);
                            writer.WriteString(TYPE_PROPERTY_NAME, list.GetType().FullName);
                            WriteListToJson(writer, list);
                            writer.WriteEndObject();
                            break;

                        case IEnumerable enumerable:
                            {
                                writer.WriteStartObject(key);
                                writer.WriteString(TYPE_PROPERTY_NAME, enumerable.GetType().FullName);
                                writer.WriteString(VALUE_PROPERTY_NAME, GetJson(enumerable));
                                writer.WriteEndObject();
                                break;
                            }
                    }
                }

                writer.WriteEndObject();
                writer.Flush();

                return bufferWriter.WrittenMemory.ToArray();
            }
            finally
            {
                bufferWriter.Clear();
            }
        }

        private static void WriteListToJson(Utf8JsonWriter writer, IList list)
        {

            writer.WriteStartArray(VALUE_PROPERTY_NAME);
            foreach (var item in list)
            {
                writer.WriteStartObject();
                writer.WriteString(TYPE_PROPERTY_NAME, item.GetType().FullName);
                writer.WriteString(VALUE_PROPERTY_NAME, GetJson(item));
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }

        private static byte[] GetJson(object obj)
        {
            return JsonSerializer.SerializeToUtf8Bytes(obj, GetSerializerOptions());
        }


        private static JsonSerializerOptions GetSerializerOptions()
        {

            if (_SerializerOptions == null)
            {
                _SerializerOptions = new JsonSerializerOptions();
                _SerializerOptions.Converters.Add(new JsonSerializableConverter());
            }

            return _SerializerOptions;
        }

        public override bool CanSerializeType(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            type = Nullable.GetUnderlyingType(type) ?? type;

            return
                type.IsEnum ||
                type == typeof(int) ||
                type == typeof(long) ||
                type == typeof(decimal) ||
                type == typeof(string) ||
                type == typeof(bool) ||
                type == typeof(DateTime) ||
                type == typeof(Guid) ||
                typeof(IJsonSerializable).IsAssignableFrom(type) ||
                typeof(IEnumerable).IsAssignableFrom(type);
        }


        private static string DeserializeErrorMessage(string valueType)
        {
            return $"Can not deserialize value from TempData. Value type is \"{valueType}\".";
        }

        private static string SerializeErrorMessage(string valueType)
        {
            return $"Can not store value in TempData. Can not serialize \"{valueType}\" .";
        }

        private class JsonSerializableConverter : JsonConverter<IJsonSerializable>
        {

            public override bool CanConvert(Type typeToConvert) =>
                typeof(IJsonSerializable).IsAssignableFrom(typeToConvert);

            public override IJsonSerializable Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    throw new JsonException();
                }

                IJsonSerializable obj = (IJsonSerializable)Activator.CreateInstance(typeToConvert);

                int rootDepth = reader.CurrentDepth;

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == rootDepth)
                    {
                        return obj;
                    }

                    if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        string propertyName = reader.GetString();

                        FieldInfo field = typeToConvert.GetField(propertyName, BindingFlags.Instance | BindingFlags.NonPublic);

                        reader.Read();

                        if (reader.TokenType == JsonTokenType.StartObject)
                        {
                            ReadOnlySpan<byte> jsonObject = ReadObject(ref reader, out Type objectType);
                            dynamic value = JsonSerializer.Deserialize(jsonObject, objectType, options);
                            field.SetValue(obj, value);
                        }
                        else if (IsEnum(field))
                        {
                            dynamic value = Enum.Parse(Nullable.GetUnderlyingType(field.FieldType), reader.GetString());
                            field.SetValue(obj, value);
                        }
                        else if (field.FieldType == typeof(string))
                        {
                            field.SetValue(obj, reader.GetString());
                        }
                        else if (IsNumber(field))
                        {
                            dynamic value = Convert.ChangeType(reader.GetInt64(), field.FieldType);
                            field.SetValue(obj, value);
                        }
                        else if (IsDecimal(field))
                        {
                            dynamic value = Convert.ChangeType(reader.GetDecimal(), field.FieldType);
                            field.SetValue(obj, value);
                        }
                        else if (field.FieldType == typeof(bool))
                        {
                            field.SetValue(obj, reader.GetBoolean());
                        }
                        else if (field.FieldType == typeof(DateTime))
                        {
                            field.SetValue(obj, reader.GetDateTime());
                        }
                        else if (field.FieldType == typeof(Guid))
                        {
                            field.SetValue(obj, reader.GetGuid());
                        }

                    }
                }
                throw new JsonException();
            }

            private static byte[] ReadObject(ref Utf8JsonReader reader, out Type typeToConvert)
            {
                typeToConvert = ReadType(ref reader);

                return ReadValue(ref reader).ToArray();
            }

            private static Type ReadType(ref Utf8JsonReader reader)
            {
                reader.Read();
                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException();
                }

                string propertyName = reader.GetString();
                if (propertyName != TYPE_PROPERTY_NAME)
                {
                    throw new JsonException();
                }

                reader.Read();
                if (reader.TokenType != JsonTokenType.String)
                {
                    throw new JsonException();
                }

                string typeDiscriminator = reader.GetString();

                return Type.GetType(typeDiscriminator);
            }

            private static List<byte> ReadValue(ref Utf8JsonReader reader)
            {
                reader.Read();
                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException();
                }

                string propertyName = reader.GetString();
                if (propertyName != VALUE_PROPERTY_NAME)
                {
                    throw new JsonException();
                }

                reader.Read();
                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    throw new JsonException();
                }

                int rootDepth = reader.CurrentDepth;
                var jsonObject = new List<byte>();

                jsonObject.AddRange(reader.ValueSpan.ToArray());

                while (reader.Read())
                {
                    jsonObject.AddRange(reader.ValueSpan.ToArray());

                    if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == rootDepth)
                    {
                        break;
                    }
                }

                return jsonObject;
            }


            public override void Write(Utf8JsonWriter writer, IJsonSerializable value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();

                Type detailedType = value.GetType();

                var fields = detailedType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);

                foreach (var field in fields)
                {
                    dynamic fieldValue = field.GetValue(value);

                    if ((field.FieldType.IsValueType == false) &&
                        (fieldValue == null))
                    {
                        continue;
                    }

                    if (IsEnum(field))
                    {
                        writer.WriteString(field.Name, fieldValue.ToString());
                    }
                    else if (
                        (fieldValue.GetType() == typeof(string)) ||
                        (fieldValue.GetType() == typeof(DateTime)) ||
                        ((fieldValue.GetType() == typeof(Guid)))
                        )
                    {
                        writer.WriteString(field.Name, fieldValue);
                    }
                    else if (
                        (IsNumber(field)) ||
                        (IsDecimal(field))
                        )
                    {
                        writer.WriteNumber(field.Name, fieldValue);
                    }
                    else if (fieldValue.GetType() == typeof(bool))
                    {
                        writer.WriteBoolean(field.Name, fieldValue);
                    }
                    else if (fieldValue.GetType().IsValueType)
                    {
                        continue;
                    }
                    else if (typeof(IJsonSerializable).IsAssignableFrom(fieldValue.GetType()))
                    {
                        writer.WriteString(field.Name, SerializeObject(fieldValue, options));
                    }
                    else if (typeof(IEnumerable).IsAssignableFrom(fieldValue.GetType()))
                    {
                        writer.WriteString(field.Name, SerializeObject(fieldValue, options));
                    }
                }

                writer.WriteEndObject();
            }

            private static ReadOnlySpan<byte> SerializeObject(IJsonSerializable fieldValue, JsonSerializerOptions options)
            {
                return JsonSerializer.SerializeToUtf8Bytes(fieldValue, fieldValue.GetType(), options);
            }

            private bool IsEnum(FieldInfo field)
            {
                Type t = Nullable.GetUnderlyingType(field.FieldType) ?? field.FieldType;
                return t.IsEnum;
            }

            private bool IsNumber(FieldInfo field)
            {
                Type t = field.FieldType;
                return (t == typeof(int)) || (t == typeof(long)) || (t == typeof(short));
            }

            private bool IsDecimal(FieldInfo field)
            {
                Type t = field.FieldType;
                return (t == typeof(decimal)) || (t == typeof(float)) || (t == typeof(double));
            }
        }

    }

    public interface IJsonSerializable { }

    public interface IJsonSerializable<T> : IJsonSerializable where T : new() { }

}