using UnityEngine;
using Newtonsoft.Json;
using System;

// Custom converter for UnityEngine.Vector3
public class Vector3Converter : JsonConverter<Vector3>
{
    public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("x");
        writer.WriteValue(value.x);
        writer.WritePropertyName("y");
        writer.WriteValue(value.y);
        writer.WritePropertyName("z");
        writer.WriteValue(value.z);
        writer.WriteEndObject();
    }

    public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        // A simplified reader. A more robust one would check for token types.
        float x = 0f, y = 0f, z = 0f;

        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.EndObject) break;

            if (reader.TokenType == JsonToken.PropertyName)
            {
                string propertyName = reader.Value.ToString();
                reader.Read(); // Move to the value

                switch (propertyName)
                {
                    case "x":
                        x = Convert.ToSingle(reader.Value);
                        break;
                    case "y":
                        y = Convert.ToSingle(reader.Value);
                        break;
                    case "z":
                        z = Convert.ToSingle(reader.Value);
                        break;
                }
            }
        }
        return new Vector3(x, y, z);
    }
}

// Custom converter for UnityEngine.Quaternion
public class QuaternionConverter : JsonConverter<Quaternion>
{
    public override void WriteJson(JsonWriter writer, Quaternion value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("x");
        writer.WriteValue(value.x);
        writer.WritePropertyName("y");
        writer.WriteValue(value.y);
        writer.WritePropertyName("z");
        writer.WriteValue(value.z);
        writer.WritePropertyName("w");
        writer.WriteValue(value.w);
        writer.WriteEndObject();
    }

    public override Quaternion ReadJson(JsonReader reader, Type objectType, Quaternion existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        float x = 0f, y = 0f, z = 0f, w = 1f;

        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.EndObject) break;

            if (reader.TokenType == JsonToken.PropertyName)
            {
                string propertyName = reader.Value.ToString();
                reader.Read();

                switch (propertyName)
                {
                    case "x":
                        x = Convert.ToSingle(reader.Value);
                        break;
                    case "y":
                        y = Convert.ToSingle(reader.Value);
                        break;
                    case "z":
                        z = Convert.ToSingle(reader.Value);
                        break;
                    case "w":
                        w = Convert.ToSingle(reader.Value);
                        break;
                }
            }
        }
        return new Quaternion(x, y, z, w);
    }
}