using System.Reflection;
using System.ComponentModel;

using OWML.Common;

namespace OWMixedInput;

// Our Settings struct is auto-generated from default-config.json using Generate_Settings_cs.py.
// The struct contains an enum field for each setting in default-config.json.
// The fields and the enum variants are annotated with a ComponentModel Description
// which describes how to read the setting from an IModConfig.

public static class SettingsUtils {
    // Get the Description of a member
    static string Description(this MemberInfo member) 
        => member.GetCustomAttribute<DescriptionAttribute>().Description;

    // Get the Description of a value `value` of a enum `type`.
    static string Description(Type type, object value)
        => type.GetMember(Enum.GetName(type, value)).Single().Description();

    // Get the value of an enum `type` whose Description is `description`.
    static object Value(Type type, string description)
        => Enum.GetValues(type).Cast<object>().Single(value => description.Equals(Description(type, value)));

    // Get all of our mod settings from an IModConfig
    public static Settings GetSettings(this IModConfig config) {
        // Box the setting so that we can use field.SetValue.
        var settings = (object)default(Settings);

        foreach (FieldInfo field in typeof(Settings).GetFields()) {
            var fieldType = field.FieldType;

            var key = field.Description();

            if (fieldType.IsEnum) {
                var setting = config.GetSettingsValue<string>(key);

                var value = Value(fieldType, setting);

                field.SetValue(settings, value);
            } else if (fieldType == typeof(bool)) {
                var setting = config.GetSettingsValue<bool>(key);

                field.SetValue(settings, setting);
            } else if (fieldType == typeof(float)) {
                var setting = config.GetSettingsValue<float>(key);

                field.SetValue(settings, setting);
            } else {
                throw new Exception($"Unsupported settings type: {fieldType}");
            }
        }

        return (Settings)settings;
    }
}
