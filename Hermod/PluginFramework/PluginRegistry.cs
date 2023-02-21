using System;

namespace Hermod.PluginFramework {

    /// <summary>
    /// Handles the loading, unloading, and general management of plugins.
    ///
    /// This class knows which plugins are loaded at any given time, can all any
    /// commands provided by the plugin and also fire events.
    /// </summary>
    internal sealed class PluginRegistry {

        #region Singleton
        private PluginRegistry() { }

        private static PluginRegistry? _instance;

        /// <summary>
        /// Gets the current instance of this object.
        /// </summary>
        public static PluginRegistry Instance => _instance ??= new PluginRegistry();
        #endregion


    }
}

