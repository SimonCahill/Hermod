using System;

namespace Hermod.Config {

    public delegate void ConfigLoadedEventHandler(object? sender, ConfigLoadedEventArgs e);

    public class ConfigLoadedEventArgs: EventArgs { }

    /// <summary>
    /// Interface defining a contract which notifies anyone whom it may concern
    /// when the application configuration has been (re-)loaded.
    /// </summary>
    public interface IConfigLoaded {

        event ConfigLoadedEventHandler? ConfigLoaded;

    }
}

