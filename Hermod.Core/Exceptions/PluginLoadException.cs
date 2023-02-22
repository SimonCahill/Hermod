using System;

namespace Hermod.Core.Exceptions {

    /// <summary>
    /// Exception class that is thrown when a plugin failed to load.
    /// </summary>
    public class PluginLoadException: Exception {
        public PluginLoadException(string? pluginName): base("Failed to load plugin!") {
            PluginName = pluginName;
        }

        public string? PluginName { get; }
    }
}

