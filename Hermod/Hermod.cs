using System;

namespace Hermod {

    using Config;
    using Core;

    using Serilog;

    /// <summary>
    /// The main application class.
    ///
    /// This class handles all the main logic within the application, such as timing operations, executing commands, handling user input, etc.
    /// </summary>
    public class Hermod {

        private ConfigManager m_configManager;
        private ILogger m_appLogger;

        /// <summary>
        /// Main constructor; initialises the object.
        /// </summary>
        /// <param name="configManager">The application-wide config instance.</param>
        /// <param name="logger">The application logger.</param>
        public Hermod(ConfigManager configManager, ILogger logger) {
            m_configManager = configManager;
            m_appLogger = logger;
        }
    }
}

