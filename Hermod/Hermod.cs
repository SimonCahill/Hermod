using System;

namespace Hermod {

    using Config;
    using Core;
    using Core.Commands.Results;
    using PluginFramework;

    using Serilog;
    using System.Diagnostics;
    using System.Text;

    /// <summary>
    /// The main application class.
    ///
    /// This class handles all the main logic within the application, such as timing operations, executing commands, handling user input, etc.
    /// </summary>
    public partial class Hermod {

        public bool InteractiveMode { get; internal set; }

        private Stack<string> m_previousCommands = new Stack<string>();

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

            m_appLogger.Debug("Setting up PluginRegistry...");
            PluginRegistry.Instance.AppLogger = m_appLogger;
            PluginRegistry.Instance.BuiltInCommands = Commands;

            m_appLogger.Information("Loading plugins...");
            m_appLogger.Information($"Plugin dir: { m_configManager.GetPluginInstallDir() }");
            foreach (var plugin in m_configManager.GetPluginInstallDir().EnumerateFiles("*.dll")) {
                try {
                    PluginRegistry.Instance.LoadPlugin(plugin, true);
                } catch (Exception ex) {
                    m_appLogger.Error($"Failed to load assembly { plugin.FullName }!");
                    m_appLogger.Debug($"Error: { ex.Message }");

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
                    var promptInput = ShowPrompt();
                    if (string.IsNullOrEmpty(promptInput)) { continue; }

                    var splitString = promptInput.Split(' ', '\t');
                    if (TryGetCommand(splitString.First(), out var command)) {
                        if (command is null) { continue; }

                        var argArray = new string[splitString.Length - 1];
                        if (argArray.Length > 0) {
                            Array.Copy(splitString[1..], argArray, argArray.Length);
                        }

                        var result = await command.ExecuteAsync(argArray);

                        if (result is CommandErrorResult errResult && !string.IsNullOrEmpty(result?.Message)) {
                            ConsoleErrorWrite(result.Message);

                            m_appLogger.Error(errResult.Result as Exception, $"Command execution failed! Command: { command.Name } { string.Join(' ', argArray) }");
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
        private string? ShowPrompt() {
            const string PROMPT_STR = "hermod > ";

            void WritePrompt(bool newLine = true) {
                if (newLine) { Console.WriteLine(); }
                Console.Write(PROMPT_STR);
            }

            WritePrompt();
            StringBuilder lineCache = new StringBuilder();

            ConsoleKeyInfo keyCode;
            var historyStartIndex = 0;

            while ((keyCode = Console.ReadKey()).Key != ConsoleKey.Enter) {
                switch (keyCode.Key) {
                    case ConsoleKey.Tab: {
                        var autocompletedString = GetAutocompletion(lineCache.ToString());
                        if (autocompletedString is null) {
                            Console.Beep();
                            continue;
                        }

                        Console.Write(autocompletedString);
                        lineCache.Append(autocompletedString);
                        break;
                    }
                    case ConsoleKey.Backspace:
                        if (lineCache.Length == 0) {
                            Debug.WriteLine("Cannot delete any more characters!");
                            continue;
                        }
                        lineCache = lineCache.Remove(lineCache.Length - 1, 1);

                        // This surely isn't the best way to handle this, but apparently the terminal doesn't response correctly to \b
                        Console.Write('\b');
                        Console.Write(' ');
                        Console.Write('\b');
                        continue;
                    case ConsoleKey.UpArrow:
                        if (m_previousCommands.Count == 0 || historyStartIndex == m_previousCommands.Count) {
                            Console.Beep();
                            continue;
                        }

                        lineCache.Clear();
                        var cmdIndex = m_previousCommands.Count - historyStartIndex - 1;
                        lineCache.Append(m_previousCommands.ElementAt(cmdIndex));
                        historyStartIndex++;
                        Console.CursorLeft = 0;
                        WritePrompt();
                        Console.Write(lineCache.ToString());
                        continue;
                    case ConsoleKey.C:
                        if (keyCode.Modifiers == ConsoleModifiers.Control) {
                            return ShowPrompt();
                        }
                        break;
                    case ConsoleKey.LeftArrow:
                        if (Console.CursorLeft > PROMPT_STR.Length) {
                            Console.CursorLeft--;
                        }
                        continue;
                    case ConsoleKey.RightArrow:
                        if (Console.CursorLeft <= PROMPT_STR.Length + lineCache.Length) {
                            Console.CursorLeft++;
                        }
                        continue;
                }

                lineCache.Append(keyCode.KeyChar);
            }

            Console.WriteLine();
            var cmdString = lineCache.ToString().Trim();
            m_previousCommands.Push(cmdString);
            return cmdString;
        }

        /// <summary>
        /// Attempts to get an auto completed string for the user's input.
        /// </summary>
        /// <param name="input">The current input in the interactive prompt.</param>
        /// <param name="maxDistance">The max levenshtein distance for the string to match.</param>
        /// <returns>The matched string or <code >default</code> if not matches were found.</returns>
        private string? GetAutocompletion(string input, int maxDistance = 2) {
            var matches =
                from command in PluginRegistry.Instance.GetAllCommands()
                let distance = LevenshteinDistance(command.Name, input)
                where distance <= maxDistance
                select command.Name;

            return matches.FirstOrDefault();
        }

        private int LevenshteinDistance(string haystack, string needle) {
            // Special cases
            if (haystack == needle) { return 0; }
            if (haystack.Length == 0) { return needle.Length; }
            if (needle.Length == 0) { return haystack.Length; }

            // Initialize the distance matrix
            int[,] distance = new int[haystack.Length + 1, needle.Length + 1];
            for (int i = 0; i <= haystack.Length; i++) {
                distance[i, 0] = i;
            }
            for (int j = 0; j <= needle.Length; j++) {
                distance[0, j] = j;
            }

            // Calculate the distance
            for (int i = 1; i <= haystack.Length; i++) {
                for (int j = 1; j <= needle.Length; j++) {
                    int cost = (haystack[i - 1] == needle[j - 1]) ? 0 : 1;
                    distance[i, j] = Math.Min(Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1), distance[i - 1, j - 1] + cost);
                }
            }
            // Return the distance
            return distance[haystack.Length, needle.Length];
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
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(message);
                Console.BackgroundColor = prevBackground;
                Console.ForegroundColor = prevForegound;
            }
        }
    }
}

