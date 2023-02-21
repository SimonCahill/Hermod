using System;

namespace Hermod.Config.Detail {

    using Core;

    using Serilog.Events;

    /// <summary>
    /// Contains configuration information specific to logger configurations.
    /// </summary>
    public class LoggerConfig {

        public LoggerConfig() { }

        public bool EnableLogging { get; set; }

        public string? LogLevel { get; set; }

        LogEventLevel LogEventLevel {
            get {
                switch (LogLevel.ToLowerInvariant()) {
                    default:
                    case "warn":
                    case "warning":
                        return LogEventLevel.Warning;
                    case "trace":
                    case "debug":
                        return LogEventLevel.Debug;
                    case "info":
                    case "information":
                    case "informational":
                        return LogEventLevel.Information;
                    case "error":
                        return LogEventLevel.Error;
                    case "critical":
                    case "fatal":
                        return LogEventLevel.Fatal;
                }
            }
        }

        private string? m_logLocation;
        public string? LogLocation {
            get => m_logLocation;
            set {
                var tmp = value?.ToLowerInvariant() ?? "sysdefault"; // allow silent failure

                switch (tmp) {
                    case "sysdefault":
                        tmp = Path.Combine(AppInfo.GetLocalHermodDirectory().FullName, AppInfo.HermodLogDirName);
                        return;
                    case "hermoddir":
                        tmp = Path.Combine(AppInfo.GetBaseHermodDirectory().FullName, AppInfo.HermodLogDirName);
                        break;
                    default:
                        break;
                }

                m_logLocation = tmp;
            }
        }

        public string? LogFileName { get; set; }
    }
}

