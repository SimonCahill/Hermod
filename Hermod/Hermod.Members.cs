// Contains all the member variables for the Hermod class.

using System;

namespace Hermod {

    using Config;

    using Serilog;

	public partial class Hermod {		

        private ConfigManager m_configManager; /// The <see cref="ConfigManager"/> instance for the application and first-party plugins.
        private ILogger m_appLogger; /// The application's logger instance.

        internal volatile bool m_keepAlive; /// A volatile bool indicating whether or not to keep the application alive.

        private CancellationTokenSource m_inputCancellationToken; /// CancellationTokenSource for reading input from the console in interactive mode

	}
}

