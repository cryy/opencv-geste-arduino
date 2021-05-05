using System;

namespace Geste.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class LoggableSettingAttribute : Attribute
    {
        public LoggableSettingAttribute(string settingName, int valueTrim = 0)
        {
            SettingName = settingName;
            ValueTrim = valueTrim;
        }

        public string SettingName { get; }
        public int ValueTrim { get; }
    }
}