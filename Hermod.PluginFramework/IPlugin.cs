using System;

namespace Hermod.PluginFramework {

    using Config;
    using Core.Commands;
    using Core.Commands.Results;
    using Hermod.Core.Delegation;
    using Serilog;

    /// <summary>
    /// Basic contract between Hermod and any plugins.
    /// </summary>
    public interface IPlugin {

        /// <summary>
        /// Gets the version of the plugin.
        /// </summary>
        Version PluginVersion { get; }

        /// <summary>
        /// Gets the name of the plugin.
        /// </summary>
        string PluginName { get; }

        /// <summary>
        /// A list of all commands this plugin provides
        /// </summary>
        List<ICommand> PluginCommands { get; }

        /// <summary>
        /// Method that is called once the plugin has been loaded.
        /// This may be used for pre-init purposes.
        /// </summary>
        /// <param name="pluginDelegator" >The delegator allocated to this plugin.</param>
        void OnLoad(IPluginDelegator pluginDelegator);

        /// <summary>
        /// Method that is called once Hermod has completed its startup procedures and is ready to run.
        /// </summary>
        void OnStart();

        /// <summary>
        /// Method that is called when Hermod is shutting down.
        /// </summary>
        void OnStop();

        /// <summary>
        /// Method that is called when an application-wide configuration has been modified.
        /// </summary>
        /// <param name="e">The <see cref="ConfigChangedEventArgs"/> that are generated when a config was modified.</param>
        void OnConfigChanged(ConfigChangedEventArgs e);

        /// <summary>
        /// Method that is called when the application-wide configurations have been loaded.
        /// </summary>
        void OnConfigLoaded();

    }

}

