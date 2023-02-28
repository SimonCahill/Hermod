using System;

namespace Hermod.Config {

    using Core;
    using Detail;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    using Serilog;

    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Provides an application-wide, thread safe way of getting and setting configurations relevant to the main portions of the application.
    ///
    /// Thread-safety is guaranteed on a per-instance basis.
    ///
    /// Getters and setters provided by this class support use of dot-notation for configurations.
    /// </summary>
    /// <example >
    /// Getting and setting values with and without dot-notation:
    ///
    /// <code >
    /// void GetConfigWithDotNotation() {
    ///     var cfg = ConfigManager.Instance.GetConfig<bool>("Object.Object.Object.Value");
    /// }
    ///
    /// void GetConfigWithoutDotNotation() {
    ///     var cfg = ConfigManager.Instance.GetConfig<bool>("Value");
    /// }
    ///
    /// void SetConfigWithDotNotation() {
    ///     ConfigManager.Instance.SetConfig<int>("Object.Object.Value", 123 ^ 456);
    /// }
    ///
    /// void SetConfigWithoutDotNotation() {
    ///     ConfigManager.Instance.SetConfig<object>("Value", new { ComplexType = new { WithMoreValues = true } });
    /// }
    /// </code>
    /// </example>
    public partial class ConfigManager: IConfigLoaded, IConfigChanged {

        private readonly object m_lock;
        private volatile string m_lockedBy;

        /// <summary>
        /// Gets or sets the logger instance.
        /// </summary>
        /// <value>The new <see cref="ILogger" /> instance.</value>
        public ILogger? AppLogger { get; set; }

        #region IConfigLoaded
        public event ConfigLoadedEventHandler? ConfigLoaded;

        protected void OnConfigLoaded() => ConfigLoaded?.Invoke(this, new ConfigLoadedEventArgs());
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
            m_lock = new object();
            m_lockedBy = string.Empty;
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
            using var rStream = typeof(ConfigManager).Assembly.GetManifestResourceStream("Hermod.Config.Resources.DefaultConfig.json");
            if (rStream is null) {
                throw new Exception("Failed to load default configuration! Hermod will abort!");
            }

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
                lock (m_lock) {
                    m_lockedBy = nameof(LoadConfigAsync);
                    m_configDictionary = m_defaultConfig;
                    m_lockedBy = string.Empty;
                }
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
                    lock (m_lock) {
                        m_lockedBy = nameof(LoadConfigAsync);
                        m_configDictionary = JObject.Parse(sBuilder.ToString());
                        m_lockedBy = string.Empty;
                    }
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
                lock (m_lock) {
                    m_lockedBy = nameof(LoadConfig);
                    m_configDictionary = m_defaultConfig;
                    m_lockedBy = string.Empty;
                }
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
                    lock (m_lock) {
                        m_lockedBy = nameof(LoadConfig);
                        m_configDictionary = JObject.Parse(sBuilder.ToString());
                        m_lockedBy = string.Empty;
                    }
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
            if (!ConfigFile.Exists) {
                try {
                    ConfigFile.Directory?.Create();
                } catch (UnauthorizedAccessException ex) {
                    HandleUnauthorizedAccessWhenSavingConfig(ex);
                    await SaveConfigAsync();
                }
                ConfigFile.Create().Close();
            }
            
            using (var cfgFile = ConfigFile.Open(FileMode.Truncate))
            using (var sWriter = new StreamWriter(cfgFile)) {
                string serialisedData;
                lock (m_lock) {
                    m_lockedBy = nameof(SaveConfigAsync);
                    serialisedData = JsonConvert.SerializeObject(m_configDictionary, Formatting.Indented);
                    m_lockedBy = string.Empty;
                }
                await sWriter.WriteLineAsync(serialisedData);
            }
        }

        private void HandleUnauthorizedAccessWhenSavingConfig(UnauthorizedAccessException ex) {
            AppLogger?.Error($"Failed to create config file in { ConfigFile.FullName }!");
            AppLogger?.Error(ex, "Hermod does not have sufficient access rights.");
            AppLogger?.Warning("Switching to user-local config location!");
            ConfigFile = AppInfo.GetLocalHermodDirectory().CreateSubdirectory(AppInfo.HermodAppCfgDirName).GetSubFile(DefaultConfigFileName);
        }

