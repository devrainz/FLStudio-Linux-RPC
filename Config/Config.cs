using Newtonsoft.Json;
using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using System.Drawing;
using Console = Colorful.Console;
using System.Runtime.InteropServices;

public static class ConfigValues
{
    [DefaultValue("1192880494086455357")]
    public static string ClientID { get; set; }

    [DefaultValue(false)]
    public static bool SecretMode { get; set; }

    [DefaultValue(true)]
    public static bool ShowTimestamp { get; set; }

    [DefaultValue(true)]
    public static bool DisplayConfigInfo { get; set; }

    [DefaultValue(false)]
    public static bool AccurateVersion { get; set; }

    [DefaultValue(4000)]
    public static int UpdateInterval { get; set; }
}

public static class ConfigSettings
{
    private static object ConvertValue(object value, Type targetType)
    {
        try
        {
            if (targetType == typeof(bool))
            {
                return Convert.ToBoolean(value);
            }
            else if (targetType == typeof(int))
            {
                return Convert.ToInt32(value);
            }
            else if (targetType == typeof(long))
            {
                return Convert.ToInt64(value);
            }
            else if (targetType == typeof(float))
            {
                return Convert.ToSingle(value);
            }
            else if (targetType == typeof(double))
            {
                return Convert.ToDouble(value);
            }
            else if (targetType == typeof(decimal))
            {
                return Convert.ToDecimal(value);
            }
            else
            {
                return value;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Couldn't convert configuration values to the appropriate types: {ex.Message}", Color.Red);
            Logger.Error("ConvertValue failed", ex);
            return null;
        }
    }

    private static void SetValues(Dictionary<string, object> properties)
    {
        try
        {
            var configProperties = typeof(ConfigValues)
                .GetProperties()
                .Where(prop => Attribute.IsDefined(prop, typeof(DefaultValueAttribute)));

            foreach (var prop in configProperties)
            {
                if (properties.TryGetValue(prop.Name, out var value))
                {
                    object convertedValue = ConvertValue(value, prop.PropertyType);
                    prop.SetValue(null, convertedValue);
                }
                else
                {
                    Console.WriteLine($"Property {prop.Name} not found in the loaded configuration.", Color.Red);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting configuration values: {ex.Message}", Color.Red);
            Logger.Error("SetValues failed", ex);
        }
    }


    public static void SaveCurrentConfig(string filePath)
    {
        try
        {
            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            var properties = typeof(ConfigValues)
                .GetProperties()
                .Where(prop => Attribute.IsDefined(prop, typeof(DefaultValueAttribute)))
                .ToDictionary(
                    prop => prop.Name,
                    prop => prop.GetValue(null)
                );

            string json = JsonConvert.SerializeObject(properties, Formatting.Indented);

            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving current configuration: {ex.Message}", Color.Red);
            Logger.Error("SaveCurrentConfig failed", ex);
        }
    }

    public static void SaveConfig(string filePath)
    {
        try
        {
            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            var properties = typeof(ConfigValues)
                .GetProperties()
                .Where(prop => Attribute.IsDefined(prop, typeof(DefaultValueAttribute)))
                .ToDictionary(
                    prop => prop.Name,
                    prop =>
                    {
                        var defaultValueAttribute = (DefaultValueAttribute)Attribute.GetCustomAttribute(prop, typeof(DefaultValueAttribute));

                    return ConvertValue(defaultValueAttribute.Value, prop.PropertyType);
                    });

            string json = JsonConvert.SerializeObject(properties, Formatting.Indented);

            if (!File.Exists(filePath))
            {
                Console.WriteLine("Configuration file not found. Creating one with default values.\n", Color.LightSkyBlue);
                File.WriteAllText(filePath, json);
            }
            else
            {
                Console.WriteLine("Configuration file present, loading values...\n", Color.LimeGreen);
            }
            LoadConfig(filePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving configuration: {ex.Message}", Color.Red);
            Logger.Error("SaveConfig failed", ex);
        }
    }

    public static void LoadConfig(string filePath)
    {
        try
        {
            string json = File.ReadAllText(filePath);

            var properties = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

            SetValues(properties);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading configuration: {ex.Message}", Color.Red);
            Logger.Error("LoadConfig failed", ex);
        }
    }
}
