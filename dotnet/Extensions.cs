using System.Collections.Generic;
using System.Reflection;
using Geste.Attributes;
using Geste.Configuration;
using Serilog.Core;

namespace BoggedFinanceBot
{
    public static class Extensions
    {
        // logging vrijednosti konfiguracije
        public static void Log(this Config config, Logger logger)
        {
            var properties = config
                .GetType()
                .GetProperties();

            var loggables = new Dictionary<string, object>();
            var padding = 0;

            AddProperties(config, properties, loggables, ref padding);

            logger.Information("");
            foreach (var (key, value) in loggables)
                logger.Information($" {key.PadRight(padding)}  »  {{Value}}", value);
            logger.Information("");
        }

        // reflection stvari za logging konfiguracije
        public static void AddProperties(object parentObject, IEnumerable<PropertyInfo> properties,
            Dictionary<string, object> loggables, ref int padding)
        {
            foreach (var property in properties)
            {
                var attribute = property.GetCustomAttribute<LoggableSettingAttribute>();
                if (attribute == null) continue;

                var name = attribute.SettingName;
                var value = property.GetValue(parentObject);

                if (name.Length > padding)
                    padding = name.Length;


                var shouldAdd = true;

                switch (value)
                {
                    case string valueString when attribute.ValueTrim > 0:
                    {
                        var trimValue = attribute.ValueTrim >= valueString.Length
                            ? 0
                            : attribute.ValueTrim;
                        var stringValue = $"{valueString[..trimValue]}";

                        if (trimValue != 0)
                            stringValue += "...";

                        value = stringValue;
                        break;
                    }
                    case { } when property.GetCustomAttribute<MergeObjectSettingAttribute>() != null:
                    {
                        shouldAdd = false;
                        var childProperties = value
                            .GetType()
                            .GetProperties();

                        AddProperties(value, childProperties, loggables, ref padding);
                        break;
                    }
                }

                if (shouldAdd)
                    loggables.Add(name, value);
            }
        }

        // za svaki slučaj ako trebamo skratiti neki string
        public static string Truncate(this string value, int length)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;

            return value.Length <= length ? value : value[..length];
        }
    }
}