using System;

namespace Hermod.Config {

    public delegate void ConfigChangedEventHandler(object? sender, ConfigChangedEventArgs e);

    /// <summary>
    /// Event arguments for when a configuration has changed.
    /// </summary>
    public class ConfigChangedEventArgs: EventArgs {

        /// <summary>
        /// The name of the configuration that was changed.
        /// </summary>
        public string ConfigName { get; set; }

        /// <summary>
        /// The old config value, boxed to an object.
        /// </summary>
        public object? OldConfigValue { get; set; }

        /// <summary>
        /// The new config value, boxed to an object.
        /// </summary>
        public object? NewConfigValue { get; set; }

        /// <summary>
        /// The type the new configuration value has.
        /// </summary>
        public Type ConfigType { get; set; }

        public ConfigChangedEventArgs(): this(string.Empty, default, default, typeof(object)) { }

        public ConfigChangedEventArgs(string configName, object? oldCfgVal, object? newCfgVal, Type newCfgType) {
            ConfigName = configName;
            OldConfigValue = oldCfgVal;
            NewConfigValue = newCfgVal;
            ConfigType = newCfgType;
        }

    }

    /// <summary>
    /// Generic contract defining the behaviour when a config was changed.
    /// </summary>
    public interface IConfigChanged {

        /// <summary>
        /// Handles situations where configurations are modified.
        /// </summary>
        public event ConfigChangedEventHandler? ConfigChanged;

    }
}

