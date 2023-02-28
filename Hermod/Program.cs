using System;

namespace Hermod {

    using Config;

	using getopt.net;

    using Serilog;
    using Serilog.Events;

    using System.IO;
	using System.Reflection;

    /// <summary>
    /// Hermod's entry point.
    ///
    /// Handles all pre-init steps and starts execution of the main business logic.
    /// </summary>
    class Program {

        private static string _shortOpts = "c:L:hv%Ui"; // the options used for this application
        private static Option[] _longOpts = new[] {
            new Option { Name = "config",           ArgumentType = ArgumentType.Required,  Value = 'c' },
            new Option { Name = "log-lvl",          ArgumentType = ArgumentType.Required,  Value = 'L' },
            new Option { Name = "help",             ArgumentType = ArgumentType.None,      Value = 'h' },
            new Option { Name = "version",          ArgumentType = ArgumentType.None,      Value = 'v' },
            new Option { Name = "check-updates",    ArgumentType = ArgumentType.None,      Value = 'U' },
            new Option { Name = "reset-cfg",        ArgumentType = ArgumentType.Optional,  Value = '%' },
            new Option { Name = "interactive",      ArgumentType = ArgumentType.None,      Value = 'i' }
            // add more as required
        };

        private static FileInfo? _overriddenConfigLocation = null;
        private static ILogger? _appLogger = null;
        private static LogEventLevel? _logLevel = null;
        private static ConfigManager _cfgManager = ConfigManager.Instance;
        private static bool _interactiveMode = false;

        static async Task<int> Main(string[] args) {
            var returnCode = ParseArgs(args);
            if (returnCode != 0) { return returnCode - 1; }

            InitialiseConfigs();

            InitialiseLogger();

            var app = new Hermod(_cfgManager, _appLogger) {
                InteractiveMode = _interactiveMode,
                m_keepAlive = true
            };
            app.StartUp();

            return await app.Execute();
        }

        /// <summary>
        /// Initialises the application's config manager.
        /// </summary>
        static void InitialiseConfigs() {
            if (_overriddenConfigLocation != null) {
                _cfgManager.ConfigFile = _overriddenConfigLocation;
            } else {
                _cfgManager.LoadConfig();
            }
        }

        /// <summary>
        /// Initialises the application's logger.
        /// </summary>
        static void InitialiseLogger() {
            var consoleSettings = _cfgManager.GetConsoleLoggerConfig();
            var fileSettings = _cfgManager.GetFileLoggerConfig();
            Core.Logger logger = new Core.Logger() {
                ConsoleLogLevel = _logLevel ?? GetLogLevelFromArg(consoleSettings.LogLevel) ?? LogEventLevel.Warning,
                EnableConsoleOutput = consoleSettings.EnableLogging,

                FileLogLevel = GetLogLevelFromArg(fileSettings.LogLevel) ?? LogEventLevel.Information,
                EnableFileOutput = fileSettings.EnableLogging
            };

            _appLogger = logger.GetLogger();
        }

        /// <summary>
        /// Parses the command-line arguments passed to the application.
        /// </summary>
        /// <param name="args">The arguments passed.</param>
        /// <returns>0 if program execution shall continue. Non-zero otherwise.</returns>
        static int ParseArgs(string[] args) {

            var getopt = new GetOpt {
                AppArgs = args,
                DoubleDashStopsParsing = true,
                Options = _longOpts,
                ShortOpts = _shortOpts
            };

            int optChar = -1;
            do {
                optChar = getopt.GetNextOpt(out var optArg);

                switch (optChar) {
                    case -1:
                    case 0:
                        break;
                    case 'h':
                        PrintHelp();
                        return 1;
                    case 'v':
                        PrintVersion();
                        return 1;
                    case 'c':
                        if (string.IsNullOrEmpty(optArg)) {
                            Console.Error.WriteLine("Missing argument for --config!");
                            return 2;
                        }
                        _overriddenConfigLocation = new FileInfo(optArg);
                        break;
                    case 'L':
                        if (string.IsNullOrEmpty(optArg)) {
                            Console.Error.WriteLine("Missing argument for --log-lvl!");
                            return 2;
                        }
                        _logLevel = GetLogLevelFromArg(optArg);
                        break;
                    case 'i':
                        _interactiveMode = true;
                        break;
                    default:
                        Console.Error.WriteLine($"The argument { optChar } is not yet implemented!");
                        break;
                }
            } while (optChar != -1);

            return 0;
        }

        /// <summary>
        /// Prints the help text for hermod.
        /// </summary>
        static void PrintHelp() {
            Console.WriteLine(
"""
hermod {0} - A high-performance, cross-platform email archival and search engine

Usage: (hermod has full getopt support)
    hermod # no options = normal execution
    hermod [options]

Switches:
    --help,         -h          Display this entry and exit
    --version,      -v          Display the application version and exit
    --reset-cfg,    -%          !!! DANGER !!! Resets the configurations to their default values!

Arguments:
    --config,       -c<config>  Override the application config file location
    --log-lvl,      -L<lvl>     Override the log level: debug, trace, error, warning (default), info, critical
""",
                GetApplicationVersion()
            );
        }

        /// <summary>
        /// Gets the application's version string.
        /// </summary>
        /// <returns>A string matching v0.0.0.0</returns>
        static string GetApplicationVersion() {
            var version = Assembly.GetExecutingAssembly().GetName().Version;

            return $"v{ version?.Major }.{ version?.MajorRevision }.{ version?.Minor }.{ version?.MinorRevision }";
        }

        /// <summary>
        /// Prints the application's version.
        /// </summary>
        static void PrintVersion() => Console.WriteLine("hermod {0}", GetApplicationVersion());

        /// <summary>
        /// Converts an incoming application argument to its respective log level.
        /// </summary>
        /// <param name="arg">The argument to parse.</param>
        /// <returns><code >true</code> if an appropriate log level was found. <code >false</code> otherwise.</returns>
        static LogEventLevel? GetLogLevelFromArg(string? arg) {
            arg = arg?.ToLowerInvariant() ?? string.Empty;

            switch (arg) {
                case "debug":
                    return LogEventLevel.Debug;
                case "trace":
                    return LogEventLevel.Verbose;
                case "warn":
                case "warning":
                    return LogEventLevel.Warning;
                case "error":
                    return LogEventLevel.Error;
                case "info":
                case "information":
                    return LogEventLevel.Information;
                case "fatal":
                    return LogEventLevel.Fatal;
                default:
                    return null;
            }
        }

    }

}