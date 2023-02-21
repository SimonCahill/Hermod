using System;

namespace Hermod {

    using Config;
    using Core;

    using Serilog;

    using System.Text;

    /// <summary>
    /// The main application class.
    ///
    /// This class handles all the main logic within the application, such as timing operations, executing commands, handling user input, etc.
    /// </summary>
    public partial class Hermod {

        public bool InteractiveMode { get; internal set; }

        /// <summary>
        /// Main constructor; initialises the object.
        /// </summary>
        /// <param name="configManager">The application-wide config instance.</param>
        /// <param name="logger">The application logger.</param>
        public Hermod(ConfigManager configManager, ILogger logger) {
            m_configManager = configManager;
            m_appLogger = logger;
            m_keepAlive = true;
            InteractiveMode = configManager.GetConfig<bool>("Terminal.EnableInteractive");
            m_inputCancellationToken = new CancellationTokenSource();
        }

        internal void StartUp() {
            SetTerminalTitle();
            m_appLogger.Information("Setting up OS event handlers...");
			Console.CancelKeyPress += Console_CancelKeyPress;

            m_appLogger.Information("Loading plugins...");
        }

        /// <summary>
        /// Executes the main business logic of the application.
        /// </summary>
        /// <returns></returns>
		internal async Task<int> Execute() {

            while (m_keepAlive) {

                if (InteractiveMode) {
                    var promptInput = await ShowPrompt();
                    if (string.IsNullOrEmpty(promptInput)) { continue; }


                } else {
                    Thread.Sleep(50);
                }

            }

            ShutDown();

            return 0; // for the moment; this will also be the exit code for the application.
        }

        /// <summary>
        /// Displays the input prompt.
        /// </summary>
        /// <returns>An awaitable string?.</returns>
        private async Task<string?> ShowPrompt() {
            Console.WriteLine();
            Console.Write("hermod > ");
            return await Console.In.ReadLineAsync(m_inputCancellationToken.Token);
        }

        /// <summary>
        /// Shuts Hermod down.
        /// </summary>
        internal void ShutDown() {
            m_appLogger.Warning("Shutting down plugins...");

            m_appLogger.Warning("Preparing for graceful exit.");
            m_keepAlive = false;
        }

        /// <summary>
        /// Sets the terminal's title.
        /// </summary>
        internal void SetTerminalTitle() {
            var version = GetType().Assembly.GetName().Version;
            var appTitle = new StringBuilder().Append("Hermod ");

            if (InteractiveMode) { appTitle.Append("[interactive] "); }

            Console.Title = $"{ appTitle.ToString() } - v{ version?.Major }.{ version?.MajorRevision }.{ version?.Minor }.{ version?.MinorRevision }";
        }
        
        /// <summary>
        /// Handles SIGINT (CTRL+C)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private async void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e) {
            m_appLogger.Warning("Received signal SIGNIT (CTRL+C)!");
			e.Cancel = true;

            m_inputCancellationToken.Cancel();
            m_keepAlive = false;
		}
    }
}

