using System;

namespace Hermod.Core {

	using Serilog;
    using Serilog.Events;
	using Serilog.Formatting.Json;

	/// <summary>
	/// Custom logger class for Hermod; internally uses Serilog.
	/// </summary>
	public class Logger {

		/// <summary>
		/// Gets or sets a value indicating whether or not to enable console logging.
		/// </summary>
		bool EnableConsoleOutput { get; set; } = false;

		#if DEBUG
		bool EnableDebugOutput { get; set; } = true;
		#else
		bool EnableDebugOutput { get; set; } = false;
		#endif

		bool EnableFileOutput { get; set; } = true;

		#if DEBUG
		LogEventLevel ConsoleLogLevel { get; set; } = LogEventLevel.Warning;
		#else
		LogEventLevel ConsoleLogLevel { get; set; } = LogEventLevel.Debug;
		#endif

		LogEventLevel FileLogLevel { get; set; } = LogEventLevel.Information;

		FileInfo? LogFilePath { get; set; } = null;

		long MaxLogFileSize { get; set; } = (long)Math.Pow(1024, 3) * 5; // default is 5MiB

		RollingInterval FileRollingInterval { get; set; } = RollingInterval.Day;

		bool RollOnFileSizeLimit { get; set; } = true;

		private ILogger? m_logger;

		public Logger() { }

		public Logger(bool enableConsole, bool enableDebug, bool enableFile) {
			EnableConsoleOutput = enableConsole;
			EnableDebugOutput = enableDebug;
			EnableFileOutput = enableFile;
		}

		/// <summary>
		/// Creates a new instance of the Serilog logger.
		/// </summary>
		protected void CreateLogger() {
			var logCfg = new LoggerConfiguration();
			if (EnableConsoleOutput) {
				logCfg.WriteTo.Console(ConsoleLogLevel, applyThemeToRedirectedOutput: true);
			}
			if (EnableDebugOutput) {
				logCfg.WriteTo.Debug(LogEventLevel.Debug);
			}
			if (EnableFileOutput && LogFilePath != null) {
				logCfg.WriteTo.File(
					new JsonFormatter(),
					LogFilePath.FullName,
					FileLogLevel,
					MaxLogFileSize,
					rollingInterval: FileRollingInterval,
					rollOnFileSizeLimit: RollOnFileSizeLimit,
					shared: true

				);
			}

			m_logger = logCfg.CreateLogger();
		}
	}
}

