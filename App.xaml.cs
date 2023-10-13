using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace NetworkProgrammingP12
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static string configFilename = "email-settings.json";
        private static JsonElement? settings = null;
        private static JsonElement? jsonTextCheck = null;
        public static string GetConfiguration(string name) //System.Text.Json.JsonException
        {
            try
            {
                jsonTextCheck ??= JsonSerializer.Deserialize<dynamic>(
                    System.IO.File.ReadAllText("email-settings.json"));
            }
            catch (System.Text.Json.JsonException) 
            { 
                return "Error"; 
            }

            if (settings == null)
            {
                if (!System.IO.File.Exists(configFilename))
                {
                    MessageBox.Show($"Config file {configFilename} not found","Operation cant be finished", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }
                settings ??= JsonSerializer.Deserialize<dynamic>(
                     System.IO.File.ReadAllText("email-settings.json"));
            }


            JsonElement? jsonElement = settings;
            try
            {
                foreach (String key in name.Split(':'))
                {
                    jsonElement = jsonElement?.GetProperty(key);
                }
            }
            catch
            {

                return null!;
            }

            return jsonElement?.GetString()!;
        }
    }
}
