using System;

namespace Hermod {

    using Config;

	using getopt.net;

    using Serilog;
    using Serilog.Events;

    using System.IO;
	using System.Reflection;

    class Program {

        private static string _shortOpts = "c:L:hv%U"; // the options used for this application
        private static Option[] _longOpts = new[] {
            new Option { Name = "config",           ArgumentType = ArgumentType.Required,   Flag = IntPtr.Zero, Value = 'c' },
            new Option { Name = "log-lvl",          ArgumentType = ArgumentType.Required,   Flag = IntPtr.Zero, Value = 'L' },
            new Option { Name = "help",             ArgumentType = ArgumentType.None,       Flag = IntPtr.Zero, Value = 'h' },
            new Option { Name = "version",          ArgumentType = ArgumentType.None,       Flag = IntPtr.Zero, Value = 'v' },
            new Option { Name = "check-updates",    ArgumentType = ArgumentType.None,       Flag = IntPtr.Zero, Value = 'U' },
            new Option { Name = "reset-cfg",        ArgumentType = ArgumentType.Optional,   Flag = IntPtr.Zero, Value = '%' }
            // add more as required
        };

        private static FileInfo? _overriddenConfigLocation = null;
        private static ILogger? _appLogger = null;
        private static LogEventLevel? _logLevel = null;
        private static ConfigManager _cfgManager = ConfigManager.Instance;

        static int Main(string[] args) {

            Console.WriteLine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
            return 0;

            var returnCode = ParseArgs(args);
            if (returnCode != 0) { return returnCode - 1; }

            InitialiseConfigs();

            InitialiseLogger();

            var app = new Hermod(_cfgManager, _appLogger);

            return 0;
        }

        static void InitialiseConfigs() {
            if (_overriddenConfigLocation != null) {
                _cfgManager.ConfigFile = _overriddenConfigLocation;
            } else {
                _cfgManager.LoadConfig();
            }

            
        }

        static void InitialiseLogger() {
            _appLogger = Log.Logger;
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
                        if (!GetLogLevelFromArg(ref optArg)) {
                            Console.Error.WriteLine($"Invalid argument found for log level! Default was set: { _logLevel.ToString().ToLowerInvariant() }");
                        }
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
        static bool GetLogLevelFromArg(ref string arg) {
            arg = arg.ToLowerInvariant();

            switch (arg) {
                case "debug":
                    _logLevel = LogEventLevel.Debug;
                    break;
                case "trace":
                    _logLevel = LogEventLevel.Verbose;
                    break;
                case "warn":
                case "warning":
                    _logLevel = LogEventLevel.Warning;
                    break;
                case "error":
                    _logLevel = LogEventLevel.Error;
                    break;
                case "info":
                case "information":
                    _logLevel = LogEventLevel.Information;
                    break;
                case "fatal":
                    _logLevel = LogEventLevel.Fatal;
                    break;
                default:
                    _logLevel = LogEventLevel.Warning;
                    return false;
            }

            return true;
        }

    }

}