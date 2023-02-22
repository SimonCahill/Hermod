using System;

namespace Hermod {

    using Config;
    using Core;
    using Core.Commands.Results;
    using PluginFramework;

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
            m_consoleLock = new object();
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
            try {
                Console.BufferHeight = 100;
                Console.BufferWidth = 120;
            } catch (PlatformNotSupportedException ex) {
                m_appLogger.Error("Failed to set terminal size!");
                m_appLogger.Error($"Error: { ex.Message }");
            }

            m_appLogger.Debug("Setting up PluginRegistry...");
            PluginRegistry.Instance.AppLogger = m_appLogger;

            m_appLogger.Information("Loading plugins...");
            m_appLogger.Information($"Plugin dir: { m_configManager.GetPluginInstallDir() }");
            foreach (var plugin in m_configManager.GetPluginInstallDir().EnumerateFiles("*.dll")) {
                m_appLogger.Information($"Attempting to load { plugin.FullName }...");
                try {
                    PluginRegistry.Instance.LoadPlugin(plugin);
                } catch (Exception ex) {
                    m_appLogger.Error($"Failed to load assembly { plugin.FullName }!");
                    m_appLogger.Error($"Error: { ex.Message }");

                    m_appLogger.Debug(ex.StackTrace);
                }
            }
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

                    var splitString = promptInput.Split(' ', '\t');
                    if (TryGetCommand(splitString.First(), out var command)) {
                        if (command is null) { continue; }

                        var argArray = new string[splitString.Length - 1];
                        if (argArray.Length > 0) {
                            Array.Copy(splitString[1..], argArray, argArray.Length);
                        }

                        var result = await command.ExecuteAsync(argArray);

                        if (result is CommandErrorResult && !string.IsNullOrEmpty(result?.Message)) {
                            ConsoleErrorWrite(result.Message);
                        } else if (!string.IsNullOrEmpty(result?.Message)) {
                            ConsoleWrite(result.Message);
                        }
                    } else {
                        m_appLogger.Error($"Command \"{splitString[0] }\" not found!");
                    }
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
		private void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e) {
            Console.WriteLine(); // make sure the output isn't on the same line as the prompt or output
            m_appLogger.Warning("Received signal SIGNIT (CTRL+C)!");
			e.Cancel = true;

            if (InteractiveMode) {
                m_appLogger.Warning("Hermod is running in interactive mode! Please use \"quit\" command!");
                ShowPrompt();
                return;
            }

            m_inputCancellationToken.Cancel();
            m_keepAlive = false;
		}

        private void ConsoleWrite(string message) {
            lock (m_consoleLock) {
                Console.WriteLine(message);
            }
        }

        private void ConsoleErrorWrite(string message) {
            lock (m_consoleLock) {
                var prevBackground = Console.BackgroundColor;
                var prevForegound = Console.ForegroundColor;
                Console.Error.WriteLine(message);
                Console.BackgroundColor = prevBackground;
                Console.ForegroundColor = prevForegound;
            }
        }
    }
}

