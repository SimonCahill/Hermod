using System;

namespace Hermod.Config {

    using Core;
    using Detail;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    using System.ComponentModel;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
	using System.Xml.Linq;

    public partial class ConfigManager: INotifyPropertyChanged, IConfigChanged {

		#region PropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
		#endregion

		#region ConfigChanged
        public event ConfigChangedEventHandler? ConfigChanged;

        protected void OnConfigChanged(string configName, object? prevValue, object? newValue, Type cfgType) {
            ConfigChanged?.Invoke(this, new ConfigChangedEventArgs(configName, prevValue, newValue, cfgType));
        }
		#endregion

		#region Defaults
		const string DefaultConfigFileName  = ".hermod.json";

        private static FileInfo? _defaultConfigPathCache = null;

        FileInfo GetDefaultConfigPath() {
            return
                _defaultConfigPathCache ??
                (
                    _defaultConfigPathCache = AppInfo.GetBaseHermodDirectory()
                                                     .GetSubFile(
                                                        AppInfo.HermodAppCfgDirName,
                                                        DefaultConfigFileName
                                                      )
                );
        }
		#endregion

		#region Singleton
		private static ConfigManager? _instance;

        /// <summary>
        /// Gets the application-wide instance of the ConfigManager.
        /// </summary>
        /// <remarks >
        /// Plugins will retrieve their own instance of this class.
        /// </remarks>
        public static ConfigManager Instance => _instance ?? (_instance = new ConfigManager());

		protected ConfigManager() {
            m_configDictionary = new JObject();
            m_configFile = GetDefaultConfigPath();
            m_defaultConfig = LoadDefaultConfig();
        }
        #endregion

