using Newtonsoft.Json;
using System;

namespace IoT.Simulator.Tools
{
    public class StringToConsoleColorConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var enumString = (string)reader.Value;

            return Enum.Parse(typeof(System.ConsoleColor), enumString, true);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            System.ConsoleColor color = (System.ConsoleColor)value;

            writer.WriteValue(value.ToString());
        }
    }
}
