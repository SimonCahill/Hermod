using System;

namespace Hermod.PluginFramework {

	using Config;
	using Core.Commands;

	using Serilog;

    /// <summary>
    /// An abstract class for plugins with all the main features already implemented.
    /// </summary>
    public abstract class Plugin: IPlugin {

		/// <summary>
		/// Specialised constructor; allows inheriting classes to set their values immediately.
		/// </summary>
		/// <param name="pluginName">The name of the plugin.</param>
		/// <param name="pluginVersion">The plugin's version.</param>
		/// <param name="commands">A list of commands (if any).</param>
		public Plugin(string pluginName, Version pluginVersion, params ICommand[] commands) {
			PluginVersion = pluginVersion;
			PluginName = pluginName;
			PluginCommands = commands.ToList();
		}

		/// <inheritdoc/>
		public Version PluginVersion { get; protected set; }

		/// <inheritdoc/>
		public string PluginName { get; protected set; }

		/// <inheritdoc/>
		public List<ICommand> PluginCommands { get; protected set; }

		/// <inheritdoc/>
		public abstract void OnLoad(ILogger logger);

		/// <inheritdoc/>
		public abstract void OnStart();

		/// <inheritdoc/>
		public abstract void OnStop();

		/// <inheritdoc/>
		public abstract void OnConfigChanged(ConfigChangedEventArgs e);

		/// <inheritdoc/>
		public abstract void OnConfigLoaded();
	}
}