        [GeneratedRegex(@"^([A-z_][A-z0-9_]+)(\.[A-z_][A-z0-9_]+)+?[^\.]$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
        public static partial Regex ConfigDotNotation();

        protected JObject m_configDictionary;
        protected JObject m_defaultConfig;

        /// <summary>
        /// Loads the default configs from the internal (embedded) resources.
        /// </summary>
        /// <returns>The JObject containing the default configs.</returns>
        protected JObject LoadDefaultConfig() {
            using (var rStream = typeof(ConfigManager).Assembly.GetManifestResourceStream("Hermod.Config.Resources.DefaultConfig.json"))
            using (var sReader = new StreamReader(rStream)) {
                var text = sReader.ReadToEnd();
                return JObject.Parse(text);
            }
        }

		private FileInfo? m_configFile = null;
        /// <summary>
        /// Gets or sets the current ConfigFile.
        /// </summary>
        /// <remarks >
        /// Setting this property will trigger a reload of all configs!
        /// </remarks>
        public FileInfo ConfigFile {
            get => m_configFile ?? GetDefaultConfigPath();
            set {
                if (value == m_configFile) { return; }

                m_configFile = value;
                OnPropertyChanged(nameof(ConfigFile));
                LoadConfig();
            }
        }

        /// <summary>
        /// Asynchronously loads the configurations from disk.
        /// </summary>
        /// <returns>An awaitable <see cref="Task"/>.</returns>
        /// <remarks >
        /// If the config file on disk doesn't exist, contains malformed JSON or is empty, then default configurations will be loaded.
        /// </remarks>
        public async Task LoadConfigAsync() {
            if (!ConfigFile.Exists || ConfigFile.Length < 10) {
                // this will happen for files that don't exist
                // and files that are less than 10B
                m_configDictionary = m_defaultConfig;
                await SaveConfigAsync();
                return;
            }

            using (var cfgFile = ConfigFile.Open(FileMode.Open))
            using (var sReader = new StreamReader(cfgFile)) {
                var sBuilder = new StringBuilder();

                while (sReader.Peek() != -1) {
                    sBuilder.AppendLine(await sReader.ReadLineAsync());
                }

                try {
                    m_configDictionary = JObject.Parse(sBuilder.ToString());
                } catch (Exception ex) {
                    Console.Error.WriteLine("An error occurred while loading configs! Loading defaults!");
                    Console.Error.WriteLine($"Error message: { ex.Message }");

                    #if DEBUG
                    Console.Error.WriteLine(ex.StackTrace);
                    #endif
                }
            }
        }

        /// <summary>
        /// Synchronously loads the configs.
        /// </summary>
        public void LoadConfig() {
            if (!ConfigFile.Exists || ConfigFile.Length < 10) {
                // this will happen for files that don't exist
                // and files that are less than 10B
                m_configDictionary = m_defaultConfig;
                SaveConfig();
                return;
            }

            using (var cfgFile = ConfigFile.Open(FileMode.Open))
            using (var sReader = new StreamReader(cfgFile)) {
                var sBuilder = new StringBuilder();

                while (sReader.Peek() != -1) {
                    sBuilder.AppendLine(sReader.ReadLine());
                }

                try {
                    m_configDictionary = JObject.Parse(sBuilder.ToString());
                } catch (Exception ex) {
                    Console.Error.WriteLine("An error occurred while loading configs! Loading defaults!");
                    Console.Error.WriteLine($"Error message: { ex.Message }");

                    #if DEBUG
                    Console.Error.WriteLine(ex.StackTrace);
                    #endif
                }
            }
        }

        /// <summary>
        /// Asynchronously saves the current configuration to disk.
        /// </summary>
        /// <remarks >
        /// Note: saving configs here WILL remove all comments from the file!
        /// </remarks>
        /// <returns>An awaitable task.</returns>
        public async Task SaveConfigAsync() {
            using (var cfgFile = ConfigFile.Open(FileMode.Truncate))
            using (var sWriter = new StreamWriter(cfgFile)) {
                var serialisedData = JsonConvert.SerializeObject(m_configDictionary);
                await sWriter.WriteLineAsync(serialisedData);
            }
        }

        /// <summary>
        /// Synchronously saves the current configuration to disk.
        /// </summary>
        /// <remarks >
        /// Note: saving configs here WILL remove all comments from the file!
        /// </remarks>
        public void SaveConfig() {
            using (var cfgFile = ConfigFile.Open(FileMode.Truncate))
            using (var sWriter = new StreamWriter(cfgFile)) {
                var serialisedData = JsonConvert.SerializeObject(m_configDictionary);
                sWriter.WriteLine(serialisedData);
            }
        }

        /// <summary>
        /// Retrieves a single configuration.
        /// </summary>
        /// <remarks >
        /// This method supports using dot notation for retrieving configuration from subobjects.
        /// </remarks>
        /// <example >
        /// Using dot notation:
        /// <code >
        /// void Foo() {
        ///     var cfg = ConfigManager.Instance.GetConfig<bool>("Logging.ConsoleLogging.EnableLogging");
        /// }
        /// </code>
        /// </example>
        /// <typeparam name="T"></typeparam>
        /// <param name="configName"></param>
        /// <returns></returns>
        public T GetConfig<T>(string configName) => GetConfig<T>(configName, m_configDictionary);

        /// <summary>
        /// Retrieves a single configuration.
        /// </summary>
        /// <typeparam name="T">The type of the config object</typeparam>
        /// <param name="configName">The name of the config</param>
        /// <param name="dict">The dictionary in which to search.</param>
        /// <returns>The config of type T.</returns>
        /// <exception cref="ArgumentNullException">If an invalid argument was passed.</exception>
        /// <exception cref="ConfigException">If a general config-related error occurred.</exception>
        /// <exception cref="ConfigNotFoundException">If the requested config was not found.</exception>
        protected T GetConfig<T>(string configName, JObject dict) {
            if (string.IsNullOrEmpty(configName) || string.IsNullOrWhiteSpace(configName)) {
                throw new ArgumentNullException(nameof(configName), "The config name must not be null or empty!");
            }

            if (ConfigDotNotation().IsMatch(configName)) {
                var periodPos = configName.IndexOf('.');
                var container = configName.Substring(0, periodPos);

                if (dict.ContainsKey(container)) {
                    var token = dict[container];
                    if (token?.Type != JTokenType.Object) {
                        throw new ConfigException($"Config type mismatch! Expected object; got { token?.Type.ToString() }");
                    }

                    return GetConfig<T>(configName.Substring(periodPos + 1), (JObject)token);
                } else {
                    throw new ConfigNotFoundException(container, $"Could not find container for { configName }!");
                }
            }

            if (!dict.ContainsKey(configName)) {
                throw new ConfigNotFoundException(configName, "Could not find requested config key!");
            }

            var config = dict[configName];

            return config.ToObject<T>();
        }

        /// <summary>
        /// Gets the configurations specific to logger configuration.
        /// </summary>
        /// <returns>An instance of <see cref="LoggerConfig"/> containing the logger configuration for the ConsoleLogger.</returns>
        public LoggerConfig GetConsoleLoggerConfig() => GetConfig<LoggerConfig>("Logging.ConsoleLogging");

        public LoggerConfig GetFileLoggerConfig() => GetConfig<LoggerConfig>("Logging.FileLogging");

    }
}

