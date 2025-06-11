using Sufficit.Asterisk.Manager.Events;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sufficit.Asterisk.Manager
{
    public static class JsonExtensions
    {
        public static string ToJson(this ManagerEventGeneric source)
        {
            var options = new JsonSerializerOptions();
            options.WriteIndented = false;
            options.Converters.Add(new IManagerEventConverter());
            return JsonSerializer.Serialize(source, source.GetType(), options);
        }

        public class IManagerEventConverter : JsonConverter<IManagerEvent>
        {
            public override IManagerEvent? Read(
                ref Utf8JsonReader reader,
                Type typeToConvert,
                JsonSerializerOptions options) =>
                    JsonSerializer.Deserialize<IManagerEvent>(reader.GetString()!, options);

            public override void Write(
                Utf8JsonWriter writer,
                IManagerEvent element,
                JsonSerializerOptions options)
            {
                var json = JsonSerializer.Serialize(element, element.GetType(), options);
                writer.WriteRawValue(json);
            }
        }
    }
}
