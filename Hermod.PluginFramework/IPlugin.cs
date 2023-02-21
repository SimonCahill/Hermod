using System;

namespace Hermod.PluginFramework {

    using Core.Commands;
    using Core.Commands.Results;

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
        ICommand[] PluginCommands { get; }

        /// <summary>
        /// Method that is called once the plugin has been loaded.
        /// This may be used for pre-init purposes.
        /// </summary>
        /// <param name="logger" >The logger provided by Hermod for this plugin.</param>
        void OnLoad(ILogger logger);

        /// <summary>
        /// Method that is called once Hermod has completed its startup procedures and is ready to run.
        /// </summary>
        void OnStart();

        /// <summary>
        /// Method that is called when Hermod is shutting down.
        /// </summary>
        void OnStop();

    }

}