        /// <summary>
        /// Synchronously saves the current configuration to disk.
        /// </summary>
        /// <remarks >
        /// Note: saving configs here WILL remove all comments from the file!
        /// </remarks>
        public void SaveConfig() {
            if (!ConfigFile.Exists) {
                try {
                    ConfigFile.Directory?.Create();
                } catch (UnauthorizedAccessException ex) {
                    HandleUnauthorizedAccessWhenSavingConfig(ex);
                    SaveConfig();
                }
                ConfigFile.Create().Close();
            }

            using (var cfgFile = ConfigFile.Open(FileMode.Truncate))
            using (var sWriter = new StreamWriter(cfgFile)) {
                string serialisedData;
                lock (m_lock) {
                    m_lockedBy = nameof(SaveConfig);
                    serialisedData = JsonConvert.SerializeObject(m_configDictionary, Formatting.Indented);
                    m_lockedBy = string.Empty;
                }
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
        public T GetConfig<T>(string configName) {
            try {
                return GetConfig<T>(configName, m_configDictionary);
            } catch (ConfigNotFoundException) {
                return GetConfig<T>(configName, m_defaultConfig); // no point in catching anything here
            } catch {
                throw;
            }
        }

        /// <summary>
        /// Sets a given configuration to a value determined by <paramref name="configValue"/>.
        /// </summary>
        /// <typeparam name="T">The config type.</typeparam>
        /// <param name="configName">The name of the configuration to modify or add. Supports dot notation.</param>
        /// <param name="configValue">The new value for the config.</param>
        public void SetConfig<T>(string configName, T? configValue) => SetConfig<T>(configName, configValue, ref m_configDictionary);

        /// <summary>
        /// Adds or sets a given configuration to a value determined by <paramref name="configValue"/>.
        /// </summary>
        /// <typeparam name="T">Generic type parameter.</typeparam>
        /// <param name="configName">The name of the config to add or modify; supports dot notation.</param>
        /// <param name="configValue">The (new) value for the configuration.</param>
        /// <param name="dict">A reference to the object to modify.</param>
        /// <exception cref="ArgumentNullException">If a passed argument is null or empty.</exception>
        /// <exception cref="ConfigException">If an unexpected error occurs.</exception>
        protected void SetConfig<T>(string configName, T? configValue, ref JObject dict) {
            if (string.IsNullOrEmpty(configName) || string.IsNullOrWhiteSpace(configName)) {
                throw new ArgumentNullException(nameof(configName), "The config name must not be null or empty!");
            }

            if (ConfigDotNotation().IsMatch(configName)) {
                var periodPos = configName.IndexOf('.');
                var container = configName.Substring(0, periodPos);
                var subConfig = configName.Substring(periodPos + 1);
                var subConfigHasDotNotation = ConfigDotNotation().IsMatch(subConfig);

                if (dict.ContainsKey(container) && subConfigHasDotNotation) {
                    var token = dict[container];
                    if (token?.Type != JTokenType.Object) {
                        throw new ConfigException($"Config type mismatch! Expected object; got { token?.Type.ToString() }");
                    }

                    var tokenObj = (JObject)token;
                    SetConfig<T>(subConfig, configValue, ref tokenObj);
                    return;
                } else if (subConfigHasDotNotation) {
                    // the desired object doesn't exist, so we'll have to add it
                    lock (m_lock) {
                        m_lockedBy = nameof(SetConfig);
                        dict.Add(container, new JObject()); // add a new object and then recursively call this method so the first condition is true
                        m_lockedBy = string.Empty;
                    }
                    SetConfig<T>(configName, configValue, ref dict);
                    return;
                }

                // the object exists, but subConfig does not match config dot notation; call the method again so the value can be set
                SetConfig<T>(subConfig, configValue, ref dict);
            }

            if (!dict.ContainsKey(configName)) {
                lock (m_lock) {
                    m_lockedBy = nameof(SetConfig);
                    dict.Add(configName, JToken.FromObject(configValue));
                    m_lockedBy = string.Empty;
                }
                ConfigChanged?.Invoke(this, new ConfigChangedEventArgs(configName, null, configValue, typeof(T)));
                return;
            }

            var prevValue = dict[configName];
            lock (m_lock) {
                m_lockedBy = nameof(SetConfig);
                dict[configName] = JToken.FromObject(configValue);
                m_lockedBy = string.Empty;
            }
            ConfigChanged?.Invoke(this, new ConfigChangedEventArgs(configName, prevValue, configValue, typeof(T)));
        }

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

            JToken config;
            lock (m_lock) {
                m_lockedBy = nameof(GetConfig);
                config = dict[configName];
                m_lockedBy = string.Empty;
            }

            return config.ToObject<T>();
        }

		#region Special Accessors
		/// <summary>
		/// Gets the configurations specific to logger configuration.
		/// </summary>
		/// <returns>An instance of <see cref="LoggerConfig"/> containing the logger configuration for the ConsoleLogger.</returns>
		public LoggerConfig GetConsoleLoggerConfig() => GetConfig<LoggerConfig>("Logging.ConsoleLogging");

        /// <summary>
        /// Gets the configurations specific to logger configuration.
        /// </summary>
        /// <returns>An instance of <see cref="LoggerConfig"/> containing the logger configuration for the FileLogger.</returns>
        public LoggerConfig GetFileLoggerConfig() => GetConfig<LoggerConfig>("Logging.FileLogging");

        /// <summary>
        /// Special accessor for config which may contain different values handleable by Hermod.
        /// </summary>
        /// <returns>The install directory for plugins.</returns>
        public DirectoryInfo GetPluginInstallDir() {
            var installDir = GetConfig<string?>("Plugins.InstallDir");

            if (string.IsNullOrEmpty(installDir)) {
                installDir = AppInfo.GetLocalHermodDirectory().CreateSubdirectory(AppInfo.HermodAppPluginDirName).FullName;
                SetConfig<string>("Plugins.InstallDir", installDir);
            }

            return new DirectoryInfo(installDir);
        }

		#endregion

	}
}

