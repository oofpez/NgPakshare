using Microsoft.Azure;
using Microsoft.WindowsAzure.ServiceRuntime;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;

namespace CachedConfigurationManager
{
    public class CachedConfigurationManager
    {
        private ConcurrentDictionary<string, string> Settings { get; set; }

        private static CachedConfigurationManager instance;

        public static CachedConfigurationManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new CachedConfigurationManager();
                }

                return instance;
            }
        }

        private CachedConfigurationManager()
        {
            Settings = new ConcurrentDictionary<string, string>();
            RoleEnvironment.Changed += RoleEnvironmentChanged;
        }

        /// <summary>
        /// Retrieve a configuration item
        /// </summary>
        /// <typeparam name="T">Convert the configuration value</typeparam>
        /// <param name="key">The configuration key of the item</param>
        /// <returns>The configuration value as T</returns>
        public T GetSetting<T>(string key)
        {
            if (String.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(String.Format("Configuration value with key '{0}' cannot be null or empty", key));
            }

            string value = GetSetting(key);

            if (value == null)
            {
                throw new ArgumentOutOfRangeException(String.Format("Configuration value with key '{0}' does not exist", key));
            }

            try
            {
                return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
            }
            catch (InvalidCastException ex)
            {
                throw new InvalidOperationException(String.Format("Configuration value with key '{0}' could not be parsed: Conversion not supported.", key), ex);
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException(String.Format("Configuration value with key '{0}' could not be parsed: Format error.", key), ex);
            }
        }

        /// <summary>
        /// Determines whether an item exists in the configurations
        /// </summary>
        /// <param name="key">The configuration key of the item</param>
        /// <returns>True if the item exists in the cloud or web configs</returns>
        public bool HasSetting(string key)
        {
            return GetSetting(key) != null;
        }

        protected string GetSetting(string key)
        {
            string value;

            if (Settings.TryGetValue(key, out value))
            {
                return value;
            }
            else
            {
                Debug.WriteLine("CachedConfigurationSettings:  Restoring " + key + " from config...");
                return RestoreSetting(key);
            }
        }

        private string RestoreSetting(string key)
        {
            string value = CloudConfigurationManager.GetSetting(key);

            if (value == null)
            {
                return null;
            }
            else
            {
                Settings.AddOrUpdate(key, value, (k, v) => v);
                return value;
            }
        }

        private void RoleEnvironmentChanged(object sender, RoleEnvironmentChangedEventArgs e)
        {
            Settings.Clear();
            Debug.WriteLine("CachedConfigurationManager: Cleared due to RoleEnvironmentChanged event");
        }
    }
}
