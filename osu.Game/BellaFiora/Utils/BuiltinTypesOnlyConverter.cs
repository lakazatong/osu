#pragma warning disable IDE0073

using Newtonsoft.Json;
using Realms;
using System;
using System.Linq;
using System.Reflection;

public class BuiltinTypesOnlyConverter : JsonConverter
{
    public override bool CanConvert(Type objectType) => true;

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        var properties = value.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead
                && p.GetIndexParameters().Length == 0
                && isBuiltinType(p.PropertyType)
                && !p.GetCustomAttributes(typeof(IgnoredAttribute), inherit: true).Any());

        writer.WriteStartObject();

        foreach (var property in properties)
        {
            try
            {
                writer.WritePropertyName(property.Name);
                object? propertyValue = property.GetValue(value);

                // Write null explicitly if the property value is null.
                if (propertyValue == null)
                {
                    writer.WriteNull();
                }
                else
                {
                    serializer.Serialize(writer, propertyValue);
                }
            }
            catch (Exception ex)
            {
                // Log or capture exceptions here if debugging
                writer.WritePropertyName(property.Name);
                writer.WriteValue($"Serialization error: {ex.Message}");
            }
        }

        writer.WriteEndObject();
    }

    private bool isBuiltinType(Type type) =>
        type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(DateTime) || type == typeof(decimal);
}
