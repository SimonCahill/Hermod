using System;

namespace Hermod.PluginFramework {

	using Config;
    using Core.Commands;
    using Core.Exceptions;
	using Serilog;

    using System.Reflection;
    using System.Reflection.Metadata;
    using System.Reflection.PortableExecutable;

    /// <summary>
    /// Handles the loading, unloading, and general management of plugins.
    ///
    /// This class knows which plugins are loaded at any given time, can all any
    /// commands provided by the plugin and also fire events.
    /// </summary>
    internal sealed partial class PluginRegistry {

        public ILogger? AppLogger { get; internal set; } = null;

        #region Singleton
        private PluginRegistry() {
            ConfigManager.Instance.ConfigChanged += ConfigManager_ConfigChanged;
            ConfigManager.Instance.ConfigLoaded += ConfigManager_ConfigLoaded;
        }

        private static PluginRegistry? _instance;

        /// <summary>
        /// Gets the current instance of this object.
        /// </summary>
        public static PluginRegistry Instance => _instance ??= new PluginRegistry();
        #endregion

        /// <summary>
        /// Gets a list of all loaded <see cref="IPlugin"/> instances.
        /// </summary>
        internal List<IPlugin> Plugins {
            get {
                List<IPlugin> plugins = new List<IPlugin>();
                foreach (var pluginList in LoadedAssemblies.Select(x => x.Value.Select(y => y.Value))) {
                    plugins.AddRange(pluginList);
                }

                return plugins;
            }
        }

        /// <summary>
        /// A <see cref="Dictionary{Assembly, List{IPlugin}}"/> containing all loaded assemblies and plugins contained within.
        /// </summary>
        internal Dictionary<Assembly, Dictionary<Type, IPlugin>> LoadedAssemblies { get; } = new Dictionary<Assembly, Dictionary<Type, IPlugin>>();

        internal List<PluginDelegator> PluginDelegators { get; } = new List<PluginDelegator>();

        internal List<ICommand>? BuiltInCommands { get; set; } = null;

        /// <summary>
        /// Gets or sets the last <see cref="IPlugin"/> to be registered.
        /// </summary>
        internal IPlugin? LastRegisteredPlugin { get; set; } = null;

        /// <summary>
        /// Loads one or plugins from an <see cref="Assembly"/> on disk.
        /// </summary>
        /// <param name="pluginFile">The file from which to load plugins.</param>
        /// <exception cref="FileNotFoundException">If the file does not exist.</exception>
        /// <exception cref="NotAPluginException">If the given file is not a valid <see cref="Assembly"/> or does not contain instances off <see cref="IPlugin"/> or <see cref="Plugin"/></exception>
        internal void LoadPlugin(FileInfo pluginFile) {
            AppLogger?.Information($"Attempting to load { pluginFile.FullName }...");
            if (!pluginFile.Exists) { throw new FileNotFoundException("The file does not exist.", pluginFile.FullName); }
            if (!IsAssembly(pluginFile)) { throw new NotAPluginException(pluginFile); }

            AppLogger?.Information("Loading assembly...");
            var assembly = Assembly.LoadFile(pluginFile.FullName);

            List<Type> pluginTypes;
            if (!ContainsPlugins(assembly, out pluginTypes)) {
                throw new NotAPluginException(pluginFile);
            }

            AppLogger?.Information($"Found { pluginTypes.Count } plugins in assembly...");
            foreach (var pluginType in pluginTypes) {
                RegisterPlugin(ref assembly, pluginType);
            }
        }

        /// <summary>
        /// Gets a value indicating whether or not a give file is a valid assembly or not.
        /// </summary>
        /// <param name="pluginFile">The file to check for validity.</param>
        /// <returns><code >true</code> if the file contains a valid assembly.</returns>
        internal bool IsAssembly(FileInfo pluginFile) {
            try {
                using (var fStream = pluginFile.OpenRead())
                using (var peReader = new PEReader(fStream)) {
                    if (!peReader.HasMetadata) { return false; }

                    var metaDataReader = peReader.GetMetadataReader();
                    return metaDataReader.IsAssembly;
                }
            } catch (Exception) { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether or not a given assembly contains members inheriting from <see cref="IPlugin"/>.
        /// </summary>
        /// <param name="assembly">The <see cref="Assembly"/> to check in.</param>
        /// <param name="pluginTypes">Out var; the list of <see cref="Type"/>s inheriting from IPlugin contained within the assembly.</param>
        /// <returns></returns>
        internal bool ContainsPlugins(Assembly assembly, out List<Type> pluginTypes) {
            pluginTypes = new List<Type>();

            foreach (var type in assembly.GetTypes()) {
                if (type.IsSubclassOf(typeof(IPlugin)) || type.IsSubclassOf(typeof(Plugin))) {
                    pluginTypes.Add(type);
                }
            }

            return pluginTypes.Count > 0;
        }

        /// <summary>
        /// Internally registers an <see cref="IPlugin"/> class and calls the <see cref="IPlugin.OnLoad(Serilog.ILogger)"/> method once loaded.
        /// </summary>
        /// <param name="assembly">The <see cref="Assembly"/> in which the plugin resides.</param>
        /// <param name="type">The <see cref="Type"/> of the plugin.</param>
        internal void RegisterPlugin(ref Assembly assembly, Type type) {
            if (!LoadedAssemblies.ContainsKey(assembly)) {
                AppLogger?.Debug($"Plugin assembly seems to be new; registering { assembly.GetName().FullName } for the first time!");
                LoadedAssemblies.Add(assembly, new Dictionary<Type, IPlugin>());
            } else if (LoadedAssemblies[assembly].ContainsKey(type)) {
                AppLogger?.Error($"The plugin { type.Name } has already been loaded as a plugin! Silently ignoring...");
                return;
            }

            try {
                var plugin = Activator.CreateInstance(type) as IPlugin;
                if (plugin is null) {
                    throw new PluginLoadException(type.Name);
                }

                LoadedAssemblies[assembly].Add(type, plugin);
                var pluginDelegator = new PluginDelegator(plugin);
                PluginDelegators.Add(pluginDelegator);

                plugin = LoadedAssemblies[assembly][type];
                plugin.OnLoad(pluginDelegator);

                LastRegisteredPlugin = plugin;
                AppLogger?.Information($"Loaded plugin { plugin.PluginName } { plugin.PluginVersion.ToString() }");
            } catch (Exception ex) {
                AppLogger?.Error("Failed to load plugin from assembly!");
                AppLogger?.Error($"Error: { ex.Message }");
                throw;
            }
        }

        private void ConfigManager_ConfigLoaded(object? sender, ConfigLoadedEventArgs e) {
            foreach (var plugin in Plugins) {
                plugin.OnConfigLoaded();
            }
        }

        private void ConfigManager_ConfigChanged(object? sender, ConfigChangedEventArgs e) {
            foreach (var plugin in Plugins) {
                plugin.OnConfigChanged(e);
            }
        }

    }
}

